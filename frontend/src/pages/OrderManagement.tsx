import React, { useEffect, useState } from 'react';
import {
  ShoppingCart,
  Search,
  Filter,
  Eye,
  Truck,
  CheckCircle,
  XCircle,
  AlertCircle,
  Clock,
  MapPin,
  RefreshCw
} from 'lucide-react';
import { api } from '../services/api';
import { Modal } from '../components/StatCard';
import { SalesOrder } from '../types';

export const OrderManagement: React.FC = () => {
  const [orders, setOrders] = useState<SalesOrder[]>([]);
  const [statusFilter, setStatusFilter] = useState<string>('');
  const [selectedOrder, setSelectedOrder] = useState<SalesOrder | null>(null);
  const [isDetailModalOpen, setIsDetailModalOpen] = useState(false);
  const [feedback, setFeedback] = useState<{ type: 'success' | 'error'; message: string } | null>(null);
  const [loading, setLoading] = useState(true);

  const loadOrders = async () => {
    setLoading(true);
    try {
      const res = await api.getOrders(statusFilter || undefined, 1, 50);
      setOrders(res.items || []);
    } catch (e: any) {
      console.error(e);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    loadOrders();
  }, [statusFilter]);

  const handleConfirmOrder = async (orderId: string) => {
    try {
      await api.confirmOrder(orderId);
      setFeedback({ type: 'success', message: 'Order confirmed successfully.' });
      await loadOrders();
      if (selectedOrder?.id === orderId) {
        const updated = await api.getOrder(orderId);
        setSelectedOrder(updated);
      }
    } catch (err: any) {
      setFeedback({ type: 'error', message: err.message });
    }
  };

  const handleDispatchOrder = async (orderId: string) => {
    try {
      await api.dispatchOrder(orderId);
      setFeedback({
        type: 'success',
        message: 'Order dispatched successfully. Physical stock has been deducted from warehouse.'
      });
      await loadOrders();
      if (selectedOrder?.id === orderId) {
        const updated = await api.getOrder(orderId);
        setSelectedOrder(updated);
      }
    } catch (err: any) {
      setFeedback({ type: 'error', message: err.message });
    }
  };

  const handleDeliverOrder = async (orderId: string) => {
    try {
      await api.deliverOrder(orderId);
      setFeedback({ type: 'success', message: 'Order marked as Delivered.' });
      await loadOrders();
      if (selectedOrder?.id === orderId) {
        const updated = await api.getOrder(orderId);
        setSelectedOrder(updated);
      }
    } catch (err: any) {
      setFeedback({ type: 'error', message: err.message });
    }
  };

  const handleCancelOrder = async (orderId: string) => {
    const reason = prompt('Please enter cancellation reason:', 'Customer requested cancellation');
    if (!reason) return;

    try {
      await api.cancelOrder(orderId, reason);
      setFeedback({
        type: 'success',
        message: 'Order cancelled. Inventory reservations have been released back into available stock.'
      });
      await loadOrders();
      if (selectedOrder?.id === orderId) {
        const updated = await api.getOrder(orderId);
        setSelectedOrder(updated);
      }
    } catch (err: any) {
      setFeedback({ type: 'error', message: err.message });
    }
  };

  return (
    <div className="space-y-6">
      {/* Header and Controls */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4 bg-white p-5 rounded-2xl border border-slate-200 shadow-xs">
        <div>
          <h2 className="text-lg font-bold text-slate-900">Sales Order Management</h2>
          <p className="text-xs text-slate-500">
            Lifecycle: Draft → Confirmed (Reserved) → Dispatched (Stock Deducted) → Delivered
          </p>
        </div>

        <button onClick={loadOrders} className="btn btn-secondary text-xs flex items-center gap-1.5">
          <RefreshCw size={14} />
          Refresh
        </button>
      </div>

      {feedback && (
        <div
          className={`p-4 rounded-xl border flex items-center gap-2.5 text-xs ${
            feedback.type === 'success'
              ? 'bg-emerald-50 border-emerald-200 text-emerald-800'
              : 'bg-rose-50 border-rose-200 text-rose-800'
          }`}
        >
          {feedback.type === 'success' ? <CheckCircle size={16} /> : <AlertCircle size={16} />}
          <span>{feedback.message}</span>
        </div>
      )}

      {/* Filter Tabs */}
      <div className="flex flex-wrap gap-2">
        {['', 'Confirmed', 'Processing', 'Dispatched', 'Delivered', 'Cancelled'].map((st) => (
          <button
            key={st}
            onClick={() => setStatusFilter(st)}
            className={`px-3.5 py-1.5 rounded-lg text-xs font-semibold transition ${
              statusFilter === st
                ? 'bg-blue-600 text-white shadow-xs'
                : 'bg-white text-slate-600 border border-slate-200 hover:bg-slate-50'
            }`}
          >
            {st || 'All Orders'}
          </button>
        ))}
      </div>

      {/* Orders Table */}
      <div className="bg-white rounded-2xl border border-slate-200 shadow-xs overflow-hidden">
        <div className="table-container border-0">
          <table>
            <thead>
              <tr>
                <th>Order Number</th>
                <th>Customer</th>
                <th>Order Date</th>
                <th>Items Count</th>
                <th>Grand Total</th>
                <th>Order Status</th>
                <th>Payment</th>
                <th className="text-right">Fulfillment Actions</th>
              </tr>
            </thead>
            <tbody>
              {orders.length === 0 ? (
                <tr>
                  <td colSpan={8} className="text-center py-8 text-slate-400 text-xs">
                    No orders found matching status filter.
                  </td>
                </tr>
              ) : (
                orders.map((ord) => (
                  <tr key={ord.id}>
                    <td className="font-mono text-xs font-bold text-blue-600">{ord.orderNumber}</td>
                    <td>
                      <div className="font-semibold text-slate-800">{ord.customerName}</div>
                      <div className="text-[11px] text-slate-400">{ord.customerPhone}</div>
                    </td>
                    <td className="text-xs text-slate-500 whitespace-nowrap">
                      {new Date(ord.orderDate).toLocaleString()}
                    </td>
                    <td className="text-xs font-medium text-slate-700">{ord.items.length} item(s)</td>
                    <td className="font-bold text-slate-900">₹{ord.grandTotal.toFixed(2)}</td>
                    <td>
                      <span
                        className={`badge ${
                          ord.status === 'Confirmed'
                            ? 'badge-primary'
                            : ord.status === 'Dispatched'
                            ? 'badge-warning'
                            : ord.status === 'Delivered'
                            ? 'badge-success'
                            : ord.status === 'Cancelled'
                            ? 'badge-danger'
                            : 'badge-neutral'
                        }`}
                      >
                        {ord.status}
                      </span>
                    </td>
                    <td>
                      <span className="badge badge-neutral text-[10px]">{ord.paymentStatus}</span>
                    </td>
                    <td className="text-right">
                      <div className="flex items-center justify-end gap-1.5">
                        <button
                          onClick={() => {
                            setSelectedOrder(ord);
                            setIsDetailModalOpen(true);
                          }}
                          className="btn btn-secondary btn-sm"
                          title="View Details"
                        >
                          <Eye size={13} />
                          Details
                        </button>

                        {ord.status === 'Confirmed' && (
                          <button
                            onClick={() => handleDispatchOrder(ord.id)}
                            className="btn btn-primary btn-sm flex items-center gap-1"
                            title="Dispatch and deduct stock"
                          >
                            <Truck size={13} />
                            Dispatch
                          </button>
                        )}

                        {ord.status === 'Dispatched' && (
                          <button
                            onClick={() => handleDeliverOrder(ord.id)}
                            className="btn btn-success btn-sm flex items-center gap-1"
                            title="Mark Delivered"
                          >
                            <CheckCircle size={13} />
                            Deliver
                          </button>
                        )}

                        {ord.status !== 'Dispatched' && ord.status !== 'Delivered' && ord.status !== 'Cancelled' && (
                          <button
                            onClick={() => handleCancelOrder(ord.id)}
                            className="btn btn-danger btn-sm"
                            title="Cancel & Release Reserved Stock"
                          >
                            Cancel
                          </button>
                        )}
                      </div>
                    </td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>
      </div>

      {/* Order Detail Modal */}
      {selectedOrder && (
        <Modal
          isOpen={isDetailModalOpen}
          onClose={() => setIsDetailModalOpen(false)}
          title={`Order Details: ${selectedOrder.orderNumber}`}
        >
          <div className="space-y-4 text-xs">
            {/* Summary strip */}
            <div className="flex justify-between items-center p-3 bg-slate-50 rounded-xl border border-slate-200">
              <div>
                <p className="text-slate-500">Order Status</p>
                <p className="font-bold text-sm text-slate-900 mt-0.5">{selectedOrder.status}</p>
              </div>
              <div>
                <p className="text-slate-500">Payment</p>
                <p className="font-semibold text-slate-800 mt-0.5">{selectedOrder.paymentStatus}</p>
              </div>
              <div className="text-right">
                <p className="text-slate-500">Grand Total</p>
                <p className="font-bold text-base text-blue-600 mt-0.5">₹{selectedOrder.grandTotal.toFixed(2)}</p>
              </div>
            </div>

            {/* Customer & Address */}
            <div className="grid grid-cols-2 gap-3 p-3 bg-white border border-slate-200 rounded-xl">
              <div>
                <p className="font-bold text-slate-800">Customer Details</p>
                <p className="text-slate-600 mt-1">{selectedOrder.customerName}</p>
                <p className="text-slate-500">{selectedOrder.customerEmail}</p>
                <p className="text-slate-500">{selectedOrder.customerPhone}</p>
              </div>
              <div>
                <p className="font-bold text-slate-800 flex items-center gap-1">
                  <MapPin size={13} className="text-slate-400" />
                  Delivery Address
                </p>
                <p className="text-slate-600 mt-1">{selectedOrder.shippingAddress || 'Standard Address'}</p>
              </div>
            </div>

            {/* Items Table */}
            <div>
              <p className="font-bold text-slate-800 mb-2">Order Line Items (Fixed Price Snapshots)</p>
              <div className="table-container">
                <table>
                  <thead>
                    <tr>
                      <th>Product</th>
                      <th>SKU</th>
                      <th>Quantity</th>
                      <th>Unit Price</th>
                      <th>Total</th>
                    </tr>
                  </thead>
                  <tbody>
                    {selectedOrder.items.map((it) => (
                      <tr key={it.id}>
                        <td className="font-medium text-slate-800">{it.productName}</td>
                        <td className="font-mono text-slate-500">{it.sku}</td>
                        <td className="font-bold text-slate-900">{it.quantity}</td>
                        <td className="text-slate-600">₹{it.unitPrice.toFixed(2)}</td>
                        <td className="font-bold text-slate-900">₹{it.lineTotal.toFixed(2)}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </div>

            {/* Price Calculations breakdown */}
            <div className="p-3 bg-slate-50 rounded-xl border border-slate-200 space-y-1 text-slate-600">
              <div className="flex justify-between">
                <span>Items Subtotal</span>
                <span>₹{selectedOrder.subtotal.toFixed(2)}</span>
              </div>
              <div className="flex justify-between">
                <span>Tax Amount (5% GST)</span>
                <span>₹{selectedOrder.taxAmount.toFixed(2)}</span>
              </div>
              <div className="flex justify-between">
                <span>Shipping Charges</span>
                <span className={selectedOrder.shippingCharges === 0 ? 'text-emerald-600 font-semibold' : ''}>
                  {selectedOrder.shippingCharges === 0 ? 'FREE' : `₹${selectedOrder.shippingCharges.toFixed(2)}`}
                </span>
              </div>
              <div className="flex justify-between font-bold text-slate-900 text-sm pt-1 border-t border-slate-200">
                <span>Grand Total</span>
                <span className="text-blue-600">₹{selectedOrder.grandTotal.toFixed(2)}</span>
              </div>
            </div>

            {/* Status History Timeline */}
            <div>
              <p className="font-bold text-slate-800 mb-2 flex items-center gap-1.5">
                <Clock size={14} className="text-slate-400" />
                Status Change Audit History
              </p>
              <div className="space-y-2 border-l-2 border-slate-200 pl-3">
                {selectedOrder.statusHistory.map((h, idx) => (
                  <div key={idx} className="relative">
                    <span className="font-semibold text-slate-800">{h.newStatus}</span>
                    <span className="text-slate-400 text-[11px] ml-2">{new Date(h.changedAt).toLocaleString()}</span>
                    {h.reason && <p className="text-slate-500 text-[11px]">{h.reason}</p>}
                    <p className="text-slate-400 text-[10px]">By: {h.changedBy}</p>
                  </div>
                ))}
              </div>
            </div>

            {/* Modal Actions */}
            <div className="flex justify-end gap-2 pt-3 border-t border-slate-100">
              {selectedOrder.status === 'Confirmed' && (
                <button
                  type="button"
                  onClick={() => handleDispatchOrder(selectedOrder.id)}
                  className="btn btn-primary text-xs flex items-center gap-1"
                >
                  <Truck size={14} />
                  Dispatch Order & Deduct Physical Stock
                </button>
              )}
              {selectedOrder.status !== 'Dispatched' && selectedOrder.status !== 'Delivered' && selectedOrder.status !== 'Cancelled' && (
                <button
                  type="button"
                  onClick={() => handleCancelOrder(selectedOrder.id)}
                  className="btn btn-danger text-xs"
                >
                  Cancel Order & Release Reservation
                </button>
              )}
            </div>
          </div>
        </Modal>
      )}
    </div>
  );
};
