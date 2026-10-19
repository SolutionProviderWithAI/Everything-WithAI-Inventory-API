import React, { useEffect, useState } from 'react';
import { Truck, Plus, Search, RefreshCw, PackageCheck, Mail, Phone, Building, Calendar, CheckCircle2, Clock } from 'lucide-react';
import { api } from '../services/api';
import { Supplier, PurchaseOrder, Product } from '../types';

export const SupplierManagement: React.FC = () => {
  const [activeTab, setActiveTab] = useState<'suppliers' | 'orders'>('suppliers');
  const [suppliers, setSuppliers] = useState<Supplier[]>([]);
  const [purchaseOrders, setPurchaseOrders] = useState<PurchaseOrder[]>([]);
  const [products, setProducts] = useState<Product[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  // Modals
  const [isAddSupplierOpen, setIsAddSupplierOpen] = useState(false);
  const [isCreatePOOpen, setIsCreatePOOpen] = useState(false);
  const [receivingPO, setReceivingPO] = useState<PurchaseOrder | null>(null);
  const [submitting, setSubmitting] = useState(false);

  // Form State - New Supplier
  const [newSupplier, setNewSupplier] = useState({
    name: '',
    code: '',
    contactPerson: '',
    email: '',
    phone: '',
    address: '',
    paymentTerms: 'Net 30'
  });

  // Form State - New PO
  const [newPO, setNewPO] = useState({
    supplierId: '',
    warehouseId: 'b0000000-0000-0000-0000-000000000001', // Default Central Hub
    expectedDeliveryDays: 7,
    notes: '',
    items: [{ productId: '', quantityOrdered: 50, unitCost: 100 }]
  });

  // Receive items state
  const [receivedQuantities, setReceivedQuantities] = useState<Record<string, number>>({});

  const loadData = async () => {
    try {
      setLoading(true);
      setError(null);
      const [supData, poData, prodData] = await Promise.all([
        api.getSuppliers(),
        api.getPurchaseOrders(),
        api.getProducts('', undefined, 1, 100)
      ]);
      setSuppliers(supData);
      setPurchaseOrders(poData);
      setProducts(prodData.items || []);
    } catch (err: any) {
      setError(err.message || 'Failed to load supplier data');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    loadData();
  }, []);

  const handleAddSupplier = async (e: React.FormEvent) => {
    e.preventDefault();
    try {
      setSubmitting(true);
      setError(null);
      await api.createSupplier(newSupplier);
      setIsAddSupplierOpen(false);
      setNewSupplier({
        name: '',
        code: '',
        contactPerson: '',
        email: '',
        phone: '',
        address: '',
        paymentTerms: 'Net 30'
      });
      await loadData();
    } catch (err: any) {
      setError(err.message || 'Failed to create supplier');
    } finally {
      setSubmitting(false);
    }
  };

  const handleCreatePO = async (e: React.FormEvent) => {
    e.preventDefault();
    try {
      setSubmitting(true);
      setError(null);

      const expectedDate = new Date();
      expectedDate.setDate(expectedDate.getDate() + Number(newPO.expectedDeliveryDays));

      const payload = {
        supplierId: newPO.supplierId,
        warehouseId: newPO.warehouseId,
        expectedDeliveryDate: expectedDate.toISOString(),
        notes: newPO.notes || undefined,
        items: newPO.items.map(item => ({
          productId: item.productId,
          quantityOrdered: Number(item.quantityOrdered),
          unitCost: Number(item.unitCost)
        }))
      };

      await api.createPurchaseOrder(payload);
      setIsCreatePOOpen(false);
      setNewPO({
        supplierId: '',
        warehouseId: 'b0000000-0000-0000-0000-000000000001',
        expectedDeliveryDays: 7,
        notes: '',
        items: [{ productId: '', quantityOrdered: 50, unitCost: 100 }]
      });
      await loadData();
    } catch (err: any) {
      setError(err.message || 'Failed to create purchase order');
    } finally {
      setSubmitting(false);
    }
  };

  const handleOpenReceiveModal = (po: PurchaseOrder) => {
    setReceivingPO(po);
    const initial: Record<string, number> = {};
    po.items.forEach(item => {
      const remaining = Math.max(0, item.quantityOrdered - (item.quantityReceived || 0));
      initial[item.productId] = remaining;
    });
    setReceivedQuantities(initial);
  };

  const handleConfirmReceive = async () => {
    if (!receivingPO) return;
    try {
      setSubmitting(true);
      setError(null);

      const itemsPayload = Object.entries(receivedQuantities)
        .filter(([_, qty]) => qty > 0)
        .map(([productId, quantityReceived]) => ({
          productId,
          quantityReceived: Number(quantityReceived)
        }));

      if (itemsPayload.length === 0) {
        alert('Please specify at least one quantity received > 0');
        return;
      }

      await api.receivePurchaseOrder(receivingPO.id, itemsPayload);
      setReceivingPO(null);
      await loadData();
    } catch (err: any) {
      setError(err.message || 'Failed to receive stock from purchase order');
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <div className="space-y-6">
      {/* Page Header */}
      <div className="flex flex-col sm:flex-row justify-between items-start sm:items-center gap-4">
        <div>
          <h2 className="text-xl font-bold text-slate-900 flex items-center gap-2">
            <Truck className="text-blue-600" size={24} />
            Suppliers & Inbound Procurement
          </h2>
          <p className="text-sm text-slate-500">
            Manage vendor relationships, issue purchase orders, and receive replenishments into stock.
          </p>
        </div>

        <div className="flex items-center gap-3">
          <button
            onClick={loadData}
            className="flex items-center gap-1.5 px-3 py-2 text-sm font-medium text-slate-600 bg-white border border-slate-200 rounded-lg hover:bg-slate-50 shadow-xs"
          >
            <RefreshCw size={15} />
            Refresh
          </button>
          {activeTab === 'suppliers' ? (
            <button
              onClick={() => setIsAddSupplierOpen(true)}
              className="flex items-center gap-2 px-4 py-2 text-sm font-semibold text-white bg-blue-600 rounded-lg hover:bg-blue-700 shadow-sm"
            >
              <Plus size={16} />
              Add Supplier
            </button>
          ) : (
            <button
              onClick={() => setIsCreatePOOpen(true)}
              className="flex items-center gap-2 px-4 py-2 text-sm font-semibold text-white bg-blue-600 rounded-lg hover:bg-blue-700 shadow-sm"
            >
              <Plus size={16} />
              New Purchase Order
            </button>
          )}
        </div>
      </div>

      {error && (
        <div className="p-4 bg-rose-50 border border-rose-200 text-rose-700 rounded-lg text-sm">
          {error}
        </div>
      )}

      {/* Tabs */}
      <div className="flex border-b border-slate-200 bg-white px-4 rounded-t-xl">
        <button
          onClick={() => setActiveTab('suppliers')}
          className={`py-3 px-4 text-sm font-semibold border-b-2 transition ${
            activeTab === 'suppliers'
              ? 'border-blue-600 text-blue-600'
              : 'border-transparent text-slate-500 hover:text-slate-800'
          }`}
        >
          Suppliers Directory ({suppliers.length})
        </button>
        <button
          onClick={() => setActiveTab('orders')}
          className={`py-3 px-4 text-sm font-semibold border-b-2 transition ${
            activeTab === 'orders'
              ? 'border-blue-600 text-blue-600'
              : 'border-transparent text-slate-500 hover:text-slate-800'
          }`}
        >
          Purchase Orders ({purchaseOrders.length})
        </button>
      </div>

      {/* Suppliers Tab Content */}
      {activeTab === 'suppliers' && (
        <div className="bg-white rounded-b-xl border border-slate-200 shadow-xs overflow-hidden">
          <div className="overflow-x-auto">
            <table className="w-full text-left text-sm text-slate-600">
              <thead className="bg-slate-50 text-slate-500 text-xs font-semibold uppercase tracking-wider border-b border-slate-200">
                <tr>
                  <th className="px-5 py-3">Code</th>
                  <th className="px-5 py-3">Supplier Name</th>
                  <th className="px-5 py-3">Contact</th>
                  <th className="px-5 py-3">Payment Terms</th>
                  <th className="px-5 py-3 text-center">Catalog Items</th>
                  <th className="px-5 py-3 text-center">Status</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-100">
                {loading ? (
                  <tr>
                    <td colSpan={6} className="px-5 py-8 text-center text-slate-400">
                      Loading suppliers...
                    </td>
                  </tr>
                ) : suppliers.length === 0 ? (
                  <tr>
                    <td colSpan={6} className="px-5 py-8 text-center text-slate-400">
                      No suppliers registered.
                    </td>
                  </tr>
                ) : (
                  suppliers.map((sup) => (
                    <tr key={sup.id} className="hover:bg-slate-50/80 transition">
                      <td className="px-5 py-4 font-mono font-medium text-slate-800">
                        {sup.code}
                      </td>
                      <td className="px-5 py-4">
                        <div className="font-semibold text-slate-900">{sup.name}</div>
                        {sup.contactPerson && (
                          <div className="text-xs text-slate-500">Contact: {sup.contactPerson}</div>
                        )}
                      </td>
                      <td className="px-5 py-4">
                        <div className="flex items-center gap-1.5 text-xs text-slate-600">
                          <Mail size={12} className="text-slate-400" />
                          {sup.email}
                        </div>
                        {sup.phone && (
                          <div className="flex items-center gap-1.5 text-xs text-slate-500 mt-1">
                            <Phone size={12} className="text-slate-400" />
                            {sup.phone}
                          </div>
                        )}
                      </td>
                      <td className="px-5 py-4 font-medium text-slate-700">
                        {sup.paymentTerms || 'Net 30'}
                      </td>
                      <td className="px-5 py-4 text-center font-semibold text-slate-700">
                        {sup.productCount || 0}
                      </td>
                      <td className="px-5 py-4 text-center">
                        <span className="inline-flex items-center gap-1 px-2.5 py-0.5 rounded-full text-xs font-medium bg-emerald-50 text-emerald-700 border border-emerald-200">
                          Active
                        </span>
                      </td>
                    </tr>
                  ))
                )}
              </tbody>
            </table>
          </div>
        </div>
      )}

      {/* Purchase Orders Tab Content */}
      {activeTab === 'orders' && (
        <div className="bg-white rounded-b-xl border border-slate-200 shadow-xs overflow-hidden">
          <div className="overflow-x-auto">
            <table className="w-full text-left text-sm text-slate-600">
              <thead className="bg-slate-50 text-slate-500 text-xs font-semibold uppercase tracking-wider border-b border-slate-200">
                <tr>
                  <th className="px-5 py-3">PO Number</th>
                  <th className="px-5 py-3">Supplier</th>
                  <th className="px-5 py-3">Warehouse</th>
                  <th className="px-5 py-3">Order Date</th>
                  <th className="px-5 py-3 text-right">Total Amount</th>
                  <th className="px-5 py-3 text-center">Status</th>
                  <th className="px-5 py-3 text-right">Actions</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-100">
                {loading ? (
                  <tr>
                    <td colSpan={7} className="px-5 py-8 text-center text-slate-400">
                      Loading purchase orders...
                    </td>
                  </tr>
                ) : purchaseOrders.length === 0 ? (
                  <tr>
                    <td colSpan={7} className="px-5 py-8 text-center text-slate-400">
                      No purchase orders issued yet.
                    </td>
                  </tr>
                ) : (
                  purchaseOrders.map((po) => {
                    const isFullyReceived = po.status === 'Received';
                    return (
                      <tr key={po.id} className="hover:bg-slate-50/80 transition">
                        <td className="px-5 py-4 font-mono font-bold text-blue-700">
                          {po.orderNumber}
                        </td>
                        <td className="px-5 py-4 font-semibold text-slate-800">
                          {po.supplierName}
                        </td>
                        <td className="px-5 py-4 text-xs text-slate-600">
                          {po.warehouseName}
                        </td>
                        <td className="px-5 py-4 text-xs text-slate-600">
                          {new Date(po.orderDate).toLocaleDateString()}
                        </td>
                        <td className="px-5 py-4 text-right font-bold text-slate-900">
                          ₹{po.totalAmount?.toLocaleString('en-IN') || '0'}
                        </td>
                        <td className="px-5 py-4 text-center">
                          <span className={`inline-flex items-center gap-1 px-2.5 py-0.5 rounded-full text-xs font-semibold ${
                            isFullyReceived
                              ? 'bg-emerald-50 text-emerald-700 border border-emerald-200'
                              : 'bg-amber-50 text-amber-700 border border-amber-200'
                          }`}>
                            {isFullyReceived ? <CheckCircle2 size={12} /> : <Clock size={12} />}
                            {po.status}
                          </span>
                        </td>
                        <td className="px-5 py-4 text-right">
                          {!isFullyReceived && (
                            <button
                              onClick={() => handleOpenReceiveModal(po)}
                              className="text-xs font-semibold text-emerald-700 hover:text-emerald-800 bg-emerald-50 hover:bg-emerald-100 px-3 py-1.5 rounded-md transition flex items-center gap-1 ml-auto"
                            >
                              <PackageCheck size={14} />
                              Receive Stock
                            </button>
                          )}
                        </td>
                      </tr>
                    );
                  })
                )}
              </tbody>
            </table>
          </div>
        </div>
      )}

      {/* Add Supplier Modal */}
      {isAddSupplierOpen && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50 p-4">
          <div className="bg-white rounded-xl shadow-xl w-full max-w-md overflow-hidden">
            <div className="px-6 py-4 border-b border-slate-200 flex justify-between items-center bg-slate-50">
              <h3 className="font-bold text-slate-800 text-base">Add New Supplier</h3>
              <button
                onClick={() => setIsAddSupplierOpen(false)}
                className="text-slate-400 hover:text-slate-600 text-lg font-bold"
              >
                &times;
              </button>
            </div>
            <form onSubmit={handleAddSupplier} className="p-6 space-y-4">
              <div>
                <label className="block text-xs font-semibold text-slate-700 mb-1">Supplier Name *</label>
                <input
                  type="text"
                  required
                  placeholder="e.g. Apex Industrial Supplies"
                  value={newSupplier.name}
                  onChange={(e) => setNewSupplier({ ...newSupplier, name: e.target.value })}
                  className="w-full px-3 py-2 text-sm border border-slate-200 rounded-lg"
                />
              </div>
              <div className="grid grid-cols-2 gap-3">
                <div>
                  <label className="block text-xs font-semibold text-slate-700 mb-1">Code *</label>
                  <input
                    type="text"
                    required
                    placeholder="e.g. SUP-004"
                    value={newSupplier.code}
                    onChange={(e) => setNewSupplier({ ...newSupplier, code: e.target.value })}
                    className="w-full px-3 py-2 text-sm border border-slate-200 rounded-lg"
                  />
                </div>
                <div>
                  <label className="block text-xs font-semibold text-slate-700 mb-1">Payment Terms</label>
                  <input
                    type="text"
                    value={newSupplier.paymentTerms}
                    onChange={(e) => setNewSupplier({ ...newSupplier, paymentTerms: e.target.value })}
                    className="w-full px-3 py-2 text-sm border border-slate-200 rounded-lg"
                  />
                </div>
              </div>
              <div className="grid grid-cols-2 gap-3">
                <div>
                  <label className="block text-xs font-semibold text-slate-700 mb-1">Email *</label>
                  <input
                    type="email"
                    required
                    placeholder="sales@vendor.com"
                    value={newSupplier.email}
                    onChange={(e) => setNewSupplier({ ...newSupplier, email: e.target.value })}
                    className="w-full px-3 py-2 text-sm border border-slate-200 rounded-lg"
                  />
                </div>
                <div>
                  <label className="block text-xs font-semibold text-slate-700 mb-1">Phone</label>
                  <input
                    type="text"
                    placeholder="+91 99999 11111"
                    value={newSupplier.phone}
                    onChange={(e) => setNewSupplier({ ...newSupplier, phone: e.target.value })}
                    className="w-full px-3 py-2 text-sm border border-slate-200 rounded-lg"
                  />
                </div>
              </div>
              <div>
                <label className="block text-xs font-semibold text-slate-700 mb-1">Contact Person</label>
                <input
                  type="text"
                  placeholder="Manager / Representative name"
                  value={newSupplier.contactPerson}
                  onChange={(e) => setNewSupplier({ ...newSupplier, contactPerson: e.target.value })}
                  className="w-full px-3 py-2 text-sm border border-slate-200 rounded-lg"
                />
              </div>

              <div className="flex justify-end gap-3 pt-3 border-t border-slate-200">
                <button
                  type="button"
                  onClick={() => setIsAddSupplierOpen(false)}
                  className="px-4 py-2 text-sm text-slate-600 hover:bg-slate-100 rounded-lg"
                >
                  Cancel
                </button>
                <button
                  type="submit"
                  disabled={submitting}
                  className="px-4 py-2 text-sm font-semibold text-white bg-blue-600 hover:bg-blue-700 rounded-lg disabled:opacity-50"
                >
                  {submitting ? 'Saving...' : 'Add Supplier'}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}

      {/* Create Purchase Order Modal */}
      {isCreatePOOpen && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50 p-4">
          <div className="bg-white rounded-xl shadow-xl w-full max-w-xl overflow-hidden">
            <div className="px-6 py-4 border-b border-slate-200 flex justify-between items-center bg-slate-50">
              <h3 className="font-bold text-slate-800 text-base">Create Purchase Order</h3>
              <button
                onClick={() => setIsCreatePOOpen(false)}
                className="text-slate-400 hover:text-slate-600 text-lg font-bold"
              >
                &times;
              </button>
            </div>
            <form onSubmit={handleCreatePO} className="p-6 space-y-4">
              <div className="grid grid-cols-2 gap-4">
                <div>
                  <label className="block text-xs font-semibold text-slate-700 mb-1">Select Supplier *</label>
                  <select
                    required
                    value={newPO.supplierId}
                    onChange={(e) => setNewPO({ ...newPO, supplierId: e.target.value })}
                    className="w-full px-3 py-2 text-sm border border-slate-200 rounded-lg"
                  >
                    <option value="">-- Choose Supplier --</option>
                    {suppliers.map(s => (
                      <option key={s.id} value={s.id}>{s.name} ({s.code})</option>
                    ))}
                  </select>
                </div>
                <div>
                  <label className="block text-xs font-semibold text-slate-700 mb-1">Expected Delivery (Days)</label>
                  <input
                    type="number"
                    min="1"
                    value={newPO.expectedDeliveryDays}
                    onChange={(e) => setNewPO({ ...newPO, expectedDeliveryDays: Number(e.target.value) })}
                    className="w-full px-3 py-2 text-sm border border-slate-200 rounded-lg"
                  />
                </div>
              </div>

              {/* Items Section */}
              <div className="border border-slate-200 rounded-lg p-3 bg-slate-50/50">
                <p className="text-xs font-bold text-slate-700 mb-2 uppercase">Items to Order</p>
                {newPO.items.map((item, idx) => (
                  <div key={idx} className="grid grid-cols-12 gap-2 mb-2 items-center">
                    <div className="col-span-6">
                      <select
                        required
                        value={item.productId}
                        onChange={(e) => {
                          const updated = [...newPO.items];
                          updated[idx].productId = e.target.value;
                          const selectedProd = products.find(p => p.id === e.target.value);
                          if (selectedProd) {
                            updated[idx].unitCost = selectedProd.costPrice || 50;
                          }
                          setNewPO({ ...newPO, items: updated });
                        }}
                        className="w-full px-2.5 py-1.5 text-xs border border-slate-200 rounded bg-white"
                      >
                        <option value="">-- Select Product --</option>
                        {products.map(p => (
                          <option key={p.id} value={p.id}>{p.name} ({p.sku})</option>
                        ))}
                      </select>
                    </div>
                    <div className="col-span-3">
                      <input
                        type="number"
                        min="1"
                        placeholder="Qty"
                        value={item.quantityOrdered}
                        onChange={(e) => {
                          const updated = [...newPO.items];
                          updated[idx].quantityOrdered = Number(e.target.value);
                          setNewPO({ ...newPO, items: updated });
                        }}
                        className="w-full px-2.5 py-1.5 text-xs border border-slate-200 rounded bg-white"
                      />
                    </div>
                    <div className="col-span-3">
                      <input
                        type="number"
                        min="0"
                        step="0.01"
                        placeholder="Unit Cost"
                        value={item.unitCost}
                        onChange={(e) => {
                          const updated = [...newPO.items];
                          updated[idx].unitCost = Number(e.target.value);
                          setNewPO({ ...newPO, items: updated });
                        }}
                        className="w-full px-2.5 py-1.5 text-xs border border-slate-200 rounded bg-white"
                      />
                    </div>
                  </div>
                ))}
              </div>

              <div>
                <label className="block text-xs font-semibold text-slate-700 mb-1">Notes / Instructions</label>
                <textarea
                  rows={2}
                  value={newPO.notes}
                  onChange={(e) => setNewPO({ ...newPO, notes: e.target.value })}
                  className="w-full px-3 py-2 text-sm border border-slate-200 rounded-lg"
                  placeholder="e.g. Inbound shipment reference..."
                />
              </div>

              <div className="flex justify-end gap-3 pt-3 border-t border-slate-200">
                <button
                  type="button"
                  onClick={() => setIsCreatePOOpen(false)}
                  className="px-4 py-2 text-sm text-slate-600 hover:bg-slate-100 rounded-lg"
                >
                  Cancel
                </button>
                <button
                  type="submit"
                  disabled={submitting}
                  className="px-4 py-2 text-sm font-semibold text-white bg-blue-600 hover:bg-blue-700 rounded-lg disabled:opacity-50"
                >
                  {submitting ? 'Generating PO...' : 'Create Purchase Order'}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}

      {/* Receive PO Modal */}
      {receivingPO && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50 p-4">
          <div className="bg-white rounded-xl shadow-xl w-full max-w-lg overflow-hidden">
            <div className="px-6 py-4 border-b border-slate-200 flex justify-between items-center bg-emerald-50 text-emerald-900">
              <h3 className="font-bold text-base flex items-center gap-2">
                <PackageCheck size={18} />
                Receive Goods: {receivingPO.orderNumber}
              </h3>
              <button
                onClick={() => setReceivingPO(null)}
                className="text-emerald-700 hover:text-emerald-900 text-lg font-bold"
              >
                &times;
              </button>
            </div>
            <div className="p-6 space-y-4">
              <p className="text-xs text-slate-500">
                Confirm physical items received at the warehouse. Receiving items immediately adds them to On-Hand stock and logs an inbound inventory movement.
              </p>
              <div className="space-y-3">
                {receivingPO.items.map((item) => (
                  <div key={item.id} className="p-3 bg-slate-50 rounded-lg border border-slate-200 flex items-center justify-between">
                    <div>
                      <div className="font-semibold text-sm text-slate-800">{item.productName}</div>
                      <div className="text-xs text-slate-500 font-mono">SKU: {item.productSku} | Ordered: {item.quantityOrdered}</div>
                    </div>
                    <div className="flex items-center gap-2">
                      <label className="text-xs font-medium text-slate-600">Receiving Qty:</label>
                      <input
                        type="number"
                        min="0"
                        max={item.quantityOrdered}
                        value={receivedQuantities[item.productId] ?? item.quantityOrdered}
                        onChange={(e) => {
                          setReceivedQuantities({
                            ...receivedQuantities,
                            [item.productId]: Number(e.target.value)
                          });
                        }}
                        className="w-20 px-2 py-1 text-sm font-bold text-center border border-slate-300 rounded bg-white"
                      />
                    </div>
                  </div>
                ))}
              </div>

              <div className="flex justify-end gap-3 pt-3 border-t border-slate-200">
                <button
                  type="button"
                  onClick={() => setReceivingPO(null)}
                  className="px-4 py-2 text-sm text-slate-600 hover:bg-slate-100 rounded-lg"
                >
                  Cancel
                </button>
                <button
                  type="button"
                  onClick={handleConfirmReceive}
                  disabled={submitting}
                  className="px-4 py-2 text-sm font-semibold text-white bg-emerald-600 hover:bg-emerald-700 rounded-lg disabled:opacity-50"
                >
                  {submitting ? 'Receiving...' : 'Confirm Receipt & Add to Stock'}
                </button>
              </div>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};
