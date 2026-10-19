import logging
import uuid
from typing import Optional
from app.models import (
    ChatMessageRequest,
    ChatMessageResponse,
    OrderCalculationSummary,
    OrderItemSummary
)
from app.tools import (
    search_products,
    calculate_order,
    place_order,
    get_order_by_number,
    cancel_order,
    log_chat_message_to_backend
)
from app.nlu import (
    classify_intent,
    extract_products_and_quantities,
    extract_order_number
)

logger = logging.getLogger("ChatbotAgent")

# In-memory session state dictionary
# Key: session_id -> { "draft_items": [...], "awaiting_confirmation": bool, "pending_order": dict, "customer_name": str, ... }
SESSION_STORAGE: dict[str, dict] = {}

def get_session(session_id: str) -> dict:
    if session_id not in SESSION_STORAGE:
        SESSION_STORAGE[session_id] = {
            "draft_items": [],
            "awaiting_confirmation": False,
            "pending_calculation": None,
            "shipping_address": "Customer Registered Address",
            "customer_name": None,
            "customer_phone": None,
            "idempotency_key": None
        }
    return SESSION_STORAGE[session_id]

def reset_session(session_id: str) -> None:
    if session_id in SESSION_STORAGE:
        del SESSION_STORAGE[session_id]

async def process_chat_message(request: ChatMessageRequest) -> ChatMessageResponse:
    session = get_session(request.session_id)
    raw_message = request.message.strip()

    # Update customer details in session if provided
    if request.customer_name:
        session["customer_name"] = request.customer_name
    if request.customer_phone:
        session["customer_phone"] = request.customer_phone
    if request.delivery_address:
        session["shipping_address"] = request.delivery_address

    intent = classify_intent(raw_message)

    # 1. Handle Explicit Confirmation Flow
    if session.get("awaiting_confirmation"):
        if intent == "CONFIRM_ORDER":
            return await handle_order_confirmation(request, session)
        elif intent == "REJECT_ORDER":
            session["awaiting_confirmation"] = False
            session["pending_calculation"] = None
            session["draft_items"] = []
            msg = "Your order draft has been cancelled. No items were reserved or charged. How else may I assist you today?"
            await log_chat_message_to_backend(request.session_id, "Bot", msg, "ORDER_REJECTED", customer_id=request.customer_id)
            return ChatMessageResponse(
                session_id=request.session_id,
                message=msg,
                intent="REJECT_ORDER",
                suggestions=["Show all products", "Check stock"]
            )

    # 2. Handle Catalog Listing
    if intent == "SHOW_PRODUCTS":
        products = await search_products()
        if not products:
            msg = "I'm currently unable to retrieve the product catalog. Please try again shortly."
            return ChatMessageResponse(session_id=request.session_id, message=msg, intent=intent)

        lines = ["Here are our available products in catalog:\n"]
        for p in products:
            avail = p.get("availableToSell", 0)
            price = p.get("sellingPrice", 0)
            status = "In Stock" if avail > 0 else "Out of Stock"
            lines.append(f"• **{p.get('name')}** (SKU: `{p.get('sku')}`) — ₹{price:,.2f} | Available: {avail} units ({status})")

        lines.append("\nTo check availability or place an order, you can say: *'I need 10 units of Product A'*.")
        reply = "\n".join(lines)
        await log_chat_message_to_backend(request.session_id, "Bot", reply, intent, customer_id=request.customer_id)
        return ChatMessageResponse(
            session_id=request.session_id,
            message=reply,
            intent=intent,
            suggestions=["Order 10 units of Product A", "Check stock of Product B"]
        )

    # 3. Handle Order Cancellation
    if intent == "CANCEL_ORDER":
        order_num = extract_order_number(raw_message)
        if not order_num:
            msg = "Please provide your Order Number to cancel (e.g., *'Cancel order ORD-20261018-1001'*)."
            return ChatMessageResponse(session_id=request.session_id, message=msg, intent=intent)

        order_data = await get_order_by_number(order_num)
        if not order_data:
            msg = f"Order **{order_num}** was not found in our database. Please double check the order number."
            return ChatMessageResponse(session_id=request.session_id, message=msg, intent=intent)

        order_status = order_data.get("status", "")
        if order_status in ["Dispatched", "Delivered"]:
            msg = f"Order **{order_num}** has already been **{order_status}** and cannot be cancelled. Only orders prior to dispatch can be cancelled."
            return ChatMessageResponse(session_id=request.session_id, message=msg, intent=intent)

        order_id = order_data.get("id")
        success, cancel_msg = await cancel_order(order_id, "Customer requested cancellation via Chatbot")
        if success:
            msg = f"✅ Order **{order_num}** has been successfully cancelled and all reserved inventory has been released back into stock."
        else:
            msg = f"Unable to cancel order **{order_num}**: {cancel_msg}"

        await log_chat_message_to_backend(request.session_id, "Bot", msg, intent, customer_id=request.customer_id)
        return ChatMessageResponse(session_id=request.session_id, message=msg, intent=intent)

    # 4. Handle Order Tracking
    if intent == "TRACK_ORDER":
        order_num = extract_order_number(raw_message)
        if order_num:
            order_data = await get_order_by_number(order_num)
            if not order_data:
                msg = f"No order found with number **{order_num}**."
                return ChatMessageResponse(session_id=request.session_id, message=msg, intent=intent)

            items_desc = ", ".join([f"{it.get('quantity')}x {it.get('productName')}" for it in order_data.get("items", [])])
            msg = (
                f"📦 **Order Status for {order_num}**:\n"
                f"• Status: **{order_data.get('status')}**\n"
                f"• Payment: **{order_data.get('paymentStatus')}**\n"
                f"• Items: {items_desc}\n"
                f"• Grand Total: **₹{order_data.get('grandTotal'):,.2f}**\n"
                f"• Placed on: {order_data.get('orderDate', '')[:10]}"
            )
            return ChatMessageResponse(session_id=request.session_id, message=msg, intent=intent)
        else:
            msg = "Please provide your Order Number to check its status (e.g., *'Where is order ORD-20261018-1001'*)."
            return ChatMessageResponse(session_id=request.session_id, message=msg, intent=intent)

    # 5. Handle Stock Check or Purchasing Intent
    known_products = await search_products()
    matched_items = extract_products_and_quantities(raw_message, known_products)

    # Specific check for stock query without ordering
    if intent == "CHECK_STOCK":
        if not matched_items:
            # Check if any product name matches loosely
            for p in known_products:
                if p.get("name", "").lower() in raw_message.lower():
                    matched_items.append(p)
                    break

        if matched_items:
            p = matched_items[0]
            avail = p.get("availableToSell", 0)
            price = p.get("sellingPrice", 0)
            msg = f"There are currently **{avail} units** of **{p.get('name')}** available to sell at **₹{price:,.2f}** per unit."
            return ChatMessageResponse(
                session_id=request.session_id,
                message=msg,
                intent=intent,
                suggestions=[f"Order 10 units of {p.get('name')}", "Show all products"]
            )
        else:
            msg = "Which product's stock would you like to check? For example: *'How many units of Product A are available?'*"
            return ChatMessageResponse(session_id=request.session_id, message=msg, intent=intent)

    # 6. Handle Insufficient Stock Scenarios (e.g. "Can I order 100 units if only 50 are available?")
    if "can i order" in raw_message.lower() or intent == "ORDER_INTENT":
        if matched_items:
            insufficient_items = []
            for item in matched_items:
                if item["quantity"] > item["availableToSell"]:
                    insufficient_items.append(item)

            if insufficient_items:
                bad_item = insufficient_items[0]
                msg = (
                    f"⚠️ Only **{bad_item['availableToSell']} units** of **{bad_item['name']}** are currently available in stock. "
                    f"You cannot order **{bad_item['quantity']} units** at this time. "
                    f"Would you like to order **{bad_item['availableToSell']} units** instead?"
                )
                return ChatMessageResponse(
                    session_id=request.session_id,
                    message=msg,
                    intent="INSUFFICIENT_STOCK",
                    suggestions=[f"Order {bad_item['availableToSell']} units of {bad_item['name']}", "Show all products"]
                )

    # 7. Order Calculation / Order Placement Workflow
    if intent in ["ORDER_INTENT", "CALCULATE_PRICE"] and matched_items:
        # Prepare calculation request payload for backend
        calc_payload = [
            {"productId": it["productId"], "quantity": it["quantity"]}
            for it in matched_items
        ]

        calc_result = await calculate_order(calc_payload, customer_id=request.customer_id)
        if not calc_result:
            msg = "Could not calculate order totals at this time. Please ensure the backend service is running."
            return ChatMessageResponse(session_id=request.session_id, message=msg, intent=intent)

        # Build OrderCalculationSummary
        item_summaries = []
        for it in calc_result.get("items", []):
            item_summaries.append(OrderItemSummary(
                product_id=it["productId"],
                sku=it["sku"],
                name=it["productName"],
                quantity=it["quantity"],
                unit_price=it["unitPrice"],
                line_total=it["lineTotal"],
                available_stock=it["availableStock"],
                has_sufficient_stock=it["hasSufficientStock"]
            ))

        order_summary = OrderCalculationSummary(
            subtotal=calc_result.get("subtotal", 0),
            tax_amount=calc_result.get("taxAmount", 0),
            tax_rate=calc_result.get("taxRate", 0.05),
            shipping_charges=calc_result.get("shippingCharges", 0),
            grand_total=calc_result.get("grandTotal", 0),
            all_items_in_stock=calc_result.get("allItemsInStock", True),
            items=item_summaries
        )

        session["draft_items"] = calc_payload
        session["pending_calculation"] = order_summary
        session["awaiting_confirmation"] = True
        session["idempotency_key"] = f"CHAT-{request.session_id}-{uuid.uuid4().hex[:8]}"

        lines = ["📋 **Order Summary Preview**:"]
        for it in order_summary.items:
            lines.append(f"• **{it.name}** × {it.quantity} @ ₹{it.unit_price:,.2f} = **₹{it.line_total:,.2f}**")

        lines.append(f"\n• Subtotal: **₹{order_summary.subtotal:,.2f}**")
        lines.append(f"• Tax ({order_summary.tax_rate * 100:.0f}% GST): **₹{order_summary.tax_amount:,.2f}**")
        shipping_desc = "FREE" if order_summary.shipping_charges == 0 else f"₹{order_summary.shipping_charges:,.2f}"
        lines.append(f"• Shipping Charges: **{shipping_desc}**")
        lines.append(f"• **Total Payable Amount: ₹{order_summary.grand_total:,.2f}**")

        lines.append("\n📍 Delivery Address: *" + session["shipping_address"] + "*")
        lines.append("\n❓ **Would you like to confirm and place this order?** Reply **'Yes'** or **'Confirm'** to proceed, or **'No'** to cancel.")

        reply = "\n".join(lines)
        await log_chat_message_to_backend(request.session_id, "Bot", reply, "ORDER_PREVIEW", customer_id=request.customer_id)

        return ChatMessageResponse(
            session_id=request.session_id,
            message=reply,
            intent="ORDER_PREVIEW",
            awaiting_confirmation=True,
            order_summary=order_summary,
            suggestions=["Confirm", "Cancel order draft"]
        )

    # 8. Greetings
    if intent == "GREETING":
        msg = (
            "👋 Hello! I am your AI Order & Inventory Assistant.\n"
            "You can ask me to:\n"
            "• Search or show available products\n"
            "• Check live stock quantities\n"
            "• Calculate prices and place orders\n"
            "• Track or cancel existing orders\n\n"
            "How can I help you today?"
        )
        return ChatMessageResponse(
            session_id=request.session_id,
            message=msg,
            intent=intent,
            suggestions=["Show me all available products", "How many units of Product A are available?"]
        )

    # Default fallback
    msg = (
        "I can help you browse products, check stock, calculate order prices, and place orders. "
        "Try saying: *'Show me all available products'* or *'I need 10 units of Product A'*."
    )
    return ChatMessageResponse(
        session_id=request.session_id,
        message=msg,
        intent="GENERAL_QUERY",
        suggestions=["Show all products", "Check stock of Product A"]
    )

async def handle_order_confirmation(request: ChatMessageRequest, session: dict) -> ChatMessageResponse:
    draft_items = session.get("draft_items", [])
    if not draft_items:
        session["awaiting_confirmation"] = False
        return ChatMessageResponse(
            session_id=request.session_id,
            message="There is no active order waiting for confirmation. Would you like to select products to order?",
            intent="NO_ACTIVE_ORDER"
        )

    order_payload = {
        "customerId": request.customer_id,
        "customerName": session.get("customer_name") or request.customer_name or "Chatbot Customer",
        "customerEmail": f"customer_{request.session_id[:6]}@example.com",
        "customerPhone": session.get("customer_phone") or request.customer_phone or "+91 9876543210",
        "items": draft_items,
        "shippingAddress": session.get("shipping_address") or "Standard Customer Address",
        "idempotencyKey": session.get("idempotency_key") or f"CHAT-{request.session_id}-{uuid.uuid4().hex[:8]}",
        "notes": f"Order submitted via AI Chatbot session {request.session_id}"
    }

    success, result_data, detail_msg = await place_order(order_payload)

    # Reset confirmation state
    session["awaiting_confirmation"] = False
    session["draft_items"] = []
    session["pending_calculation"] = None

    if success and result_data:
        order_number = result_data.get("orderNumber", "ORD-CONFIRMED")
        grand_total = result_data.get("grandTotal", 0)
        items_cnt = len(result_data.get("items", []))

        msg = (
            f"🎉 **Order Placed Successfully!**\n\n"
            f"• Unique Order Number: **`{order_number}`**\n"
            f"• Status: **Confirmed** (Inventory reserved)\n"
            f"• Total Items: **{items_cnt}**\n"
            f"• Grand Total: **₹{grand_total:,.2f}**\n\n"
            f"Your inventory reservation is secured. You can track this order anytime by saying: *'Where is order {order_number}?'*."
        )

        await log_chat_message_to_backend(request.session_id, "Bot", msg, "ORDER_CONFIRMED", customer_id=request.customer_id)

        return ChatMessageResponse(
            session_id=request.session_id,
            message=msg,
            intent="ORDER_CONFIRMED",
            order_number=order_number,
            structured_data=result_data,
            suggestions=[f"Track {order_number}", "Show all products"]
        )
    else:
        msg = f"❌ Order submission could not be completed: {detail_msg}"
        await log_chat_message_to_backend(request.session_id, "Bot", msg, "ORDER_FAILED", customer_id=request.customer_id)
        return ChatMessageResponse(
            session_id=request.session_id,
            message=msg,
            intent="ORDER_FAILED",
            suggestions=["Show all products", "Check stock"]
        )
