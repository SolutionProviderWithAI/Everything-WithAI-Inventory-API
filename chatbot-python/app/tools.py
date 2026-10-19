import httpx
import logging
from typing import Optional, Any
from app.config import settings

logger = logging.getLogger("ChatbotTools")

async def get_http_client() -> httpx.AsyncClient:
    return httpx.AsyncClient(timeout=10.0)

async def search_products(query: Optional[str] = None, active_only: bool = True) -> list[dict]:
    """Search products catalog from the .NET backend API."""
    params: dict[str, Any] = {"activeOnly": str(active_only).lower(), "pageSize": 50}
    if query:
        params["query"] = query

    url = f"{settings.backend_api_url}/products"
    try:
        async with httpx.AsyncClient(timeout=10.0) as client:
            resp = await client.get(url, params=params)
            if resp.status_code == 200:
                data = resp.json()
                return data.get("items", [])
            logger.warning(f"Product search returned status {resp.status_code}: {resp.text}")
            return []
    except Exception as e:
        logger.error(f"Failed to connect to backend for search_products: {e}")
        return []

async def check_product_stock(product_id: str) -> list[dict]:
    """Check live inventory quantities for a product across warehouses."""
    url = f"{settings.backend_api_url}/inventory/{product_id}"
    try:
        async with httpx.AsyncClient(timeout=10.0) as client:
            resp = await client.get(url)
            if resp.status_code == 200:
                return resp.json()
            return []
    except Exception as e:
        logger.error(f"Failed to check product stock: {e}")
        return []

async def calculate_order(items: list[dict], customer_id: Optional[str] = None) -> Optional[dict]:
    """Calculate order prices, taxes, shipping and verify stock server-side."""
    url = f"{settings.backend_api_url}/orders/calculate"
    payload = {
        "customerId": customer_id,
        "items": items
    }
    try:
        async with httpx.AsyncClient(timeout=10.0) as client:
            resp = await client.post(url, json=payload)
            if resp.status_code == 200:
                return resp.json()
            logger.warning(f"Order calculation failed {resp.status_code}: {resp.text}")
            return None
    except Exception as e:
        logger.error(f"Order calculation error: {e}")
        return None

async def place_order(order_payload: dict) -> tuple[bool, Optional[dict], str]:
    """Place customer order through the validated .NET backend API."""
    url = f"{settings.backend_api_url}/orders"
    try:
        async with httpx.AsyncClient(timeout=15.0) as client:
            resp = await client.post(url, json=order_payload)
            if resp.status_code in (200, 201):
                return True, resp.json(), "Order placed successfully"
            
            # Extract RFC 7807 problem details if available
            try:
                err_data = resp.json()
                detail = err_data.get("detail", resp.text)
                return False, err_data, detail
            except Exception:
                return False, None, resp.text
    except Exception as e:
        logger.error(f"Failed to submit order to backend: {e}")
        return False, None, f"Service error: {str(e)}"

async def get_order_by_number(order_number: str) -> Optional[dict]:
    """Retrieve order status and details by unique order number."""
    url = f"{settings.backend_api_url}/orders/number/{order_number.strip()}"
    try:
        async with httpx.AsyncClient(timeout=10.0) as client:
            resp = await client.get(url)
            if resp.status_code == 200:
                return resp.json()
            return None
    except Exception as e:
        logger.error(f"Failed to get order by number: {e}")
        return None

async def cancel_order(order_id: str, reason: str = "Customer requested cancellation") -> tuple[bool, str]:
    """Cancel customer order prior to dispatch."""
    url = f"{settings.backend_api_url}/orders/{order_id}/cancel"
    payload = {"reason": reason}
    try:
        async with httpx.AsyncClient(timeout=10.0) as client:
            resp = await client.post(url, json=payload)
            if resp.status_code == 200:
                return True, "Order cancelled successfully and stock reservation released."
            try:
                err = resp.json()
                return False, err.get("detail", resp.text)
            except Exception:
                return False, resp.text
    except Exception as e:
        return False, f"Cancellation error: {str(e)}"

async def log_chat_message_to_backend(
    session_id: str, sender: str, message: str,
    intent: Optional[str] = None, data: Optional[dict] = None, customer_id: Optional[str] = None
) -> None:
    """Log conversation transcript to PostgreSQL via backend API."""
    url = f"{settings.backend_api_url}/chatbot/log"
    payload = {
        "sessionId": session_id,
        "customerId": customer_id,
        "sender": sender,
        "messageText": message,
        "intentDetected": intent,
        "structuredData": str(data) if data else None
    }
    try:
        async with httpx.AsyncClient(timeout=5.0) as client:
            await client.post(url, json=payload)
    except Exception as e:
        logger.debug(f"Non-critical: Chat logging to backend failed: {e}")
