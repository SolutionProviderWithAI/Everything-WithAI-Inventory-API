import pytest
import sys
import os

# Add parent directory to path so 'app' can be imported
sys.path.insert(0, os.path.abspath(os.path.join(os.path.dirname(__file__), "..")))

from app.nlu import classify_intent, extract_order_number, extract_products_and_quantities
from app.models import ChatMessageRequest
from app.agent import process_chat_message, reset_session

def test_intent_classification_required_phrases():
    # 1. "Show me all available products."
    assert classify_intent("Show me all available products.") == "SHOW_PRODUCTS"
    assert classify_intent("What products do you have?") == "SHOW_PRODUCTS"

    # 2. "How many units of Product A are available?"
    assert classify_intent("How many units of Product A are available?") == "CHECK_STOCK"
    assert classify_intent("Is Product B in stock?") == "CHECK_STOCK"

    # 3. "I need 10 units of Product A and 5 units of Product B."
    assert classify_intent("I need 10 units of Product A and 5 units of Product B.") == "ORDER_INTENT"
    assert classify_intent("Order 20 units of Smart IoT Sensor") == "ORDER_INTENT"

    # 4. "Calculate the total price for my order."
    assert classify_intent("Calculate the total price for my order.") == "CALCULATE_PRICE"
    assert classify_intent("Calculate total price") == "CALCULATE_PRICE"

    # 5. "Can I order 100 units if only 70 are available?"
    assert classify_intent("Can I order 100 units if only 70 are available?") == "ORDER_INTENT"

    # 6. "Place my order."
    assert classify_intent("Place my order.") == "CONFIRM_ORDER"
    assert classify_intent("Yes") == "CONFIRM_ORDER"
    assert classify_intent("Confirm") == "CONFIRM_ORDER"

    # 7. "Show me my previous orders."
    assert classify_intent("Show me my previous orders.") == "TRACK_ORDER"
    assert classify_intent("Where is my order ORD-20261018-0001?") == "TRACK_ORDER"

    # 8. "Cancel my order if it has not been dispatched."
    assert classify_intent("Cancel my order if it has not been dispatched.") == "CANCEL_ORDER"
    assert classify_intent("Cancel order ORD-20261018-1234") == "CANCEL_ORDER"

def test_entity_extraction_order_numbers():
    assert extract_order_number("Where is my order ORD-20261018-1234?") == "ORD-20261018-1234"
    assert extract_order_number("Cancel order ORD-20261018-ABCD please") == "ORD-20261018-ABCD"
    assert extract_order_number("I do not have an order yet") is None

def test_entity_extraction_products_and_quantities():
    known_products = [
        {"id": "p1", "sku": "PROD-A-001", "name": "Product A", "sellingPrice": 100.0, "availableToSell": 50},
        {"id": "p2", "sku": "PROD-B-002", "name": "Product B", "sellingPrice": 250.0, "availableToSell": 120},
        {"id": "p3", "sku": "IOT-SENS-101", "name": "Smart IoT Sensor", "sellingPrice": 499.0, "availableToSell": 85}
    ]

    text = "I need 10 units of Product A and 5 units of Product B."
    items = extract_products_and_quantities(text, known_products)

    assert len(items) == 2
    item_a = next(i for i in items if i["name"] == "Product A")
    item_b = next(i for i in items if i["name"] == "Product B")

    assert item_a["quantity"] == 10
    assert item_a["sellingPrice"] == 100.0
    assert item_b["quantity"] == 5
    assert item_b["sellingPrice"] == 250.0

@pytest.mark.asyncio
async def test_greeting_response():
    session_id = "test-greeting-session"
    reset_session(session_id)
    req = ChatMessageRequest(session_id=session_id, message="Hello!")
    resp = await process_chat_message(req)

    assert resp.intent == "GREETING"
    assert "Hello" in resp.message
    assert len(resp.suggestions) > 0
