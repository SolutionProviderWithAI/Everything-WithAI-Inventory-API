import React, { useEffect, useState } from 'react';
import {
  Package,
  ShoppingCart,
  Users,
  AlertTriangle,
  TrendingUp,
  DollarSign,
  Boxes,
  ArrowUpRight,
  Clock,
  CheckCircle2
} from 'lucide-react';
import { api } from '../services/api';
import { StatCard } from '../components/StatCard';
import { Product, SalesOrder, SalesReport, InventoryValuation, LowStockAlert } from '../types';

interface DashboardOverviewProps {
  onNavigateTab: (tab: any) => void;
}

export const DashboardOverview: React.FC<DashboardOverviewProps> = ({ onNavigateTab }) => {
  const [salesReport, setSalesReport] = useState<SalesReport | null>(null);
  const [valuation, setValuation] = useState<InventoryValuation | null>(null);
  const [recentOrders, setRecentOrders] = useState<SalesOrder[]>([]);
  const [lowStockAlerts, setLowStockAlerts] = useState<LowStockAlert[]>([]);
  const [loading, setLoading] = useState(true);

  const loadData = async () => {
    setLoading(true);
    try {
      const [sales, val, ordersRes, lowStock] = await Promise.all([
        api.getSalesReport(),
        api.getInventoryValuation(),
        api.getOrders(undefined, 1, 6),
        api.getLowStockAlerts()
      ]);
      setSalesReport(sales);
      setValuation(val);
      setRecentOrders(ordersRes.items || []);
      setLowStockAlerts(lowStock || []);
    } catch (e) {
      console.error('Failed to load dashboard data:', e);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    loadData();
  }, []);

  return (
    <div className="space-y-6">
      {/* Top Welcome Header */}
      <div className="flex flex-col md:flex-row md:items-center justify-between gap-4 bg-white p-6 rounded-2xl border border-slate-200 shadow-xs">
        <div>
          <h2 className="text-xl font-bold text-slate-900">Enterprise Dashboard Overview</h2>
          <p className="text-sm text-slate-500 mt-1">
            Real-time monitoring of inventory, AI-driven orders, stock reservations, and revenue.
          </p>
        </div>
        <div className="flex items-center gap-3">
          <button
            onClick={() => onNavigateTab('products')}
            className="btn btn-secondary text-xs"
          >
            Manage Products
          </button>
          <button
            onClick={() => onNavigateTab('orders')}
            className="btn btn-primary text-xs"
          >
            View Orders Pipeline
          </button>
        </div>
      </div>

      {/* Metric Cards Grid */}
      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
        <StatCard
          title="Total Products"
          value={valuation?.totalActiveProducts ?? '—'}
          subtitle="Catalog Active Items"
          icon={<Package size={22} />}
          variant="primary"
        />
        <StatCard
          title="Total Revenue (30d)"
          value={salesReport ? `₹${salesReport.totalRevenue.toLocaleString()}` : '—'}
          subtitle={`Across ${salesReport?.totalOrders ?? 0} orders`}
          icon={<DollarSign size={22} />}
          variant="success"
        />
        <StatCard
          title="Inventory Cost Valuation"
          value={valuation ? `₹${valuation.totalCostValuation.toLocaleString()}` : '—'}
          subtitle={`${valuation?.totalUnitsOnHand ?? 0} units on hand`}
          icon={<Boxes size={22} />}
          variant="default"
        />
        <StatCard
          title="Low & Out-of-Stock"
          value={(valuation?.lowStockProductCount ?? 0) + (valuation?.outOfStockProductCount ?? 0)}
          subtitle={`${valuation?.outOfStockProductCount ?? 0} completely out`}
          icon={<AlertTriangle size={22} />}
          variant="danger"
        />
      </div>

      {/* Second Row Stats */}
      <div className="grid grid-cols-1 sm:grid-cols-3 gap-4">
        <div className="p-4 bg-white rounded-xl border border-slate-200 shadow-xs flex items-center gap-4">
          <div className="p-3 bg-blue-50 text-blue-600 rounded-lg">
            <ShoppingCart size={20} />
          </div>
          <div>
            <p className="text-xs text-slate-500">Reserved Units</p>
            <p className="text-xl font-bold text-slate-900">{valuation?.totalUnitsReserved ?? 0} units</p>
            <p className="text-[11px] text-slate-400">Locked in active orders</p>
          </div>
        </div>

        <div className="p-4 bg-white rounded-xl border border-slate-200 shadow-xs flex items-center gap-4">
          <div className="p-3 bg-emerald-50 text-emerald-600 rounded-lg">
            <TrendingUp size={20} />
          </div>
          <div>
            <p className="text-xs text-slate-500">Available to Sell</p>
            <p className="text-xl font-bold text-slate-900">{valuation?.totalUnitsAvailable ?? 0} units</p>
            <p className="text-[11px] text-slate-400">On-hand minus reserved</p>
          </div>
        </div>

        <div className="p-4 bg-white rounded-xl border border-slate-200 shadow-xs flex items-center gap-4">
          <div className="p-3 bg-indigo-50 text-indigo-600 rounded-lg">
            <DollarSign size={20} />
          </div>
          <div>
            <p className="text-xs text-slate-500">Average Order Value</p>
            <p className="text-xl font-bold text-slate-900">
              ₹{salesReport?.averageOrderValue.toFixed(2) ?? '0.00'}
            </p>
            <p className="text-[11px] text-slate-400">Net per customer order</p>
          </div>
        </div>
      </div>

      {/* Low Stock Alerts & Best Sellers Grid */}
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        {/* Low Stock Watchlist */}
        <div className="bg-white rounded-2xl border border-slate-200 p-5 shadow-xs flex flex-col justify-between">
          <div>
            <div className="flex items-center justify-between mb-4">
              <div className="flex items-center gap-2">
                <AlertTriangle size={18} className="text-amber-500" />
                <h3 className="font-bold text-slate-900 text-sm">Low Stock & Out-of-Stock Alerts</h3>
              </div>
              <button
                onClick={() => onNavigateTab('inventory')}
                className="text-xs text-blue-600 hover:text-blue-800 font-semibold"
              >
                Stock Adjustments →
              </button>
            </div>

            {lowStockAlerts.length === 0 ? (
              <div className="p-8 text-center text-slate-400 text-xs">
                <CheckCircle2 size={32} className="mx-auto text-emerald-500 mb-2 opacity-80" />
                All products maintain healthy inventory levels above their reorder thresholds.
              </div>
            ) : (
              <div className="table-container">
                <table>
                  <thead>
                    <tr>
                      <th>Product</th>
                      <th>SKU</th>
                      <th>Available</th>
                      <th>Reorder Level</th>
                      <th>Status</th>
                    </tr>
                  </thead>
                  <tbody>
                    {lowStockAlerts.map((alert) => (
                      <tr key={alert.productId}>
                        <td className="font-medium text-slate-800">{alert.name}</td>
                        <td className="font-mono text-xs">{alert.sku}</td>
                        <td className="font-bold text-slate-900">{alert.availableToSell}</td>
                        <td className="text-slate-500">{alert.reorderLevel}</td>
                        <td>
                          <span
                            className={`badge ${
                              alert.status === 'OutOfStock' ? 'badge-danger' : 'badge-warning'
                            }`}
                          >
                            {alert.status === 'OutOfStock' ? 'Out of Stock' : 'Low Stock'}
                          </span>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}
          </div>
        </div>

        {/* Best Selling Products */}
        <div className="bg-white rounded-2xl border border-slate-200 p-5 shadow-xs flex flex-col justify-between">
          <div>
            <div className="flex items-center justify-between mb-4">
              <div className="flex items-center gap-2">
                <TrendingUp size={18} className="text-blue-600" />
                <h3 className="font-bold text-slate-900 text-sm">Top Best-Selling Products</h3>
              </div>
              <button
                onClick={() => onNavigateTab('reports')}
                className="text-xs text-blue-600 hover:text-blue-800 font-semibold"
              >
                Full Analytics →
              </button>
            </div>

            {(!salesReport || salesReport.topProducts.length === 0) ? (
              <div className="p-8 text-center text-slate-400 text-xs">
                No orders confirmed in the active period yet.
              </div>
            ) : (
              <div className="table-container">
                <table>
                  <thead>
                    <tr>
                      <th>Product</th>
                      <th>SKU</th>
                      <th>Units Sold</th>
                      <th>Revenue (INR)</th>
                    </tr>
                  </thead>
                  <tbody>
                    {salesReport.topProducts.map((p) => (
                      <tr key={p.productId}>
                        <td className="font-medium text-slate-800">{p.productName}</td>
                        <td className="font-mono text-xs">{p.sku}</td>
                        <td className="font-bold text-slate-900">{p.unitsSold}</td>
                        <td className="font-bold text-blue-600">₹{p.revenueGenerated.toFixed(2)}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}
          </div>
        </div>
      </div>

      {/* Recent Orders Table */}
      <div className="bg-white rounded-2xl border border-slate-200 p-5 shadow-xs">
        <div className="flex items-center justify-between mb-4">
          <div className="flex items-center gap-2">
            <Clock size={18} className="text-slate-500" />
            <h3 className="font-bold text-slate-900 text-sm">Recent Orders Pipeline</h3>
          </div>
          <button
            onClick={() => onNavigateTab('orders')}
            className="text-xs text-blue-600 hover:text-blue-800 font-semibold"
          >
            All Orders ({recentOrders.length}) →
          </button>
        </div>

        {recentOrders.length === 0 ? (
          <div className="p-8 text-center text-slate-400 text-xs">
            No orders placed yet. Open the AI Chatbot or Storefront to place your first order.
          </div>
        ) : (
          <div className="table-container">
            <table>
              <thead>
                <tr>
                  <th>Order #</th>
                  <th>Customer</th>
                  <th>Date</th>
                  <th>Items</th>
                  <th>Grand Total</th>
                  <th>Status</th>
                </tr>
              </thead>
              <tbody>
                {recentOrders.map((ord) => (
                  <tr key={ord.id}>
                    <td className="font-mono font-bold text-blue-600">{ord.orderNumber}</td>
                    <td className="font-medium text-slate-800">{ord.customerName}</td>
                    <td className="text-slate-500 text-xs">{new Date(ord.orderDate).toLocaleString()}</td>
                    <td className="text-slate-600">{ord.items.length} item(s)</td>
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
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>
    </div>
  );
};
