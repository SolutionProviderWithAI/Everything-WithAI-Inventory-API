const API_BASE = (import.meta as any).env?.VITE_API_BASE_URL || 'http://localhost:5000/api';
const CHATBOT_BASE = (import.meta as any).env?.VITE_CHATBOT_BASE_URL || 'http://localhost:8000';

function getHeaders(customHeaders: HeadersInit = {}): HeadersInit {
  const token = localStorage.getItem('token');
  const headers: Record<string, string> = {
    'Content-Type': 'application/json',
    ...customHeaders as Record<string, string>
  };
  if (token) {
    headers['Authorization'] = `Bearer ${token}`;
  }
  return headers;
}

async function handleResponse<T>(res: Response): Promise<T> {
  if (!res.ok) {
    let errorDetail = 'API request failed';
    try {
      const errJson = await res.json();
      errorDetail = errJson.detail || errJson.title || JSON.stringify(errJson);
    } catch {
      errorDetail = await res.text() || res.statusText;
    }
    throw new Error(errorDetail);
  }
  if (res.status === 204) {
    return {} as T;
  }
  return res.json();
}

export const api = {
  // Auth
  login: (credentials: any) =>
    fetch(`${API_BASE}/auth/login`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(credentials)
    }).then(handleResponse<any>),

  getMe: () =>
    fetch(`${API_BASE}/auth/me`, {
      headers: getHeaders()
    }).then(handleResponse<any>),

  getUsers: () =>
    fetch(`${API_BASE}/auth/users`, {
      headers: getHeaders()
    }).then(handleResponse<any[]>),

  // Products
  getProducts: (query?: string, categoryId?: string, page = 1, pageSize = 50) => {
    const params = new URLSearchParams({ page: page.toString(), pageSize: pageSize.toString() });
    if (query) params.append('query', query);
    if (categoryId) params.append('categoryId', categoryId);
    return fetch(`${API_BASE}/products?${params}`, { headers: getHeaders() }).then(handleResponse<any>);
  },

  getProduct: (id: string) =>
    fetch(`${API_BASE}/products/${id}`, { headers: getHeaders() }).then(handleResponse<any>),

  createProduct: (data: any) =>
    fetch(`${API_BASE}/products`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(data)
    }).then(handleResponse<any>),

  updateProduct: (id: string, data: any) =>
    fetch(`${API_BASE}/products/${id}`, {
      method: 'PUT',
      headers: getHeaders(),
      body: JSON.stringify(data)
    }).then(handleResponse<any>),

  deactivateProduct: (id: string) =>
    fetch(`${API_BASE}/products/${id}`, {
      method: 'DELETE',
      headers: getHeaders()
    }).then(handleResponse<any>),

  // Categories
  getCategories: () =>
    fetch(`${API_BASE}/categories`, { headers: getHeaders() }).then(handleResponse<any[]>),

  createCategory: (data: any) =>
    fetch(`${API_BASE}/categories`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(data)
    }).then(handleResponse<any>),

  // Inventory
  getProductStock: (productId: string) =>
    fetch(`${API_BASE}/inventory/${productId}`, { headers: getHeaders() }).then(handleResponse<any[]>),

  getLowStockAlerts: () =>
    fetch(`${API_BASE}/inventory/low-stock`, { headers: getHeaders() }).then(handleResponse<any[]>),

  getStockMovements: (page = 1, pageSize = 50, productId?: string) => {
    const params = new URLSearchParams({ page: page.toString(), pageSize: pageSize.toString() });
    if (productId) params.append('productId', productId);
    return fetch(`${API_BASE}/inventory/movements?${params}`, { headers: getHeaders() }).then(handleResponse<any>);
  },

  adjustStock: (data: any) =>
    fetch(`${API_BASE}/inventory/adjustments`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(data)
    }).then(handleResponse<any>),

  receiveStock: (data: any) =>
    fetch(`${API_BASE}/inventory/receipts`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(data)
    }).then(handleResponse<any>),

  // Orders
  getOrders: (status?: string, page = 1, pageSize = 50) => {
    const params = new URLSearchParams({ page: page.toString(), pageSize: pageSize.toString() });
    if (status) params.append('status', status);
    return fetch(`${API_BASE}/orders?${params}`, { headers: getHeaders() }).then(handleResponse<any>);
  },

  getOrder: (id: string) =>
    fetch(`${API_BASE}/orders/${id}`, { headers: getHeaders() }).then(handleResponse<any>),

  calculateOrder: (data: any) =>
    fetch(`${API_BASE}/orders/calculate`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(data)
    }).then(handleResponse<any>),

  createOrder: (data: any) =>
    fetch(`${API_BASE}/orders`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(data)
    }).then(handleResponse<any>),

  confirmOrder: (id: string) =>
    fetch(`${API_BASE}/orders/${id}/confirm`, {
      method: 'POST',
      headers: getHeaders()
    }).then(handleResponse<any>),

  dispatchOrder: (id: string) =>
    fetch(`${API_BASE}/orders/${id}/dispatch`, {
      method: 'POST',
      headers: getHeaders()
    }).then(handleResponse<any>),

  deliverOrder: (id: string) =>
    fetch(`${API_BASE}/orders/${id}/deliver`, {
      method: 'POST',
      headers: getHeaders()
    }).then(handleResponse<any>),

  cancelOrder: (id: string, reason?: string) =>
    fetch(`${API_BASE}/orders/${id}/cancel`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify({ reason: reason || 'Customer requested cancellation' })
    }).then(handleResponse<any>),

  // Customers
  getCustomers: () =>
    fetch(`${API_BASE}/customers`, { headers: getHeaders() }).then(handleResponse<any[]>),

  createCustomer: (data: any) =>
    fetch(`${API_BASE}/customers`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(data)
    }).then(handleResponse<any>),

  // Suppliers & Purchase Orders
  getSuppliers: () =>
    fetch(`${API_BASE}/suppliers`, { headers: getHeaders() }).then(handleResponse<any[]>),

  createSupplier: (data: any) =>
    fetch(`${API_BASE}/suppliers`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(data)
    }).then(handleResponse<any>),

  getPurchaseOrders: () =>
    fetch(`${API_BASE}/purchaseorders`, { headers: getHeaders() }).then(handleResponse<any[]>),

  createPurchaseOrder: (data: any) =>
    fetch(`${API_BASE}/purchaseorders`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(data)
    }).then(handleResponse<any>),

  receivePurchaseOrder: (id: string, items: any[]) =>
    fetch(`${API_BASE}/purchaseorders/${id}/receive`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify({ purchaseOrderId: id, items })
    }).then(handleResponse<any>),

  // Reports
  getSalesReport: (startDate?: string, endDate?: string) => {
    const params = new URLSearchParams();
    if (startDate) params.append('startDate', startDate);
    if (endDate) params.append('endDate', endDate);
    return fetch(`${API_BASE}/reports/sales?${params}`, { headers: getHeaders() }).then(handleResponse<any>);
  },

  getInventoryValuation: () =>
    fetch(`${API_BASE}/reports/inventory`, { headers: getHeaders() }).then(handleResponse<any>),

  exportSalesCsv: (startDate?: string, endDate?: string) => {
    const params = new URLSearchParams();
    if (startDate) params.append('startDate', startDate);
    if (endDate) params.append('endDate', endDate);
    return fetch(`${API_BASE}/reports/export/sales-csv?${params}`, { headers: getHeaders() })
      .then(res => res.blob());
  },

  exportInventoryCsv: () => {
    return fetch(`${API_BASE}/reports/export/inventory-csv`, { headers: getHeaders() })
      .then(res => res.blob());
  },

  // Chatbot conversations transcript
  getChatConversations: () =>
    fetch(`${API_BASE}/chatbot/conversations`, { headers: getHeaders() }).then(handleResponse<any[]>),

  getChatMessages: (sessionId: string) =>
    fetch(`${API_BASE}/chatbot/conversations/${sessionId}/messages`, { headers: getHeaders() }).then(handleResponse<any[]>),

  // Settings
  getSettings: () =>
    fetch(`${API_BASE}/settings`, { headers: getHeaders() }).then(handleResponse<any[]>),

  updateSetting: (key: string, value: string, description?: string) =>
    fetch(`${API_BASE}/settings/${key}`, {
      method: 'PUT',
      headers: getHeaders(),
      body: JSON.stringify({ settingValue: value, description })
    }).then(handleResponse<any>),

  // Audit Logs
  getAuditLogs: (page = 1, pageSize = 50) =>
    fetch(`${API_BASE}/auditlogs?page=${page}&pageSize=${pageSize}`, { headers: getHeaders() }).then(handleResponse<any>),

  // Health checks
  checkBackendHealth: () =>
    fetch(`${API_BASE.replace('/api', '')}/health`).then(res => res.ok).catch(() => false),

  checkChatbotHealth: () =>
    fetch(`${CHATBOT_BASE}/health`).then(res => res.ok).catch(() => false)
};

// Chatbot Direct Service
export const chatbotService = {
  sendMessage: (sessionId: string, message: string, customerId?: string, customerName?: string, customerPhone?: string, deliveryAddress?: string) =>
    fetch(`${CHATBOT_BASE}/chat/message`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        session_id: sessionId,
        message,
        customer_id: customerId,
        customer_name: customerName,
        customer_phone: customerPhone,
        delivery_address: deliveryAddress
      })
    }).then(handleResponse<any>),

  resetSession: (sessionId: string) =>
    fetch(`${CHATBOT_BASE}/chat/session/reset`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ session_id: sessionId })
    }).then(handleResponse<any>)
};
