from pydantic import BaseModel, Field
from typing import Optional, Any
from datetime import datetime, timezone

class ChatMessageRequest(BaseModel):
    session_id: str = Field(..., description="Unique conversation session ID")
    message: str = Field(..., description="User natural language message")
    customer_id: Optional[str] = Field(None, description="Logged in customer UUID if authenticated")
    customer_name: Optional[str] = None
    customer_phone: Optional[str] = None
    delivery_address: Optional[str] = None

class OrderItemSummary(BaseModel):
    product_id: str
    sku: str
    name: str
    quantity: int
    unit_price: float
    line_total: float
    available_stock: int
    has_sufficient_stock: bool

class OrderCalculationSummary(BaseModel):
    subtotal: float
    tax_amount: float
    tax_rate: float
    shipping_charges: float
    grand_total: float
    all_items_in_stock: bool
    items: list[OrderItemSummary] = []

class ChatMessageResponse(BaseModel):
    session_id: str
    message: str
    intent: Optional[str] = None
    awaiting_confirmation: bool = False
    order_summary: Optional[OrderCalculationSummary] = None
    order_number: Optional[str] = None
    structured_data: Optional[dict[str, Any]] = None
    suggestions: list[str] = []
    timestamp: datetime = Field(default_factory=lambda: datetime.now(timezone.utc))

class SessionResetRequest(BaseModel):
    session_id: str
