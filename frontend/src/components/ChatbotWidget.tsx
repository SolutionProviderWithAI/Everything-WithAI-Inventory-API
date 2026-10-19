import React, { useState, useEffect, useRef } from 'react';
import {
  MessageSquare,
  X,
  Send,
  RotateCcw,
  CheckCircle2,
  AlertCircle,
  PackageCheck,
  Bot,
  User as UserIcon,
  Sparkles,
  ShoppingBag
} from 'lucide-react';
import { chatbotService } from '../services/api';
import { ChatMessage } from '../types';

interface ChatbotWidgetProps {
  currentCustomerName?: string;
  currentCustomerId?: string;
  onOrderPlaced?: (orderNumber: string) => void;
}

export const ChatbotWidget: React.FC<ChatbotWidgetProps> = ({
  currentCustomerName = 'John Doe',
  currentCustomerId,
  onOrderPlaced
}) => {
  const [isOpen, setIsOpen] = useState(false);
  const [sessionId] = useState<string>(() => `sess_${Math.random().toString(36).substring(2, 9)}`);
  const [messages, setMessages] = useState<ChatMessage[]>([]);
  const [inputValue, setInputValue] = useState('');
  const [isLoading, setIsLoading] = useState(false);
  const messagesEndRef = useRef<HTMLDivElement>(null);

  const scrollToBottom = () => {
    messagesEndRef.current?.scrollIntoView({ behavior: 'smooth' });
  };

  useEffect(() => {
    if (isOpen && messages.length === 0) {
      // Send initial greeting
      setMessages([
        {
          id: 'welcome',
          sender: 'bot',
          text: `👋 Hello ${currentCustomerName}! I am your AI Order & Inventory Assistant. You can ask me to search products, check stock, calculate order prices, or place orders with real-time stock reservation.`,
          suggestions: [
            'Show me all available products',
            'How many units of Product A are available?',
            'I need 10 units of Product A',
            'Can I order 100 units if only 50 are available?'
          ],
          timestamp: new Date()
        }
      ]);
    }
  }, [isOpen]);

  useEffect(() => {
    const handleOpenQuery = (e: any) => {
      setIsOpen(true);
      if (e.detail?.query) {
        setTimeout(() => {
          handleSendMessage(e.detail.query);
        }, 150);
      }
    };
    window.addEventListener('chatbot-open-query', handleOpenQuery);
    return () => window.removeEventListener('chatbot-open-query', handleOpenQuery);
  }, [sessionId, isLoading]);

  useEffect(() => {
    if (isOpen) {
      scrollToBottom();
    }
  }, [messages, isOpen]);

  const handleSendMessage = async (textToSend?: string) => {
    const text = (textToSend || inputValue).trim();
    if (!text || isLoading) return;

    const userMessage: ChatMessage = {
      id: `usr_${Date.now()}`,
      sender: 'user',
      text,
      timestamp: new Date()
    };

    setMessages((prev) => [...prev, userMessage]);
    setInputValue('');
    setIsLoading(true);

    try {
      const response = await chatbotService.sendMessage(
        sessionId,
        text,
        currentCustomerId,
        currentCustomerName,
        '+91 9876543210',
        'Flat 402, Lotus Grandeur, Andheri East, Mumbai 400069'
      );

      const botMessage: ChatMessage = {
        id: `bot_${Date.now()}`,
        sender: 'bot',
        text: response.message,
        intent: response.intent,
        awaitingConfirmation: response.awaiting_confirmation,
        orderSummary: response.order_summary,
        orderNumber: response.order_number,
        suggestions: response.suggestions || [],
        timestamp: new Date(response.timestamp || Date.now())
      };

      setMessages((prev) => [...prev, botMessage]);

      if (response.order_number && onOrderPlaced) {
        onOrderPlaced(response.order_number);
      }
    } catch (err: any) {
      const errMsg: ChatMessage = {
        id: `err_${Date.now()}`,
        sender: 'system',
        text: `⚠️ Could not reach AI Chatbot service (${err.message}). Please ensure Python service is running on port 8000 and .NET backend on 5000.`,
        timestamp: new Date()
      };
      setMessages((prev) => [...prev, errMsg]);
    } finally {
      setIsLoading(false);
    }
  };

  const handleReset = async () => {
    try {
      await chatbotService.resetSession(sessionId);
    } catch (e) {
      console.warn(e);
    }
    setMessages([
      {
        id: 'reset',
        sender: 'bot',
        text: 'Session reset! How can I assist you with your orders today?',
        suggestions: ['Show me all available products', 'Check stock of Product A'],
        timestamp: new Date()
      }
    ]);
  };

  return (
    <>
      {/* Floating Action Button */}
      {!isOpen && (
        <button
          onClick={() => setIsOpen(true)}
          className="chatbot-fab"
          title="Open AI Ordering Assistant"
        >
          <div className="relative">
            <Bot size={28} />
            <span className="absolute -top-1 -right-1 flex h-3 w-3">
              <span className="animate-ping absolute inline-flex h-full w-full rounded-full bg-emerald-400 opacity-75"></span>
              <span className="relative inline-flex rounded-full h-3 w-3 bg-emerald-500"></span>
            </span>
          </div>
        </button>
      )}

      {/* Floating Chatbot Window */}
      {isOpen && (
        <div className="chatbot-panel">
          {/* Header */}
          <div className="bg-gradient-to-r from-blue-600 to-indigo-700 text-white px-4 py-3 flex items-center justify-between shadow-sm">
            <div className="flex items-center gap-2.5">
              <div className="bg-white/15 p-1.5 rounded-lg backdrop-blur-xs flex items-center justify-center">
                <Sparkles size={18} className="text-yellow-300" />
              </div>
              <div>
                <h2 className="text-sm font-bold flex items-center gap-1.5">
                  AI Order Assistant
                  <span className="text-[10px] bg-emerald-500/30 text-emerald-100 px-1.5 py-0.2 rounded font-semibold border border-emerald-400/30">
                    Live NLU
                  </span>
                </h2>
                <p className="text-[11px] text-blue-100/90">Autonomous Inventory & Order Processing</p>
              </div>
            </div>

            <div className="flex items-center gap-1">
              <button
                onClick={handleReset}
                title="Reset session"
                className="text-white/70 hover:text-white p-1 rounded hover:bg-white/10 transition"
              >
                <RotateCcw size={16} />
              </button>
              <button
                onClick={() => setIsOpen(false)}
                title="Close chat"
                className="text-white/70 hover:text-white p-1 rounded hover:bg-white/10 transition"
              >
                <X size={18} />
              </button>
            </div>
          </div>

          {/* Messages Thread */}
          <div className="flex-1 overflow-y-auto p-4 space-y-3.5 bg-slate-50 text-xs">
            {messages.map((msg) => {
              const isUser = msg.sender === 'user';
              const isSystem = msg.sender === 'system';

              if (isSystem) {
                return (
                  <div key={msg.id} className="p-2.5 rounded-lg bg-rose-50 border border-rose-200 text-rose-700 flex items-start gap-2">
                    <AlertCircle size={16} className="shrink-0 mt-0.5 text-rose-600" />
                    <span>{msg.text}</span>
                  </div>
                );
              }

              return (
                <div
                  key={msg.id}
                  className={`flex gap-2.5 ${isUser ? 'justify-end' : 'justify-start'}`}
                >
                  {!isUser && (
                    <div className="w-7 h-7 rounded-full bg-blue-100 text-blue-700 flex items-center justify-center shrink-0 mt-0.5 border border-blue-200">
                      <Bot size={15} />
                    </div>
                  )}

                  <div className={`max-w-[85%] space-y-2`}>
                    <div
                      className={`p-3 rounded-2xl ${
                        isUser
                          ? 'bg-blue-600 text-white rounded-br-xs shadow-xs'
                          : 'bg-white text-slate-800 border border-slate-200/80 rounded-bl-xs shadow-xs'
                      }`}
                    >
                      <div className="whitespace-pre-line leading-relaxed">
                        {msg.text}
                      </div>

                      {/* Render Order Calculation Summary Card if present */}
                      {msg.orderSummary && (
                        <div className="mt-3 pt-3 border-t border-slate-100 bg-slate-50/80 p-2.5 rounded-xl text-slate-700 space-y-1.5">
                          <p className="font-bold text-slate-900 flex items-center gap-1.5 text-[11px]">
                            <ShoppingBag size={13} className="text-blue-600" />
                            Live Server Price Breakdown:
                          </p>
                          <div className="space-y-1 text-[11px]">
                            {msg.orderSummary.items.map((it, idx) => (
                              <div key={idx} className="flex justify-between items-center text-slate-600">
                                <span>{it.name} ({it.quantity}x @ ₹{it.unit_price})</span>
                                <span className="font-semibold text-slate-800">₹{it.line_total.toFixed(2)}</span>
                              </div>
                            ))}
                          </div>
                          <div className="border-t border-slate-200 pt-1.5 space-y-0.5 text-[11px]">
                            <div className="flex justify-between text-slate-500">
                              <span>Subtotal</span>
                              <span>₹{msg.orderSummary.subtotal.toFixed(2)}</span>
                            </div>
                            <div className="flex justify-between text-slate-500">
                              <span>GST Tax ({(msg.orderSummary.tax_rate * 100).toFixed(0)}%)</span>
                              <span>₹{msg.orderSummary.tax_amount.toFixed(2)}</span>
                            </div>
                            <div className="flex justify-between text-slate-500">
                              <span>Shipping Charges</span>
                              <span className={msg.orderSummary.shipping_charges === 0 ? 'text-emerald-600 font-semibold' : ''}>
                                {msg.orderSummary.shipping_charges === 0 ? 'FREE' : `₹${msg.orderSummary.shipping_charges.toFixed(2)}`}
                              </span>
                            </div>
                            <div className="flex justify-between font-bold text-slate-900 pt-1 text-xs border-t border-slate-200">
                              <span>Final Payable</span>
                              <span className="text-blue-600">₹{msg.orderSummary.grand_total.toFixed(2)}</span>
                            </div>
                          </div>
                        </div>
                      )}

                      {/* Render Order Confirmation Action Buttons */}
                      {msg.awaitingConfirmation && (
                        <div className="mt-3 pt-2 flex items-center gap-2">
                          <button
                            onClick={() => handleSendMessage('Confirm')}
                            className="flex-1 bg-emerald-600 hover:bg-emerald-700 text-white font-bold py-1.5 px-3 rounded-lg flex items-center justify-center gap-1.5 text-xs shadow-xs transition"
                          >
                            <CheckCircle2 size={14} />
                            Confirm Order
                          </button>
                          <button
                            onClick={() => handleSendMessage('Cancel order draft')}
                            className="bg-slate-200 hover:bg-slate-300 text-slate-700 font-semibold py-1.5 px-3 rounded-lg text-xs transition"
                          >
                            Cancel
                          </button>
                        </div>
                      )}

                      {/* Render Placed Order Badge */}
                      {msg.orderNumber && (
                        <div className="mt-3 p-2 bg-emerald-50 border border-emerald-200 rounded-lg text-emerald-800 text-[11px] flex items-center gap-2">
                          <PackageCheck size={18} className="text-emerald-600 shrink-0" />
                          <div>
                            <p className="font-bold">Order Number: {msg.orderNumber}</p>
                            <p className="text-[10px] text-emerald-700">Database reservation complete. Ready for dispatch.</p>
                          </div>
                        </div>
                      )}
                    </div>

                    {/* Suggestions Pill Buttons */}
                    {msg.suggestions && msg.suggestions.length > 0 && (
                      <div className="flex flex-wrap gap-1.5 pt-1">
                        {msg.suggestions.map((sug, i) => (
                          <button
                            key={i}
                            onClick={() => handleSendMessage(sug)}
                            className="text-[11px] bg-white hover:bg-blue-50 text-slate-700 hover:text-blue-700 border border-slate-200 hover:border-blue-300 px-2.5 py-1 rounded-full font-medium transition shadow-2xs"
                          >
                            {sug}
                          </button>
                        ))}
                      </div>
                    )}
                  </div>

                  {isUser && (
                    <div className="w-7 h-7 rounded-full bg-slate-800 text-white flex items-center justify-center shrink-0 mt-0.5">
                      <UserIcon size={14} />
                    </div>
                  )}
                </div>
              );
            })}

            {isLoading && (
              <div className="flex gap-2 items-center text-slate-400 text-xs italic">
                <div className="w-6 h-6 rounded-full bg-blue-50 flex items-center justify-center">
                  <Bot size={14} className="text-blue-500 animate-spin" />
                </div>
                <span>Checking catalog & calculating stock...</span>
              </div>
            )}
            <div ref={messagesEndRef} />
          </div>

          {/* Input Bar */}
          <form
            onSubmit={(e) => {
              e.preventDefault();
              handleSendMessage();
            }}
            className="p-3 bg-white border-t border-slate-200 flex items-center gap-2"
          >
            <input
              type="text"
              value={inputValue}
              onChange={(e) => setInputValue(e.target.value)}
              placeholder="Ask about products, stock, or say 'Order 10 units'..."
              className="flex-1 bg-slate-50 border border-slate-200 rounded-lg px-3 py-2 text-xs focus:outline-none focus:ring-1 focus:ring-blue-500 text-slate-800"
              disabled={isLoading}
            />
            <button
              type="submit"
              disabled={isLoading || !inputValue.trim()}
              className="bg-blue-600 hover:bg-blue-700 disabled:opacity-40 text-white p-2 rounded-lg transition"
            >
              <Send size={15} />
            </button>
          </form>
        </div>
      )}
    </>
  );
};
