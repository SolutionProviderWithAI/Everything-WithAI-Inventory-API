import React, { useEffect, useState } from 'react';
import { ArrowLeftRight, Search, RefreshCw, FileText } from 'lucide-react';
import { api } from '../services/api';
import { InventoryMovement } from '../types';

export const StockMovements: React.FC = () => {
  const [movements, setMovements] = useState<InventoryMovement[]>([]);
  const [total, setTotal] = useState(0);
  const [page, setPage] = useState(1);
  const [loading, setLoading] = useState(true);

  const loadMovements = async () => {
    setLoading(true);
    try {
      const res = await api.getStockMovements(page, 50);
      setMovements(res.items || []);
      setTotal(res.totalCount || 0);
    } catch (e) {
      console.error(e);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    loadMovements();
  }, [page]);

  return (
    <div className="space-y-6">
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4 bg-white p-5 rounded-2xl border border-slate-200 shadow-xs">
        <div>
          <h2 className="text-lg font-bold text-slate-900">Inventory Movement Audit Trail</h2>
          <p className="text-xs text-slate-500">
            Immutable log of all receipts, reservations, dispatches, adjustments, and releases.
          </p>
        </div>

        <button onClick={loadMovements} className="btn btn-secondary text-xs flex items-center gap-1.5">
          <RefreshCw size={14} />
          Refresh Log
        </button>
      </div>

      <div className="bg-white rounded-2xl border border-slate-200 shadow-xs overflow-hidden">
        <div className="table-container border-0">
          <table>
            <thead>
              <tr>
                <th>Timestamp</th>
                <th>Product & SKU</th>
                <th>Warehouse</th>
                <th>Transaction Type</th>
                <th>Qty Change</th>
                <th>Balance On-Hand After</th>
                <th>Balance Reserved After</th>
                <th>Reference</th>
                <th>Operator</th>
              </tr>
            </thead>
            <tbody>
              {movements.length === 0 ? (
                <tr>
                  <td colSpan={9} className="text-center py-8 text-slate-400 text-xs">
                    No movement records logged yet.
                  </td>
                </tr>
              ) : (
                movements.map((m) => {
                  const isPositive = m.quantityChange > 0;
                  const isZero = m.quantityChange === 0;

                  return (
                    <tr key={m.id}>
                      <td className="text-xs text-slate-500 whitespace-nowrap">
                        {new Date(m.createdAt).toLocaleString()}
                      </td>
                      <td>
                        <div className="font-semibold text-slate-800">{m.productName}</div>
                        <div className="font-mono text-xs text-slate-400">{m.productSku}</div>
                      </td>
                      <td className="text-xs text-slate-600">{m.warehouseCode}</td>
                      <td>
                        <span
                          className={`badge ${
                            m.transactionType === 'Receipt'
                              ? 'badge-success'
                              : m.transactionType === 'Dispatch'
                              ? 'badge-primary'
                              : m.transactionType === 'Reservation'
                              ? 'badge-warning'
                              : m.transactionType === 'ReservationRelease'
                              ? 'badge-neutral'
                              : 'badge-neutral'
                          }`}
                        >
                          {m.transactionType}
                        </span>
                      </td>
                      <td className="font-bold text-sm">
                        <span
                          className={
                            isPositive
                              ? 'text-emerald-600'
                              : isZero
                              ? 'text-slate-500'
                              : 'text-rose-600'
                          }
                        >
                          {isPositive ? `+${m.quantityChange}` : m.quantityChange}
                        </span>
                      </td>
                      <td className="font-semibold text-slate-900">{m.balanceOnHandAfter}</td>
                      <td className="font-semibold text-slate-600">{m.balanceReservedAfter}</td>
                      <td className="text-xs">
                        <span className="font-semibold text-slate-700">{m.referenceType}: </span>
                        <span className="font-mono text-slate-500">{m.referenceId || 'N/A'}</span>
                        {m.notes && <div className="text-[11px] text-slate-400">{m.notes}</div>}
                      </td>
                      <td className="text-xs text-slate-500">{m.createdBy}</td>
                    </tr>
                  );
                })
              )}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  );
};
