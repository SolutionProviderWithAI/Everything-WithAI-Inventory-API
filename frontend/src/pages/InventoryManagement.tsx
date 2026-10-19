import React, { useEffect, useState } from 'react';
import { Boxes, PlusCircle, MinusCircle, AlertTriangle, CheckCircle2, RefreshCw, FileText } from 'lucide-react';
import { api } from '../services/api';
import { Modal } from '../components/StatCard';
import { Product } from '../types';

export const InventoryManagement: React.FC = () => {
  const [products, setProducts] = useState<Product[]>([]);
  const [loading, setLoading] = useState(true);

  // Adjustment modal state
  const [isAdjustModalOpen, setIsAdjustModalOpen] = useState(false);
  const [selectedProduct, setSelectedProduct] = useState<Product | null>(null);
  const [adjustQuantity, setAdjustQuantity] = useState('10');
  const [adjustReason, setAdjustReason] = useState('Stock Correction');
  const [adjustNotes, setAdjustNotes] = useState('');

  // Receipt modal state
  const [isReceiptModalOpen, setIsReceiptModalOpen] = useState(false);
  const [receiptQuantity, setReceiptQuantity] = useState('50');
  const [receiptRef, setReceiptRef] = useState('SUPP-REC-2026');
  const [receiptNotes, setReceiptNotes] = useState('');

  const [feedback, setFeedback] = useState<{ type: 'success' | 'error'; message: string } | null>(null);

  const loadInventory = async () => {
    setLoading(true);
    try {
      const res = await api.getProducts(undefined, undefined, 1, 100);
      setProducts(res.items || []);
    } catch (e: any) {
      console.error(e);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    loadInventory();
  }, []);

  const handleAdjustSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!selectedProduct) return;
    setFeedback(null);

    const qty = parseInt(adjustQuantity);
    if (isNaN(qty)) return;

    try {
      // Fetch warehouse stock for product
      const stock = await api.getProductStock(selectedProduct.id);
      const whId = stock.length > 0 ? stock[0].warehouseId : undefined;

      if (!whId) {
        throw new Error('No warehouse inventory found for this product.');
      }

      await api.adjustStock({
        productId: selectedProduct.id,
        warehouseId: whId,
        quantityAdjustment: qty,
        reason: adjustReason,
        notes: adjustNotes || `Manual adjustment by operator`
      });

      setFeedback({ type: 'success', message: `Stock for ${selectedProduct.name} adjusted by ${qty > 0 ? `+${qty}` : qty} units.` });
      setIsAdjustModalOpen(false);
      await loadInventory();
    } catch (err: any) {
      setFeedback({ type: 'error', message: err.message || 'Stock adjustment failed.' });
    }
  };

  const handleReceiptSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!selectedProduct) return;
    setFeedback(null);

    const qty = parseInt(receiptQuantity);
    if (isNaN(qty) || qty <= 0) return;

    try {
      const stock = await api.getProductStock(selectedProduct.id);
      const whId = stock.length > 0 ? stock[0].warehouseId : undefined;

      if (!whId) {
        throw new Error('No warehouse inventory found for this product.');
      }

      await api.receiveStock({
        productId: selectedProduct.id,
        warehouseId: whId,
        quantityReceived: qty,
        referenceType: 'SupplierReceipt',
        referenceId: receiptRef,
        notes: receiptNotes || 'Recorded shipment receipt'
      });

      setFeedback({ type: 'success', message: `Successfully received ${qty} units of ${selectedProduct.name}.` });
      setIsReceiptModalOpen(false);
      await loadInventory();
    } catch (err: any) {
      setFeedback({ type: 'error', message: err.message || 'Stock receipt failed.' });
    }
  };

  return (
    <div className="space-y-6">
      {/* Top Banner with Calculation Formula */}
      <div className="bg-gradient-to-r from-slate-900 to-blue-950 text-white p-6 rounded-2xl shadow-sm flex flex-col md:flex-row md:items-center justify-between gap-4">
        <div>
          <span className="text-xs font-bold uppercase tracking-wider text-blue-400 bg-blue-900/50 px-2.5 py-1 rounded-md border border-blue-700/50">
            Real-Time Concurrency & Reservation Layer
          </span>
          <h2 className="text-xl font-bold mt-2">Active Inventory Control</h2>
          <p className="text-xs text-slate-300 mt-1 max-w-2xl">
            Live inventory tracking with guaranteed concurrency safety. Orders atomically reserve available stock at confirmation, preventing negative balances and race conditions.
          </p>
        </div>

        <div className="bg-white/10 backdrop-blur-md p-3.5 rounded-xl border border-white/10 text-xs font-mono space-y-1">
          <p className="text-blue-300 font-bold">Calculation Rule:</p>
          <p className="text-white">Available to Sell = On-Hand Stock − Reserved</p>
          <p className="text-slate-400 text-[11px]">Dispatch converts reservation to physical deduction.</p>
        </div>
      </div>

      {feedback && (
        <div
          className={`p-4 rounded-xl border flex items-center gap-2.5 text-xs ${
            feedback.type === 'success'
              ? 'bg-emerald-50 border-emerald-200 text-emerald-800'
              : 'bg-rose-50 border-rose-200 text-rose-800'
          }`}
        >
          {feedback.type === 'success' ? <CheckCircle2 size={16} /> : <AlertTriangle size={16} />}
          <span>{feedback.message}</span>
        </div>
      )}

      {/* Inventory Table */}
      <div className="bg-white rounded-2xl border border-slate-200 shadow-xs overflow-hidden">
        <div className="p-4 border-b border-slate-100 flex items-center justify-between">
          <div className="flex items-center gap-2">
            <Boxes size={18} className="text-blue-600" />
            <h3 className="font-bold text-slate-900 text-sm">Warehouse Stock Breakdown</h3>
          </div>
          <button onClick={loadInventory} className="btn btn-secondary text-xs flex items-center gap-1.5">
            <RefreshCw size={14} />
            Refresh
          </button>
        </div>

        <div className="table-container border-0">
          <table>
            <thead>
              <tr>
                <th>Product & SKU</th>
                <th>Category</th>
                <th>Physical On-Hand</th>
                <th>Reserved Stock</th>
                <th>Available to Sell</th>
                <th>Reorder Level</th>
                <th>Stock Status</th>
                <th className="text-right">Quick Stock Actions</th>
              </tr>
            </thead>
            <tbody>
              {products.map((p) => {
                const isLow = p.availableToSell <= p.reorderLevel && p.availableToSell > 0;
                const isOut = p.availableToSell <= 0;

                return (
                  <tr key={p.id}>
                    <td>
                      <div className="font-bold text-slate-900">{p.name}</div>
                      <div className="font-mono text-xs text-slate-400">{p.sku}</div>
                    </td>
                    <td className="text-xs text-slate-600">{p.categoryName}</td>
                    <td className="font-semibold text-slate-800 text-sm">{p.totalQuantityOnHand}</td>
                    <td className="font-semibold text-amber-700 text-sm">
                      {p.totalReservedQuantity > 0 ? (
                        <span className="px-2 py-0.5 rounded bg-amber-50 border border-amber-200">
                          {p.totalReservedQuantity} reserved
                        </span>
                      ) : (
                        <span className="text-slate-400">0</span>
                      )}
                    </td>
                    <td className="font-bold text-blue-700 text-base">
                      {p.availableToSell}
                    </td>
                    <td className="text-xs text-slate-500">{p.reorderLevel} units</td>
                    <td>
                      {isOut ? (
                        <span className="badge badge-danger">Out of Stock</span>
                      ) : isLow ? (
                        <span className="badge badge-warning">Low Stock</span>
                      ) : (
                        <span className="badge badge-success">In Stock</span>
                      )}
                    </td>
                    <td className="text-right">
                      <div className="flex items-center justify-end gap-2">
                        <button
                          onClick={() => {
                            setSelectedProduct(p);
                            setAdjustQuantity('10');
                            setIsAdjustModalOpen(true);
                          }}
                          className="btn btn-secondary btn-sm"
                          title="Adjust Stock"
                        >
                          <PlusCircle size={13} className="text-blue-600" />
                          Adjust
                        </button>
                        <button
                          onClick={() => {
                            setSelectedProduct(p);
                            setReceiptQuantity('50');
                            setIsReceiptModalOpen(true);
                          }}
                          className="btn btn-primary btn-sm"
                          title="Record Supplier Receipt"
                        >
                          Receive Stock
                        </button>
                      </div>
                    </td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        </div>
      </div>

      {/* Quick Adjust Modal */}
      {selectedProduct && (
        <Modal
          isOpen={isAdjustModalOpen}
          onClose={() => setIsAdjustModalOpen(false)}
          title={`Stock Adjustment: ${selectedProduct.name} (${selectedProduct.sku})`}
        >
          <form onSubmit={handleAdjustSubmit} className="space-y-4 text-xs">
            <div className="p-3 bg-slate-50 rounded-lg border border-slate-200 space-y-1">
              <p className="font-semibold text-slate-800">Current Balance:</p>
              <div className="flex gap-4 text-slate-600">
                <span>On-Hand: <b>{selectedProduct.totalQuantityOnHand}</b></span>
                <span>Reserved: <b>{selectedProduct.totalReservedQuantity}</b></span>
                <span>Available to Sell: <b className="text-blue-600">{selectedProduct.availableToSell}</b></span>
              </div>
            </div>

            <div>
              <label className="block text-slate-700 font-semibold mb-1">
                Quantity Adjustment (+ to add, - to reduce) *
              </label>
              <input
                type="number"
                required
                value={adjustQuantity}
                onChange={(e) => setAdjustQuantity(e.target.value)}
                placeholder="e.g. 10 or -5"
                className="input"
              />
              <p className="text-[11px] text-slate-400 mt-1">
                Negative adjustments cannot reduce stock below active reservations.
              </p>
            </div>

            <div>
              <label className="block text-slate-700 font-semibold mb-1">Adjustment Reason *</label>
              <select
                value={adjustReason}
                onChange={(e) => setAdjustReason(e.target.value)}
                className="select"
              >
                <option value="Stock Correction">Stock Correction / Cycle Count</option>
                <option value="Damaged Goods">Damaged Goods</option>
                <option value="Customer Return">Customer Return Restock</option>
                <option value="Internal Transfer">Internal Transfer</option>
              </select>
            </div>

            <div>
              <label className="block text-slate-700 font-semibold mb-1">Audit Notes</label>
              <textarea
                rows={2}
                value={adjustNotes}
                onChange={(e) => setAdjustNotes(e.target.value)}
                placeholder="Explanation for stock movement history..."
                className="textarea"
              />
            </div>

            <div className="flex justify-end gap-2 pt-3 border-t border-slate-100">
              <button
                type="button"
                onClick={() => setIsAdjustModalOpen(false)}
                className="btn btn-secondary text-xs"
              >
                Cancel
              </button>
              <button type="submit" className="btn btn-primary text-xs">
                Submit Adjustment
              </button>
            </div>
          </form>
        </Modal>
      )}

      {/* Stock Receipt Modal */}
      {selectedProduct && (
        <Modal
          isOpen={isReceiptModalOpen}
          onClose={() => setIsReceiptModalOpen(false)}
          title={`Record Supplier Receipt: ${selectedProduct.name}`}
        >
          <form onSubmit={handleReceiptSubmit} className="space-y-4 text-xs">
            <div>
              <label className="block text-slate-700 font-semibold mb-1">Received Quantity *</label>
              <input
                type="number"
                min="1"
                required
                value={receiptQuantity}
                onChange={(e) => setReceiptQuantity(e.target.value)}
                className="input"
              />
            </div>

            <div>
              <label className="block text-slate-700 font-semibold mb-1">PO / Shipment Reference</label>
              <input
                type="text"
                value={receiptRef}
                onChange={(e) => setReceiptRef(e.target.value)}
                placeholder="e.g. PO-20261018-1234"
                className="input"
              />
            </div>

            <div>
              <label className="block text-slate-700 font-semibold mb-1">Receiving Notes</label>
              <textarea
                rows={2}
                value={receiptNotes}
                onChange={(e) => setReceiptNotes(e.target.value)}
                placeholder="Supplier batch, package condition..."
                className="textarea"
              />
            </div>

            <div className="flex justify-end gap-2 pt-3 border-t border-slate-100">
              <button
                type="button"
                onClick={() => setIsReceiptModalOpen(false)}
                className="btn btn-secondary text-xs"
              >
                Cancel
              </button>
              <button type="submit" className="btn btn-success text-xs">
                Confirm Receipt & Update Inventory
              </button>
            </div>
          </form>
        </Modal>
      )}
    </div>
  );
};
