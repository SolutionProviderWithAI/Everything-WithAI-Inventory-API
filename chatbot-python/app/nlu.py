import re
from typing import Optional

def extract_order_number(text: str) -> Optional[str]:
    match = re.search(r'\b(ORD-[0-9]{8}-[0-9A-Za-z]+)\b', text, re.IGNORECASE)
    if match:
        return match.group(1).upper()
    match2 = re.search(r'\b(ORD-[0-9A-Za-z\-]+)\b', text, re.IGNORECASE)
    if match2:
        return match2.group(1).upper()
    return None

def extract_products_and_quantities(text: str, known_products: list[dict]) -> list[dict]:
    results = []
    text_lower = text.lower()

    for p in known_products:
        p_name = p.get("name", "").lower()
        p_sku = p.get("sku", "").lower()

        if p_name in text_lower or p_sku in text_lower:
            qty = 1
            p_escaped = re.escape(p_name)
            p1 = rf'(\d+)\s*(?:units?|pcs?|pieces?|rolls?|boxes?)?\s*(?:of\s+)?{p_escaped}'
            m1 = re.search(p1, text_lower)
            if m1:
                qty = int(m1.group(1))
            else:
                p2 = rf'{p_escaped}\s*(?:with\s*)?(\d+)'
                m2 = re.search(p2, text_lower)
                if m2:
                    qty = int(m2.group(1))
                else:
                    words = text_lower.split()
                    for word in words:
                        if word.isdigit():
                            qty = int(word)
                            break

            results.append({
                "productId": p.get("id"),
                "sku": p.get("sku"),
                "name": p.get("name"),
                "sellingPrice": p.get("sellingPrice", 0),
                "availableToSell": p.get("availableToSell", 0),
                "quantity": qty
            })

    return results

def classify_intent(text: str) -> str:
    # Clean text without punctuation for matching
    cleaned = re.sub(r'[^\w\s]', '', text.lower()).strip()
    raw_lower = text.lower().strip()

    # Confirmations
    confirm_keywords = ["confirm", "place my order", "place order", "place it", "proceed", "submit", "yes please", "confirm my order", "confirm order"]
    if any(k in raw_lower for k in confirm_keywords) or cleaned in ["yes", "yep", "sure", "ok", "okay", "go ahead", "do it"]:
        return "CONFIRM_ORDER"

    # Rejections
    reject_keywords = ["cancel draft", "cancel order draft", "dont place", "reject", "nevermind", "do not place"]
    if any(k in raw_lower for k in reject_keywords) or cleaned in ["no", "nope"]:
        return "REJECT_ORDER"

    # Cancel dispatched/placed order
    if "cancel my order" in raw_lower or "cancel order" in raw_lower or "cancel the order" in raw_lower:
        return "CANCEL_ORDER"

    # Order tracking / previous orders
    if "previous orders" in raw_lower or "my orders" in raw_lower or "show me my previous orders" in raw_lower or "track order" in raw_lower or "order status" in raw_lower or "where is my order" in raw_lower:
        return "TRACK_ORDER"

    # Order placement / purchase intent (must come before check_stock so "Can I order 100 units if only 70 are available?" is classified as ORDER_INTENT)
    if "can i order" in raw_lower or "i need" in raw_lower or "i want to order" in raw_lower or "order " in raw_lower or "buy " in raw_lower or "purchase " in raw_lower:
        return "ORDER_INTENT"

    # Price / Total calculation
    if "calculate the total" in raw_lower or "calculate total" in raw_lower or "calculate price" in raw_lower or "total price" in raw_lower or "how much will it cost" in raw_lower:
        return "CALCULATE_PRICE"

    # Catalog listing
    if "show me all" in raw_lower or "show products" in raw_lower or "available products" in raw_lower or "all products" in raw_lower or "catalog" in raw_lower or "list products" in raw_lower or "what products" in raw_lower:
        return "SHOW_PRODUCTS"

    # Stock check
    if "how many units" in raw_lower or "is available" in raw_lower or "are available" in raw_lower or "check stock" in raw_lower or "stock of" in raw_lower or "availability" in raw_lower or "in stock" in raw_lower:
        return "CHECK_STOCK"

    # Greetings
    if cleaned in ["hi", "hello", "hey", "good morning", "good evening", "greetings", "hi there", "hello there"]:
        return "GREETING"

    # Help
    if "help" in cleaned or "what can you do" in raw_lower:
        return "HELP"

    return "GENERAL_QUERY"
