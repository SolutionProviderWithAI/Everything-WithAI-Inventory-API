import React, { useEffect, useState } from 'react';
import { Package, Shield, User, Bot, ShoppingBag, LayoutDashboard, RefreshCw } from 'lucide-react';
import { api } from '../services/api';

interface NavbarProps {
  currentRole: 'Admin' | 'Staff' | 'Customer';
  onRoleChange: (role: 'Admin' | 'Staff' | 'Customer') => void;
  activeView: 'admin' | 'storefront';
  onViewChange: (view: 'admin' | 'storefront') => void;
}

export const Navbar: React.FC<NavbarProps> = ({
  currentRole,
  onRoleChange,
  activeView,
  onViewChange
}) => {
  const [backendOnline, setBackendOnline] = useState<boolean | null>(null);
  const [chatbotOnline, setChatbotOnline] = useState<boolean | null>(null);

  const checkHealth = async () => {
    const bOk = await api.checkBackendHealth();
    const cOk = await api.checkChatbotHealth();
    setBackendOnline(bOk);
    setChatbotOnline(cOk);
  };

  useEffect(() => {
    checkHealth();
    const interval = setInterval(checkHealth, 15000);
    return () => clearInterval(interval);
  }, []);

  return (
    <header className="bg-white border-b border-slate-200 sticky top-0 z-30 px-6 py-3.5 flex items-center justify-between">
      <div className="flex items-center gap-3">
        <div className="bg-blue-600 text-white p-2 rounded-lg shadow-sm flex items-center justify-center">
          <Package size={22} />
        </div>
        <div>
          <h1 className="text-base font-bold text-slate-900 leading-tight flex items-center gap-2">
            Apex Inventory & AI Commerce
            <span className="text-xs font-semibold px-2 py-0.5 bg-blue-50 text-blue-700 rounded border border-blue-200">
              .NET 10 + FastAPI
            </span>
          </h1>
          <p className="text-xs text-slate-500">Autonomous Order Processing & Real-time Stock Reservation</p>
        </div>
      </div>

      <div className="flex items-center gap-4">
        {/* Backend & Chatbot Status Badges */}
        <div className="hidden md:flex items-center gap-3 text-xs">
          <div className="flex items-center gap-1.5 px-2.5 py-1 rounded-full bg-slate-100 border border-slate-200">
            <span className={`w-2 h-2 rounded-full ${backendOnline ? 'bg-emerald-500 animate-pulse' : 'bg-rose-500'}`} />
            <span className="text-slate-600 font-medium">.NET API:</span>
            <span className={backendOnline ? 'text-emerald-700 font-semibold' : 'text-rose-600 font-semibold'}>
              {backendOnline === null ? 'Checking...' : backendOnline ? 'Online (5000)' : 'Offline'}
            </span>
          </div>

          <div className="flex items-center gap-1.5 px-2.5 py-1 rounded-full bg-slate-100 border border-slate-200">
            <span className={`w-2 h-2 rounded-full ${chatbotOnline ? 'bg-emerald-500 animate-pulse' : 'bg-amber-500'}`} />
            <span className="text-slate-600 font-medium">AI Chatbot:</span>
            <span className={chatbotOnline ? 'text-emerald-700 font-semibold' : 'text-amber-600 font-semibold'}>
              {chatbotOnline === null ? 'Checking...' : chatbotOnline ? 'Online (8000)' : 'Offline'}
            </span>
          </div>

          <button onClick={checkHealth} title="Refresh connection status" className="text-slate-400 hover:text-slate-700">
            <RefreshCw size={14} />
          </button>
        </div>

        {/* View Toggle (Storefront vs Admin Dashboard) */}
        <div className="flex items-center bg-slate-100 p-1 rounded-lg border border-slate-200">
          <button
            onClick={() => onViewChange('admin')}
            className={`flex items-center gap-1.5 px-3 py-1 text-xs font-semibold rounded-md transition ${
              activeView === 'admin' ? 'bg-white text-blue-700 shadow-sm' : 'text-slate-600 hover:text-slate-900'
            }`}
          >
            <LayoutDashboard size={14} />
            Admin Dashboard
          </button>
          <button
            onClick={() => onViewChange('storefront')}
            className={`flex items-center gap-1.5 px-3 py-1 text-xs font-semibold rounded-md transition ${
              activeView === 'storefront' ? 'bg-white text-blue-700 shadow-sm' : 'text-slate-600 hover:text-slate-900'
            }`}
          >
            <ShoppingBag size={14} />
            Storefront & Chat
          </button>
        </div>

        {/* Role Switcher */}
        <div className="flex items-center gap-2 border-l border-slate-200 pl-4">
          <Shield size={16} className="text-slate-400" />
          <span className="text-xs font-medium text-slate-500">Role:</span>
          <select
            value={currentRole}
            onChange={(e) => onRoleChange(e.target.value as any)}
            className="text-xs font-semibold bg-slate-50 border border-slate-200 rounded-md px-2.5 py-1.5 text-slate-800 cursor-pointer focus:outline-none focus:ring-1 focus:ring-blue-500"
          >
            <option value="Admin">Administrator</option>
            <option value="Staff">Warehouse Staff</option>
            <option value="Customer">Customer</option>
          </select>
        </div>
      </div>
    </header>
  );
};
