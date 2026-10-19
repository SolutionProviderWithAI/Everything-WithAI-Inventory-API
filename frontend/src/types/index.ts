export interface Product {
  id: string;
  categoryId: string;
  categoryName: string;
  sku: string;
  name: string;
  description?: string;
  unitOfMeasurement: string;
  costPrice: number;
  sellingPrice: number;
  reorderLevel: number;
  reorderQuantity: number;
  isActive: boolean;
  totalQuantityOnHand: number;
  totalReservedQuantity: number;
  availableToSell: number;
  isLowStock: boolean;
  isOutOfStock: boolean;
  createdAt: string;
  updatedAt: string;
}

export interface Category {
  id: string;
  name: string;
  code: string;
  description?: string;
  productCount: number;
  isActive: boolean;
}

export interface InventoryItem {
  id: string;
  productId: string;
  productSku: string;
  productName: string;
  warehouseId: string;
  warehouseCode: string;
  warehouseName: string;
  quantityOnHand: number;
  reservedQuantity: number;
  damagedQuantity: number;
  minimumStock: number;
  availableToSell: number;
  updatedAt: string;
}

export interface LowStockAlert {
  productId: string;
  sku: string;
  name: string;
  quantityOnHand: number;
  reservedQuantity: number;
  availableToSell: number;
  reorderLevel: number;
  reorderQuantity: number;
  status: 'LowStock' | 'OutOfStock';
}

export interface InventoryMovement {
  id: string;
  productId: string;
  productSku: string;
  productName: string;
  warehouseId: string;
  warehouseCode: string;
  transactionType: string;
  quantityChange: number;
  balanceOnHandAfter: number;
  balanceReservedAfter: number;
  referenceType: string;
  referenceId?: string;
  notes?: string;
  createdBy: string;
  createdAt: string;
}

export interface OrderItem {
  id: string;
  productId: string;
  sku: string;
  productName: string;
  quantity: number;
  unitPrice: number;
  discountAmount: number;
  taxAmount: number;
  lineTotal: number;
}

export interface OrderStatusHistoryItem {
  previousStatus?: string;
  newStatus: string;
  reason?: string;
  changedBy: string;
  changedAt: string;
}

export interface SalesOrder {
  id: string;
  orderNumber: string;
  customerId: string;
  customerName: string;
  customerEmail: string;
  customerPhone: string;
  orderDate: string;
  status: 'Draft' | 'PendingConfirmation' | 'Confirmed' | 'Processing' | 'Dispatched' | 'Delivered' | 'Cancelled' | 'Returned';
  paymentStatus: 'Pending' | 'Paid' | 'Failed' | 'Refunded';
  subtotal: number;
  totalDiscount: number;
  taxAmount: number;
  shippingCharges: number;
  grandTotal: number;
  shippingAddress?: string;
  billingAddress?: string;
  notes?: string;
  idempotencyKey?: string;
  createdBy: string;
  createdAt: string;
  updatedAt: string;
  items: OrderItem[];
  statusHistory: OrderStatusHistoryItem[];
}

export interface Customer {
  id: string;
  customerCode: string;
  name: string;
  email: string;
  phone: string;
  companyName?: string;
  taxNumber?: string;
  creditLimit: number;
  outstandingBalance: number;
  isActive: boolean;
  orderCount: number;
  addresses?: Array<{
    id: string;
    line1: string;
    city: string;
    state: string;
    postalCode: string;
  }>;
}

export interface Supplier {
  id: string;
  code: string;
  name: string;
  contactPerson?: string;
  email: string;
  phone?: string;
  address?: string;
  paymentTerms?: string;
  isActive: boolean;
  productCount: number;
}

export interface PurchaseOrder {
  id: string;
  orderNumber: string;
  supplierId: string;
  supplierName: string;
  warehouseId: string;
  warehouseName: string;
  orderDate: string;
  expectedDeliveryDate?: string;
  status: string;
  totalAmount: number;
  createdBy: string;
  notes?: string;
  items: Array<{
    id: string;
    productId: string;
    productSku: string;
    productName: string;
    quantityOrdered: number;
    quantityReceived: number;
    unitCost: number;
    lineTotal: number;
  }>;
}

export interface SalesReport {
  periodStart: string;
  periodEnd: string;
  totalOrders: number;
  totalRevenue: number;
  totalDiscount: number;
  totalTax: number;
  averageOrderValue: number;
  dailySales: Array<{ date: string; orderCount: number; totalSales: number }>;
  topProducts: Array<{ productId: string; sku: string; productName: string; unitsSold: number; revenueGenerated: number }>;
  ordersByStatus: Array<{ status: string; count: number; totalAmount: number }>;
}

export interface InventoryValuation {
  totalActiveProducts: number;
  totalUnitsOnHand: number;
  totalUnitsReserved: number;
  totalUnitsAvailable: number;
  totalCostValuation: number;
  totalRetailValuation: number;
  lowStockProductCount: number;
  outOfStockProductCount: number;
  warehouseBreakdown: Array<{ warehouseId: string; warehouseName: string; totalUnits: number; totalCostValue: number }>;
}

export interface ChatMessage {
  id: string;
  sender: 'user' | 'bot' | 'system';
  text: string;
  intent?: string;
  awaitingConfirmation?: boolean;
  orderSummary?: {
    subtotal: number;
    tax_amount: number;
    tax_rate: number;
    shipping_charges: number;
    grand_total: number;
    all_items_in_stock: boolean;
    items: Array<{
      product_id: string;
      sku: string;
      name: string;
      quantity: number;
      unit_price: number;
      line_total: number;
      available_stock: number;
      has_sufficient_stock: boolean;
    }>;
  };
  orderNumber?: string;
  suggestions?: string[];
  timestamp: Date;
}
