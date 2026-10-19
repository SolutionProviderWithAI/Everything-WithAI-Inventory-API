import React, { useEffect, useState } from 'react';
import { BarChart3, Download, Calendar, DollarSign, TrendingUp, Package, Layers, RefreshCw, ShoppingBag } from 'lucide-react';
import { api } from '../services/api';
import { SalesReport, InventoryValuation } from '../types';

export const ReportsAnalytics: React.FC = () => {
  const [salesReport, setSalesReport] = useState<SalesReport | null>(null);
  const [inventoryValuation, setInventoryValuation] = useState<InventoryValuation | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [exporting, setExporting] = useState(false);

  // Date filters
  const today = new Date().toISOString().split('T')[0];
  const thirtyDaysAgo = new Date(Date.now() - 30 * 24 * 60 * 60 * 1000).toISOString().split('T')[0];
  const [startDate, setStartDate] = useState(thirtyDaysAgo);
  const [endDate, setEndDate] = useState(today);

  const loadReports = async () => {
    try {
      setLoading(true);
      setError(null);
      const [sales, inv] = await Promise.all([
        api.getSalesReport(startDate, endDate),
        api.getInventoryValuation()
      ]);
      setSalesReport(sales);
      setInventoryValuation(inv);
    } catch (err: any) {
      setError(err.message || 'Failed to load analytics and reports');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    loadReports();
  }, []);

  const handleExportSalesCsv = async () => {
    try {
      setExporting(true);
      const blob = await api.exportSalesCsv(startDate, endDate);
      const url = window.URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.href = url;
      a.download = `sales_report_${startDate}_to_${endDate}.csv`;
      document.body.appendChild(a);
      a.click();
      a.remove();
    } catch (err: any) {
      alert('Failed to export sales CSV: ' + err.message);
    } finally {
      setExporting(false);
    }
  };

  const handleExportInventoryCsv = async () => {
    try {
      setExporting(true);
      const blob = await api.exportInventoryCsv();
      const url = window.URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.href = url;
      a.download = `inventory_valuation_${new Date().toISOString().split('T')[0]}.csv`;
      document.body.appendChild(a);
      a.click();
      a.remove();
    } catch (err: any) {
      alert('Failed to export inventory CSV: ' + err.message);
    } finally {
      setExporting(false);
    }
  };

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex flex-col sm:flex-row justify-between items-start sm:items-center gap-4">
        <div>
          <h2 className="text-xl font-bold text-slate-900 flex items-center gap-2">
            <BarChart3 className="text-blue-600" size={24} />
            Executive Reports & Analytics
          </h2>
          <p className="text-sm text-slate-500">
            Real-time financial summaries, inventory valuation at cost vs retail, and CSV export.
          </p>
        </div>

        <div className="flex flex-wrap items-center gap-2">
          <button
            onClick={loadReports}
            className="flex items-center gap-1.5 px-3 py-2 text-sm font-medium text-slate-600 bg-white border border-slate-200 rounded-lg hover:bg-slate-50 shadow-xs"
          >
            <RefreshCw size={15} />
            Refresh
          </button>
          <button
            onClick={handleExportSalesCsv}
            disabled={exporting}
            className="flex items-center gap-1.5 px-3 py-2 text-sm font-semibold text-slate-700 bg-white border border-slate-200 rounded-lg hover:bg-slate-50 shadow-xs disabled:opacity-50"
          >
            <Download size={15} className="text-blue-600" />
            Export Sales (CSV)
          </button>
          <button
            onClick={handleExportInventoryCsv}
            disabled={exporting}
            className="flex items-center gap-1.5 px-3 py-2 text-sm font-semibold text-white bg-blue-600 hover:bg-blue-700 rounded-lg shadow-sm disabled:opacity-50"
          >
            <Download size={15} />
            Export Valuation (CSV)
          </button>
        </div>
      </div>

      {error && (
        <div className="p-4 bg-rose-50 border border-rose-200 text-rose-700 rounded-lg text-sm">
          {error}
        </div>
      )}

      {/* Date Filter Bar */}
      <div className="bg-white p-4 rounded-xl border border-slate-200 shadow-xs flex flex-wrap items-center justify-between gap-4">
        <div className="flex items-center gap-2 text-sm font-medium text-slate-700">
          <Calendar size={18} className="text-slate-400" />
          <span>Reporting Period:</span>
        </div>
        <div className="flex items-center gap-3">
          <div className="flex items-center gap-2 text-xs">
            <span className="text-slate-500 font-medium">From:</span>
            <input
              type="date"
              value={startDate}
              onChange={(e) => setStartDate(e.target.value)}
              className="px-2.5 py-1.5 text-xs border border-slate-200 rounded-lg bg-slate-50 focus:bg-white"
            />
          </div>
          <div className="flex items-center gap-2 text-xs">
            <span className="text-slate-500 font-medium">To:</span>
            <input
              type="date"
              value={endDate}
              onChange={(e) => setEndDate(e.target.value)}
              className="px-2.5 py-1.5 text-xs border border-slate-200 rounded-lg bg-slate-50 focus:bg-white"
            />
          </div>
          <button
            onClick={loadReports}
            className="px-3 py-1.5 text-xs font-semibold text-white bg-blue-600 hover:bg-blue-700 rounded-lg"
          >
            Filter
          </button>
        </div>
      </div>

      {/* KPI Highlights */}
      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
        <div className="bg-white p-5 rounded-xl border border-slate-200 shadow-xs">
          <div className="flex items-center justify-between">
            <p className="text-xs font-semibold text-slate-500 uppercase">Total Net Revenue</p>
            <div className="p-2 bg-emerald-50 text-emerald-600 rounded-lg">
              <DollarSign size={18} />
            </div>
          </div>
          <h3 className="text-2xl font-bold text-slate-900 mt-2">
            ₹{salesReport?.totalRevenue?.toLocaleString('en-IN') || '0'}
          </h3>
          <p className="text-xs text-slate-500 mt-1">
            Total sales generated across {salesReport?.totalOrders || 0} orders
          </p>
        </div>

        <div className="bg-white p-5 rounded-xl border border-slate-200 shadow-xs">
          <div className="flex items-center justify-between">
            <p className="text-xs font-semibold text-slate-500 uppercase">Average Order Value</p>
            <div className="p-2 bg-blue-50 text-blue-600 rounded-lg">
              <TrendingUp size={18} />
            </div>
          </div>
          <h3 className="text-2xl font-bold text-slate-900 mt-2">
            ₹{salesReport?.averageOrderValue?.toLocaleString('en-IN') || '0'}
          </h3>
          <p className="text-xs text-slate-500 mt-1">
            Per-order customer basket size
          </p>
        </div>

        <div className="bg-white p-5 rounded-xl border border-slate-200 shadow-xs">
          <div className="flex items-center justify-between">
            <p className="text-xs font-semibold text-slate-500 uppercase">Cost Valuation</p>
            <div className="p-2 bg-purple-50 text-purple-600 rounded-lg">
              <Package size={18} />
            </div>
          </div>
          <h3 className="text-2xl font-bold text-slate-900 mt-2">
            ₹{inventoryValuation?.totalCostValuation?.toLocaleString('en-IN') || '0'}
          </h3>
          <p className="text-xs text-slate-500 mt-1">
            Total on-hand stock valued at procurement cost
          </p>
        </div>

        <div className="bg-white p-5 rounded-xl border border-slate-200 shadow-xs">
          <div className="flex items-center justify-between">
            <p className="text-xs font-semibold text-slate-500 uppercase">Retail Potential</p>
            <div className="p-2 bg-indigo-50 text-indigo-600 rounded-lg">
              <Layers size={18} />
            </div>
          </div>
          <h3 className="text-2xl font-bold text-slate-900 mt-2">
            ₹{inventoryValuation?.totalRetailValuation?.toLocaleString('en-IN') || '0'}
          </h3>
          <p className="text-xs text-slate-500 mt-1">
            Projected revenue if all available stock sells
          </p>
        </div>
      </div>

      {/* Two Column Section */}
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        {/* Top Products */}
        <div className="bg-white rounded-xl border border-slate-200 shadow-xs p-5">
          <h3 className="text-sm font-bold text-slate-800 uppercase tracking-wider mb-4 flex items-center gap-2">
            <ShoppingBag size={16} className="text-blue-600" />
            Top Selling Products
          </h3>
          <div className="overflow-x-auto">
            <table className="w-full text-left text-sm text-slate-600">
              <thead className="bg-slate-50 text-slate-500 text-xs font-semibold uppercase">
                <tr>
                  <th className="px-3 py-2">Product</th>
                  <th className="px-3 py-2 text-right">Units Sold</th>
                  <th className="px-3 py-2 text-right">Revenue</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-100">
                {salesReport?.topProducts && salesReport.topProducts.length > 0 ? (
                  salesReport.topProducts.map((p) => (
                    <tr key={p.productId} className="hover:bg-slate-50">
                      <td className="px-3 py-3">
                        <div className="font-semibold text-slate-800">{p.productName}</div>
                        <div className="text-xs text-slate-400 font-mono">{p.sku}</div>
                      </td>
                      <td className="px-3 py-3 text-right font-medium text-slate-800">
                        {p.unitsSold}
                      </td>
                      <td className="px-3 py-3 text-right font-bold text-emerald-700">
                        ₹{p.revenueGenerated?.toLocaleString('en-IN')}
                      </td>
                    </tr>
                  ))
                ) : (
                  <tr>
                    <td colSpan={3} className="px-3 py-6 text-center text-slate-400 text-xs">
                      No sales recorded in the selected date range.
                    </td>
                  </tr>
                )}
              </tbody>
            </table>
          </div>
        </div>

        {/* Inventory Stock Breakdown */}
        <div className="bg-white rounded-xl border border-slate-200 shadow-xs p-5">
          <h3 className="text-sm font-bold text-slate-800 uppercase tracking-wider mb-4 flex items-center gap-2">
            <Layers size={16} className="text-blue-600" />
            Warehouse Inventory Breakdown
          </h3>
          <div className="space-y-3">
            <div className="grid grid-cols-3 gap-3 p-3 bg-slate-50 rounded-lg text-center text-xs">
              <div>
                <p className="text-slate-500 font-medium">Physical On-Hand</p>
                <p className="text-lg font-bold text-slate-900 mt-1">{inventoryValuation?.totalUnitsOnHand || 0}</p>
              </div>
              <div>
                <p className="text-slate-500 font-medium">Reserved in Orders</p>
                <p className="text-lg font-bold text-blue-700 mt-1">{inventoryValuation?.totalUnitsReserved || 0}</p>
              </div>
              <div>
                <p className="text-slate-500 font-medium">Available to Sell</p>
                <p className="text-lg font-bold text-emerald-700 mt-1">{inventoryValuation?.totalUnitsAvailable || 0}</p>
              </div>
            </div>

            <div className="pt-2">
              <p className="text-xs font-semibold text-slate-500 uppercase mb-2">Location Distribution</p>
              {inventoryValuation?.warehouseBreakdown && inventoryValuation.warehouseBreakdown.length > 0 ? (
                inventoryValuation.warehouseBreakdown.map((wh) => (
                  <div key={wh.warehouseId} className="flex justify-between items-center py-2 border-b border-slate-100 text-sm">
                    <div>
                      <span className="font-semibold text-slate-800">{wh.warehouseName}</span>
                      <span className="text-xs text-slate-400 ml-2">({wh.totalUnits} units)</span>
                    </div>
                    <span className="font-medium text-slate-700">
                      ₹{wh.totalCostValue?.toLocaleString('en-IN')}
                    </span>
                  </div>
                ))
              ) : (
                <p className="text-xs text-slate-400 italic">No warehouse distribution data</p>
              )}
            </div>
          </div>
        </div>
      </div>
    </div>
  );
};
