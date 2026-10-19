import React, { useEffect, useState } from 'react';
import { Package, Search, Bot, ShoppingCart, Sparkles, AlertCircle, CheckCircle2, ArrowRight } from 'lucide-react';
import { api } from '../services/api';
import { Product, Category } from '../types';

interface CustomerStorefrontProps {
  onOpenChatWithQuery?: (query: string) => void;
}

export const CustomerStorefront: React.FC<CustomerStorefrontProps> = ({ onOpenChatWithQuery }) => {
  const [products, setProducts] = useState<Product[]>([]);
  const [categories, setCategories] = useState<Category[]>([]);
  const [selectedCategory, setSelectedCategory] = useState<string>('all');
  const [searchQuery, setSearchQuery] = useState('');
  const [loading, setLoading] = useState(true);
  const [orderQuantity, setOrderQuantity] = useState<Record<string, number>>({});

  const loadStoreData = async () => {
    try {
      setLoading(true);
      const [prodData, catData] = await Promise.all([
        api.getProducts('', undefined, 1, 100),
        api.getCategories()
      ]);
      setProducts(prodData.items || []);
      setCategories(catData || []);

      const initialQty: Record<string, number> = {};
      (prodData.items || []).forEach((p: Product) => {
        initialQty[p.id] = 1;
      });
      setOrderQuantity(initialQty);
    } catch (err) {
      console.error('Failed to load storefront data', err);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    loadStoreData();
  }, []);

  const handleOrderWithChatbot = (product: Product) => {
    const qty = orderQuantity[product.id] || 1;
    const query = `I would like to order ${qty} units of ${product.name}`;
    if (onOpenChatWithQuery) {
      onOpenChatWithQuery(query);
    } else {
      // Dispatch custom event for ChatbotWidget
      window.dispatchEvent(new CustomEvent('chatbot-open-query', { detail: { query } }));
    }
  };

  const filteredProducts = products.filter(p => {
    const matchesCategory = selectedCategory === 'all' || p.categoryId === selectedCategory;
    const matchesSearch = p.name.toLowerCase().includes(searchQuery.toLowerCase()) ||
                          p.sku.toLowerCase().includes(searchQuery.toLowerCase()) ||
                          (p.description && p.description.toLowerCase().includes(searchQuery.toLowerCase()));
    return matchesCategory && matchesSearch;
  });

  return (
    <div className="space-y-8 pb-12">
      {/* Hero AI Commerce Banner */}
      <div className="relative overflow-hidden rounded-2xl bg-gradient-to-r from-blue-700 via-indigo-700 to-blue-900 text-white p-8 sm:p-10 shadow-lg">
        <div className="relative z-10 max-w-2xl space-y-3">
          <div className="inline-flex items-center gap-2 px-3 py-1 rounded-full bg-blue-500/30 text-blue-100 text-xs font-semibold backdrop-blur-xs">
            <Sparkles size={14} className="text-amber-300" />
            AI-Driven Autonomous Commerce
          </div>
          <h1 className="text-3xl sm:text-4xl font-extrabold tracking-tight">
            Order Products Instantly Using Natural Language
          </h1>
          <p className="text-blue-100 text-sm sm:text-base leading-relaxed">
            Our autonomous AI chatbot verifies real-time warehouse inventory, calculates GST taxes & discounts, and confirms orders with zero delay.
          </p>
          <div className="pt-2 flex flex-wrap gap-3">
            <button
              onClick={() => onOpenChatWithQuery ? onOpenChatWithQuery("Show me all available products and prices") : window.dispatchEvent(new CustomEvent('chatbot-open-query', { detail: { query: "Show me all available products and prices" } }))}
              className="flex items-center gap-2 px-5 py-2.5 rounded-xl bg-white text-blue-700 font-bold text-sm shadow-md hover:bg-blue-50 transition"
            >
              <Bot size={18} />
              Chat with AI Assistant
            </button>
            <button
              onClick={() => onOpenChatWithQuery ? onOpenChatWithQuery("How many units of Product A are available?") : window.dispatchEvent(new CustomEvent('chatbot-open-query', { detail: { query: "How many units of Product A are available?" } }))}
              className="flex items-center gap-2 px-5 py-2.5 rounded-xl bg-blue-600/40 text-white font-semibold text-sm border border-blue-400/40 hover:bg-blue-600/60 transition"
            >
              Check Product A Stock
              <ArrowRight size={14} />
            </button>
          </div>
        </div>

        {/* Decorative background circle */}
        <div className="absolute -right-10 -bottom-10 w-80 h-80 rounded-full bg-indigo-500/20 blur-2xl pointer-events-none" />
      </div>

      {/* Filter and Category Bar */}
      <div className="bg-white p-4 rounded-xl border border-slate-200 shadow-xs flex flex-col md:flex-row justify-between items-center gap-4">
        {/* Category Pills */}
        <div className="flex items-center gap-2 overflow-x-auto w-full md:w-auto pb-1 md:pb-0">
          <button
            onClick={() => setSelectedCategory('all')}
            className={`px-3 py-1.5 rounded-lg text-xs font-semibold whitespace-nowrap transition ${
              selectedCategory === 'all'
                ? 'bg-blue-600 text-white'
                : 'bg-slate-100 text-slate-600 hover:bg-slate-200'
            }`}
          >
            All Categories ({products.length})
          </button>
          {categories.map((c) => (
            <button
              key={c.id}
              onClick={() => setSelectedCategory(c.id)}
              className={`px-3 py-1.5 rounded-lg text-xs font-semibold whitespace-nowrap transition ${
                selectedCategory === c.id
                  ? 'bg-blue-600 text-white'
                  : 'bg-slate-100 text-slate-600 hover:bg-slate-200'
              }`}
            >
              {c.name}
            </button>
          ))}
        </div>

        {/* Search Input */}
        <div className="relative w-full md:w-72">
          <Search className="absolute left-3 top-1/2 -translate-y-1/2 text-slate-400" size={16} />
          <input
            type="text"
            placeholder="Search products..."
            value={searchQuery}
            onChange={(e) => setSearchQuery(e.target.value)}
            className="w-full pl-9 pr-4 py-2 text-xs border border-slate-200 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500"
          />
        </div>
      </div>

      {/* Product Grid */}
      {loading ? (
        <div className="text-center py-20 text-slate-400">Loading live product catalog...</div>
      ) : filteredProducts.length === 0 ? (
        <div className="text-center py-20 text-slate-400 bg-white rounded-xl border border-slate-200">
          No products found matching your search.
        </div>
      ) : (
        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-6">
          {filteredProducts.map((p) => {
            const inStock = p.availableToSell > 0;
            const isLow = p.availableToSell > 0 && p.availableToSell <= (p.reorderLevel || 10);
            const currentQty = orderQuantity[p.id] || 1;

            return (
              <div
                key={p.id}
                className="bg-white rounded-xl border border-slate-200 shadow-xs hover:shadow-md transition flex flex-col justify-between overflow-hidden group"
              >
                <div className="p-5 space-y-3">
                  {/* Category & Stock Pill */}
                  <div className="flex justify-between items-center text-xs">
                    <span className="font-semibold text-slate-400 uppercase tracking-wider text-[11px]">
                      {p.categoryName || 'General'}
                    </span>
                    {inStock ? (
                      <span className={`px-2 py-0.5 rounded-full font-semibold text-[10px] flex items-center gap-1 ${
                        isLow
                          ? 'bg-amber-50 text-amber-700 border border-amber-200'
                          : 'bg-emerald-50 text-emerald-700 border border-emerald-200'
                      }`}>
                        <CheckCircle2 size={10} />
                        {isLow ? `Low Stock (${p.availableToSell} left)` : `In Stock (${p.availableToSell})`}
                      </span>
                    ) : (
                      <span className="px-2 py-0.5 rounded-full font-semibold text-[10px] bg-rose-50 text-rose-700 border border-rose-200 flex items-center gap-1">
                        <AlertCircle size={10} /> Out of Stock
                      </span>
                    )}
                  </div>

                  {/* Title & SKU */}
                  <div>
                    <h3 className="font-bold text-slate-900 group-hover:text-blue-600 transition text-base">
                      {p.name}
                    </h3>
                    <p className="font-mono text-xs text-slate-400 mt-0.5">SKU: {p.sku}</p>
                  </div>

                  <p className="text-xs text-slate-500 line-clamp-2 leading-relaxed">
                    {p.description || 'Premium quality enterprise inventory item with immediate delivery.'}
                  </p>

                  {/* Price */}
                  <div className="pt-2 flex items-baseline gap-1">
                    <span className="text-2xl font-black text-slate-900">
                      ₹{p.sellingPrice?.toLocaleString('en-IN')}
                    </span>
                    <span className="text-xs text-slate-500">/{p.unitOfMeasurement || 'unit'}</span>
                  </div>
                </div>

                {/* Footer Actions */}
                <div className="p-4 bg-slate-50/70 border-t border-slate-100 space-y-2">
                  <div className="flex items-center gap-2">
                    <div className="flex items-center border border-slate-200 rounded-lg bg-white">
                      <button
                        type="button"
                        onClick={() => setOrderQuantity({
                          ...orderQuantity,
                          [p.id]: Math.max(1, currentQty - 1)
                        })}
                        className="px-2.5 py-1 text-slate-500 hover:text-slate-900 font-bold text-xs"
                      >
                        -
                      </button>
                      <span className="px-2 text-xs font-bold text-slate-800">{currentQty}</span>
                      <button
                        type="button"
                        onClick={() => setOrderQuantity({
                          ...orderQuantity,
                          [p.id]: currentQty + 1
                        })}
                        className="px-2.5 py-1 text-slate-500 hover:text-slate-900 font-bold text-xs"
                      >
                        +
                      </button>
                    </div>

                    <button
                      type="button"
                      disabled={!inStock}
                      onClick={() => handleOrderWithChatbot(p)}
                      className="flex-1 flex items-center justify-center gap-1.5 px-3 py-1.5 text-xs font-semibold text-white bg-blue-600 hover:bg-blue-700 rounded-lg shadow-xs transition disabled:opacity-50"
                    >
                      <Bot size={14} />
                      Order with AI Chat
                    </button>
                  </div>
                </div>
              </div>
            );
          })}
        </div>
      )}
    </div>
  );
};
