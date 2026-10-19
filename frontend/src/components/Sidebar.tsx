import React from 'react';
import {
  LayoutDashboard,
  Package,
  Boxes,
  ArrowLeftRight,
  ShoppingCart,
  Users,
  Truck,
  BarChart3,
  MessageSquare,
  Settings
} from 'lucide-react';

export type NavTab =
  | 'overview'
  | 'products'
  | 'inventory'
  | 'movements'
  | 'orders'
  | 'customers'
  | 'suppliers'
  | 'reports'
  | 'chatbot'
  | 'settings';

interface SidebarProps {
  currentTab: NavTab;
  onTabSelect: (tab: NavTab) => void;
  pendingOrdersCount?: number;
  lowStockCount?: number;
}

export const Sidebar: React.FC<SidebarProps> = ({
  currentTab,
  onTabSelect,
  pendingOrdersCount = 0,
  lowStockCount = 0
}) => {
  const navItems: Array<{ id: NavTab; label: string; icon: React.ReactNode; badge?: number; badgeColor?: string }> = [
    { id: 'overview', label: 'Dashboard Overview', icon: <LayoutDashboard size={18} /> },
    { id: 'products', label: 'Product Catalog', icon: <Package size={18} /> },
    {
      id: 'inventory',
      label: 'Inventory Control',
      icon: <Boxes size={18} />,
      badge: lowStockCount > 0 ? lowStockCount : undefined,
      badgeColor: 'bg-amber-100 text-amber-800'
    },
    { id: 'movements', label: 'Stock Movements', icon: <ArrowLeftRight size={18} /> },
    {
      id: 'orders',
      label: 'Order Management',
      icon: <ShoppingCart size={18} />,
      badge: pendingOrdersCount > 0 ? pendingOrdersCount : undefined,
      badgeColor: 'bg-blue-100 text-blue-800'
    },
    { id: 'customers', label: 'Customers', icon: <Users size={18} /> },
    { id: 'suppliers', label: 'Suppliers & POs', icon: <Truck size={18} /> },
    { id: 'reports', label: 'Reports & Analytics', icon: <BarChart3 size={18} /> },
    { id: 'chatbot', label: 'Chatbot Conversations', icon: <MessageSquare size={18} /> },
    { id: 'settings', label: 'Settings & Audit Logs', icon: <Settings size={18} /> },
  ];

  return (
    <aside className="w-64 bg-white border-r border-slate-200 flex flex-col shrink-0 min-h-[calc(100vh-65px)]">
      <div className="p-4 border-b border-slate-100">
        <p className="text-xs font-semibold uppercase tracking-wider text-slate-400">Management Modules</p>
      </div>

      <nav className="p-3 space-y-1 flex-1">
        {navItems.map((item) => {
          const isActive = currentTab === item.id;
          return (
            <button
              key={item.id}
              onClick={() => onTabSelect(item.id)}
              className={`w-full flex items-center justify-between px-3 py-2.5 rounded-lg text-sm font-medium transition ${
                isActive
                  ? 'bg-blue-50 text-blue-700 font-semibold shadow-xs'
                  : 'text-slate-600 hover:bg-slate-50 hover:text-slate-900'
              }`}
            >
              <div className="flex items-center gap-3">
                <span className={isActive ? 'text-blue-600' : 'text-slate-400'}>{item.icon}</span>
                <span>{item.label}</span>
              </div>
              {item.badge !== undefined && (
                <span className={`text-xs font-bold px-2 py-0.5 rounded-full ${item.badgeColor}`}>
                  {item.badge}
                </span>
              )}
            </button>
          );
        })}
      </nav>

      <div className="p-4 m-3 bg-slate-50 border border-slate-200 rounded-lg text-xs text-slate-600">
        <p className="font-semibold text-slate-800">Inventory Formula:</p>
        <p className="mt-1 font-mono text-[11px] text-blue-700">Available = On-Hand − Reserved</p>
        <p className="mt-1 text-[11px] text-slate-500">Atomic locking active against overselling.</p>
      </div>
    </aside>
  );
};
