import React, { useState, useEffect } from 'react';
import { Navbar } from './components/Navbar';
import { Sidebar, NavTab } from './components/Sidebar';
import { ChatbotWidget } from './components/ChatbotWidget';

// Admin Pages
import { DashboardOverview } from './pages/DashboardOverview';
import { ProductManagement } from './pages/ProductManagement';
import { InventoryManagement } from './pages/InventoryManagement';
import { StockMovements } from './pages/StockMovements';
import { OrderManagement } from './pages/OrderManagement';
import { CustomerManagement } from './pages/CustomerManagement';
import { SupplierManagement } from './pages/SupplierManagement';
import { ReportsAnalytics } from './pages/ReportsAnalytics';
import { ChatbotHistory } from './pages/ChatbotHistory';
import { SystemSettings } from './pages/SystemSettings';

// Storefront Page
import { CustomerStorefront } from './pages/CustomerStorefront';

import { api } from './services/api';

export const App: React.FC = () => {
  const [currentRole, setCurrentRole] = useState<'Admin' | 'Staff' | 'Customer'>('Admin');
  const [activeView, setActiveView] = useState<'admin' | 'storefront'>('admin');
  const [currentTab, setCurrentTab] = useState<NavTab>('overview');
  const [pendingOrdersCount, setPendingOrdersCount] = useState<number>(0);
  const [lowStockCount, setLowStockCount] = useState<number>(0);
  const [placedOrderAlert, setPlacedOrderAlert] = useState<string | null>(null);

  const fetchBadgeCounts = async () => {
    try {
      const [ordersRes, lowStockRes] = await Promise.all([
        api.getOrders('PendingConfirmation', 1, 10).catch(() => ({ totalCount: 0 })),
        api.getLowStockAlerts().catch(() => [])
      ]);
      setPendingOrdersCount(ordersRes.totalCount || 0);
      setLowStockCount(lowStockRes.length || 0);
    } catch {
      // Ignore background badge errors
    }
  };

  useEffect(() => {
    fetchBadgeCounts();
    const interval = setInterval(fetchBadgeCounts, 20000);
    return () => clearInterval(interval);
  }, []);

  const handleRoleChange = (role: 'Admin' | 'Staff' | 'Customer') => {
    setCurrentRole(role);
    if (role === 'Customer') {
      setActiveView('storefront');
    } else {
      setActiveView('admin');
    }
  };

  const handleOrderPlaced = (orderNumber: string) => {
    setPlacedOrderAlert(`Order ${orderNumber} placed successfully and inventory reserved!`);
    fetchBadgeCounts();
    setTimeout(() => setPlacedOrderAlert(null), 8000);
  };

  const renderContent = () => {
    if (activeView === 'storefront') {
      return (
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
          <CustomerStorefront
            onOpenChatWithQuery={(query) => {
              window.dispatchEvent(new CustomEvent('chatbot-open-query', { detail: { query } }));
            }}
          />
        </div>
      );
    }

    // Admin Dashboard View with Sidebar
    return (
      <div className="flex flex-1 overflow-hidden">
        <Sidebar
          currentTab={currentTab}
          onTabSelect={(tab) => setCurrentTab(tab)}
          pendingOrdersCount={pendingOrdersCount}
          lowStockCount={lowStockCount}
        />

        <main className="flex-1 overflow-y-auto bg-slate-100/70 p-6 md:p-8">
          <div className="max-w-7xl mx-auto">
            {currentTab === 'overview' && (
              <DashboardOverview
                onNavigateTab={(tab) => setCurrentTab(tab)}
              />
            )}
            {currentTab === 'products' && <ProductManagement />}
            {currentTab === 'inventory' && <InventoryManagement />}
            {currentTab === 'movements' && <StockMovements />}
            {currentTab === 'orders' && <OrderManagement />}
            {currentTab === 'customers' && <CustomerManagement />}
            {currentTab === 'suppliers' && <SupplierManagement />}
            {currentTab === 'reports' && <ReportsAnalytics />}
            {currentTab === 'chatbot' && <ChatbotHistory />}
            {currentTab === 'settings' && <SystemSettings />}
          </div>
        </main>
      </div>
    );
  };

  return (
    <div className="min-h-screen flex flex-col bg-slate-50 text-slate-800 font-sans antialiased">
      {/* Top Navigation */}
      <Navbar
        currentRole={currentRole}
        onRoleChange={handleRoleChange}
        activeView={activeView}
        onViewChange={(v) => setActiveView(v)}
      />

      {/* Order Placed Floating Alert */}
      {placedOrderAlert && (
        <div className="fixed top-20 right-6 z-50 bg-emerald-600 text-white px-5 py-3 rounded-xl shadow-xl flex items-center gap-3 animate-in fade-in slide-in-from-top-4">
          <span className="w-2.5 h-2.5 rounded-full bg-emerald-300 animate-ping" />
          <span className="text-sm font-semibold">{placedOrderAlert}</span>
          <button
            onClick={() => setPlacedOrderAlert(null)}
            className="text-emerald-200 hover:text-white font-bold ml-2"
          >
            &times;
          </button>
        </div>
      )}

      {/* Main Body */}
      {renderContent()}

      {/* Floating Autonomous AI Chatbot */}
      <ChatbotWidget
        currentCustomerName={currentRole === 'Customer' ? 'Jane Customer' : 'Admin User'}
        currentCustomerId={currentRole === 'Customer' ? 'c0000000-0000-0000-0000-000000000001' : undefined}
        onOrderPlaced={handleOrderPlaced}
      />
    </div>
  );
};

export default App;
