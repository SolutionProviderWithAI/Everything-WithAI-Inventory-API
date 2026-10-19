"""
End-to-End System Scenario Verification
=======================================
Verifies the complete lifecycle requirements:
1. Product A verified (₹100 selling price, 50 opening stock).
2. Chatbot inquiry: "How many units of Product A are available?" -> 50 units verified.
3. Order draft inquiry: "I need 10 units of Product A" -> server calculates ₹1,000 subtotal, ₹50 tax, ₹0 shipping, ₹1,050 total.
4. Order confirmation -> backend reserves 10 units; available becomes 40 (50 - 10).
5. Order dispatch -> physical on-hand stock reduced to 40 (40 - 0 = 40).
6. Stock shortage rejection -> 100 units requested when 40 available rejected with InsufficientStock, no overselling.
7. Concurrency safety -> parallel requests contending for limited stock without overselling or negative inventory.
"""

import sys
import time
import uuid
import asyncio
import httpx

if hasattr(sys.stdout, 'reconfigure'):
    sys.stdout.reconfigure(encoding='utf-8')

BACKEND_BASE = "http://localhost:5000/api"
CHATBOT_BASE = "http://localhost:8000"

async def run_scenario():
    print("=" * 70)
    print("Starting End-to-End Autonomous Inventory & Chatbot Verification")
    print("=" * 70)

    async with httpx.AsyncClient(timeout=30.0) as client:
        # Step 1: Verify Product A in Database
        print("\n[Step 1] Querying .NET API for Product A initial state...")
        resp = await client.get(f"{BACKEND_BASE}/products?query=Product%20A")
        assert resp.status_code == 200, f"Failed to get products: {resp.text}"
        prod_data = resp.json()
        assert prod_data["items"], "Product A not found in catalog!"
        prod_a = next((p for p in prod_data["items"] if p["sku"] == "PROD-A-001"), None)
        assert prod_a is not None, "Product A (PROD-A-001) not found!"
        
        prod_id = prod_a["id"]
        selling_price = prod_a["sellingPrice"]
        initial_available = prod_a["availableToSell"]
        initial_on_hand = prod_a["totalQuantityOnHand"]
        initial_reserved = prod_a["totalReservedQuantity"]

        # Ensure initial available is exactly 50 for repeated scenario executions
        if initial_available != 50:
            auth_init = await client.post(f"{BACKEND_BASE}/auth/login", json={
                "usernameOrEmail": "admin",
                "password": "Admin@123"
            })
            token_init = auth_init.json()["token"]
            diff = 50 - initial_available
            await client.post(f"{BACKEND_BASE}/inventory/adjustments", headers={"Authorization": f"Bearer {token_init}"}, json={
                "productId": prod_id,
                "warehouseId": "b0000001-0000-0000-0000-000000000001",
                "quantityAdjustment": diff,
                "reason": "ScenarioTestSetup",
                "notes": "Reset available stock to exactly 50 units"
            })
            prod_a = (await client.get(f"{BACKEND_BASE}/products/{prod_id}")).json()
            initial_available = prod_a["availableToSell"]
            initial_on_hand = prod_a["totalQuantityOnHand"]
            initial_reserved = prod_a["totalReservedQuantity"]

        print(f"  -> Product ID: {prod_id}")
        print(f"  -> SKU: {prod_a['sku']}, Name: {prod_a['name']}")
        print(f"  -> Selling Price: ₹{selling_price}")
        print(f"  -> Initial On-Hand: {initial_on_hand}, Reserved: {initial_reserved}, Available: {initial_available}")
        assert selling_price == 100.0, f"Expected price ₹100, got {selling_price}"
        assert initial_available == 50, f"Expected exactly 50 units available, got {initial_available}"
        print("  ✓ Step 1 PASSED: Product A verified with ₹100 price and 50 units stock.")

        # Step 2: Chatbot Natural Language Stock Check
        session_id = f"test_e2e_{uuid.uuid4().hex[:8]}"
        print(f"\n[Step 2] Customer asks Chatbot: 'How many units of Product A are available?' (Session: {session_id})")
        chat_resp = await client.post(f"{CHATBOT_BASE}/chat/message", json={
            "session_id": session_id,
            "message": "How many units of Product A are available?"
        })
        assert chat_resp.status_code == 200, f"Chatbot error: {chat_resp.text}"
        chat_data = chat_resp.json()
        print(f"  -> Bot Response: {chat_data['message']}")
        assert "Product A" in chat_data["message"]
        assert str(int(initial_available)) in chat_data["message"]
        print("  ✓ Step 2 PASSED: Chatbot retrieved live stock from backend.")

        # Step 3: Customer requests 10 units of Product A
        print("\n[Step 3] Customer asks Chatbot: 'I need 10 units of Product A'")
        order_intent_resp = await client.post(f"{CHATBOT_BASE}/chat/message", json={
            "session_id": session_id,
            "message": "I need 10 units of Product A",
            "customer_name": "John Doe",
            "delivery_address": "Flat 402, Lotus Grandeur, Mumbai 400069"
        })
        assert order_intent_resp.status_code == 200
        order_data = order_intent_resp.json()
        print(f"  -> Bot Response: {order_data['message']}")
        assert order_data["awaiting_confirmation"] is True, "Bot should await confirmation!"
        summary = order_data["order_summary"]
        assert summary is not None, "Order summary should be generated by server pricing engine!"
        
        print(f"  -> Server Calculation Summary:")
        print(f"     Subtotal: ₹{summary['subtotal']}")
        print(f"     GST Tax (5%): ₹{summary['tax_amount']}")
        print(f"     Shipping Charges: ₹{summary['shipping_charges']}")
        print(f"     Grand Total: ₹{summary['grand_total']}")
        
        assert summary["subtotal"] == 1000.0, f"Expected ₹1000 subtotal, got {summary['subtotal']}"
        assert summary["tax_amount"] == 50.0, f"Expected ₹50 tax, got {summary['tax_amount']}"
        assert summary["shipping_charges"] == 0.0, f"Expected ₹0 shipping (subtotal >= 1000), got {summary['shipping_charges']}"
        assert summary["grand_total"] == 1050.0, f"Expected ₹1050 grand total, got {summary['grand_total']}"
        print("  ✓ Step 3 PASSED: Server pricing calculations strictly verified (₹1,000 + ₹50 tax + ₹0 shipping = ₹1,050).")

        # Step 4: Customer Confirms Order
        print("\n[Step 4] Customer confirms order: 'Yes, please confirm my order'")
        confirm_resp = await client.post(f"{CHATBOT_BASE}/chat/message", json={
            "session_id": session_id,
            "message": "Yes, please confirm my order"
        })
        assert confirm_resp.status_code == 200
        confirm_data = confirm_resp.json()
        order_number = confirm_data.get("order_number")
        print(f"  -> Bot Response: {confirm_data['message']}")
        print(f"  -> Generated Order Number: {order_number}")
        assert order_number and order_number.startswith("ORD-"), "Valid order number should be returned!"

        # Verify stock reservation in backend
        prod_check = (await client.get(f"{BACKEND_BASE}/products/{prod_id}")).json()
        expected_available = initial_available - 10
        print(f"  -> Backend Inventory Status After Order:")
        print(f"     On-Hand: {prod_check['totalQuantityOnHand']}")
        print(f"     Reserved: {prod_check['totalReservedQuantity']}")
        print(f"     Available To Sell: {prod_check['availableToSell']}")
        assert prod_check["totalReservedQuantity"] >= 10, "10 units must be reserved!"
        assert prod_check["availableToSell"] == expected_available, f"Expected {expected_available} available, got {prod_check['availableToSell']}"
        print(f"  ✓ Step 4 PASSED: 10 units atomically reserved. Available stock decreased to {expected_available}.")

        # Step 5: Order Dispatch & Reservation Fulfillment
        print(f"\n[Step 5] Dispatching Order {order_number} as Authorized Staff...")
        # Authenticate as admin to dispatch
        auth_resp = await client.post(f"{BACKEND_BASE}/auth/login", json={
            "usernameOrEmail": "admin",
            "password": "Admin@123"
        })
        assert auth_resp.status_code == 200, f"Login failed: {auth_resp.text}"
        admin_token = auth_resp.json()["token"]
        auth_headers = {"Authorization": f"Bearer {admin_token}"}

        # Get order details to obtain order ID
        orders_resp = await client.get(f"{BACKEND_BASE}/orders", headers=auth_headers)
        order_record = next(o for o in orders_resp.json()["items"] if o["orderNumber"] == order_number)
        order_id = order_record["id"]

        dispatch_resp = await client.post(f"{BACKEND_BASE}/orders/{order_id}/dispatch", headers=auth_headers)
        assert dispatch_resp.status_code == 200, f"Dispatch failed: {dispatch_resp.text}"
        dispatched_order = dispatch_resp.json()
        assert dispatched_order["status"] == "Dispatched"
        print(f"  -> Order Status: {dispatched_order['status']}")

        prod_post_dispatch = (await client.get(f"{BACKEND_BASE}/products/{prod_id}")).json()
        print(f"  -> Backend Inventory Status After Dispatch:")
        print(f"     On-Hand: {prod_post_dispatch['totalQuantityOnHand']}")
        print(f"     Reserved: {prod_post_dispatch['totalReservedQuantity']}")
        print(f"     Available To Sell: {prod_post_dispatch['availableToSell']}")
        expected_on_hand = initial_on_hand - 10
        assert prod_post_dispatch["totalQuantityOnHand"] == expected_on_hand, f"Expected on-hand {expected_on_hand}, got {prod_post_dispatch['totalQuantityOnHand']}"
        print(f"  ✓ Step 5 PASSED: Order dispatched. Physical on-hand stock deducted to {expected_on_hand}.")

        # Step 6: Insufficient Stock Rejection (Ordering 100 units when only 40 available)
        current_available = prod_post_dispatch["availableToSell"]
        print(f"\n[Step 6] Testing Insufficient Stock Rejection (Attempting 100 units when only {current_available} available)...")
        # Direct backend order attempt
        fail_order_payload = {
            "customerName": "Test Overbuyer",
            "customerEmail": "overbuyer@test.com",
            "items": [
                {
                    "productId": prod_id,
                    "quantity": 100,
                    "unitPrice": 100.0
                }
            ]
        }
        fail_resp = await client.post(f"{BACKEND_BASE}/orders", json=fail_order_payload)
        print(f"  -> Backend Response Status: {fail_resp.status_code}")
        print(f"  -> Response Detail: {fail_resp.text}")
        assert fail_resp.status_code == 400 or fail_resp.status_code == 409, "Should reject order with 400/409!"
        assert "Insufficient" in fail_resp.text or "stock" in fail_resp.text.lower()

        # Verify stock was untouched
        prod_untouched = (await client.get(f"{BACKEND_BASE}/products/{prod_id}")).json()
        assert prod_untouched["availableToSell"] == current_available, "Available stock must not change on rejected order!"
        assert prod_untouched["availableToSell"] >= 0, "Stock must never become negative!"
        print(f"  ✓ Step 6 PASSED: Order rejected cleanly with InsufficientStockException. Stock remains {current_available}.")

        # Step 7: Concurrency Test (Parallel threads contending for remaining stock)
        print(f"\n[Step 7] Running Concurrency Test (10 parallel requests contending for limited stock)...")
        concurrent_count = 10
        qty_per_order = 5 # Total requested: 50 units, but only ~40 units available!

        async def place_concurrent_order(idx):
            payload = {
                "customerName": f"Concurrent Customer {idx}",
                "customerEmail": f"concurrent{idx}@test.com",
                "idempotencyKey": f"concurrent-test-{uuid.uuid4()}",
                "items": [
                    {
                        "productId": prod_id,
                        "quantity": qty_per_order,
                        "unitPrice": 100.0
                    }
                ]
            }
            res = await client.post(f"{BACKEND_BASE}/orders", json=payload)
            return res.status_code

        tasks = [place_concurrent_order(i) for i in range(concurrent_count)]
        results = await asyncio.gather(*tasks)
        successes = sum(1 for r in results if r == 201)
        failures = sum(1 for r in results if r in (400, 409))
        print(f"  -> Parallel Requests: {concurrent_count}")
        print(f"  -> Successful Orders: {successes} ({successes * qty_per_order} units reserved)")
        print(f"  -> Rejected (Insufficient Stock): {failures}")

        # Final stock check
        final_prod = (await client.get(f"{BACKEND_BASE}/products/{prod_id}")).json()
        print(f"  -> Final Stock:")
        print(f"     On-Hand: {final_prod['totalQuantityOnHand']}")
        print(f"     Reserved: {final_prod['totalReservedQuantity']}")
        print(f"     Available To Sell: {final_prod['availableToSell']}")
        assert final_prod["availableToSell"] >= 0, "Fatal: Available stock became negative!"
        assert final_prod["totalQuantityOnHand"] >= final_prod["totalReservedQuantity"], "Fatal: Overselling detected!"
        print("  ✓ Step 7 PASSED: Zero overselling under high concurrency. Positive inventory invariant strictly maintained.")

    print("\n" + "=" * 70)
    print("ALL 7 CORE SCENARIO TESTS COMPLETED SUCCESSFULLY!")
    print("=" * 70)

if __name__ == "__main__":
    asyncio.run(run_scenario())
