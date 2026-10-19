import React, { useEffect, useState } from 'react';
import { MessageSquare, RefreshCw, User, Bot, Clock, Calendar, CheckCircle, Search } from 'lucide-react';
import { api } from '../services/api';

export const ChatbotHistory: React.FC = () => {
  const [conversations, setConversations] = useState<any[]>([]);
  const [selectedSessionId, setSelectedSessionId] = useState<string | null>(null);
  const [messages, setMessages] = useState<any[]>([]);
  const [loading, setLoading] = useState(true);
  const [loadingMessages, setLoadingMessages] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [searchTerm, setSearchTerm] = useState('');

  const loadConversations = async () => {
    try {
      setLoading(true);
      setError(null);
      const data = await api.getChatConversations();
      setConversations(data);
      if (data.length > 0 && !selectedSessionId) {
        setSelectedSessionId(data[0].sessionId);
      }
    } catch (err: any) {
      setError(err.message || 'Failed to load chatbot conversations');
    } finally {
      setLoading(false);
    }
  };

  const loadMessages = async (sessionId: string) => {
    try {
      setLoadingMessages(true);
      const msgs = await api.getChatMessages(sessionId);
      setMessages(msgs);
    } catch (err: any) {
      console.error('Failed to load session messages', err);
    } finally {
      setLoadingMessages(false);
    }
  };

  useEffect(() => {
    loadConversations();
  }, []);

  useEffect(() => {
    if (selectedSessionId) {
      loadMessages(selectedSessionId);
    }
  }, [selectedSessionId]);

  const filteredConversations = conversations.filter(c =>
    c.sessionId.toLowerCase().includes(searchTerm.toLowerCase()) ||
    (c.customerName && c.customerName.toLowerCase().includes(searchTerm.toLowerCase()))
  );

  const activeConversation = conversations.find(c => c.sessionId === selectedSessionId);

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex flex-col sm:flex-row justify-between items-start sm:items-center gap-4">
        <div>
          <h2 className="text-xl font-bold text-slate-900 flex items-center gap-2">
            <MessageSquare className="text-blue-600" size={24} />
            Chatbot Conversation Transcripts
          </h2>
          <p className="text-sm text-slate-500">
            Audit customer interactions, NLP intent recognition logs, and automated order placements.
          </p>
        </div>

        <button
          onClick={loadConversations}
          className="flex items-center gap-1.5 px-3 py-2 text-sm font-medium text-slate-600 bg-white border border-slate-200 rounded-lg hover:bg-slate-50 shadow-xs"
        >
          <RefreshCw size={15} />
          Refresh Transcripts
        </button>
      </div>

      {error && (
        <div className="p-4 bg-rose-50 border border-rose-200 text-rose-700 rounded-lg text-sm">
          {error}
        </div>
      )}

      {/* Main Two-Column View */}
      <div className="grid grid-cols-1 lg:grid-cols-12 gap-6 min-h-[550px]">
        {/* Left: Conversation List */}
        <div className="lg:col-span-5 bg-white rounded-xl border border-slate-200 shadow-xs flex flex-col overflow-hidden">
          <div className="p-3 border-b border-slate-100 bg-slate-50">
            <div className="relative">
              <Search className="absolute left-3 top-1/2 -translate-y-1/2 text-slate-400" size={14} />
              <input
                type="text"
                placeholder="Search session ID or customer..."
                value={searchTerm}
                onChange={(e) => setSearchTerm(e.target.value)}
                className="w-full pl-8 pr-3 py-1.5 text-xs border border-slate-200 rounded-lg bg-white"
              />
            </div>
          </div>

          <div className="divide-y divide-slate-100 overflow-y-auto flex-1 max-h-[600px]">
            {loading ? (
              <div className="p-8 text-center text-xs text-slate-400">Loading conversation logs...</div>
            ) : filteredConversations.length === 0 ? (
              <div className="p-8 text-center text-xs text-slate-400">No chatbot sessions recorded.</div>
            ) : (
              filteredConversations.map((conv) => {
                const isSelected = conv.sessionId === selectedSessionId;
                return (
                  <button
                    key={conv.sessionId}
                    onClick={() => setSelectedSessionId(conv.sessionId)}
                    className={`w-full text-left p-4 transition flex flex-col gap-1.5 ${
                      isSelected ? 'bg-blue-50/70 border-l-4 border-blue-600' : 'hover:bg-slate-50'
                    }`}
                  >
                    <div className="flex justify-between items-start">
                      <span className="font-semibold text-xs text-slate-800">
                        {conv.customerName || 'Anonymous Customer'}
                      </span>
                      <span className="text-[10px] text-slate-400 flex items-center gap-1">
                        <Clock size={10} />
                        {new Date(conv.startedAt).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}
                      </span>
                    </div>

                    <p className="font-mono text-[11px] text-slate-500 truncate">
                      {conv.sessionId}
                    </p>

                    <div className="flex items-center justify-between text-[11px] text-slate-500 pt-1">
                      <span>{conv.messageCount || 0} messages</span>
                      <span className="px-2 py-0.5 rounded-full text-[10px] bg-slate-100 font-medium">
                        {conv.status || 'Active'}
                      </span>
                    </div>
                  </button>
                );
              })
            )}
          </div>
        </div>

        {/* Right: Message Transcript */}
        <div className="lg:col-span-7 bg-white rounded-xl border border-slate-200 shadow-xs flex flex-col overflow-hidden">
          {activeConversation ? (
            <>
              {/* Transcript Header */}
              <div className="p-4 border-b border-slate-200 bg-slate-50 flex justify-between items-center">
                <div>
                  <h3 className="font-bold text-sm text-slate-800">
                    Session: <span className="font-mono text-blue-700">{activeConversation.sessionId}</span>
                  </h3>
                  <p className="text-xs text-slate-500">
                    Customer: {activeConversation.customerName || 'Unregistered Guest'} | Started: {new Date(activeConversation.startedAt).toLocaleString()}
                  </p>
                </div>
              </div>

              {/* Message List */}
              <div className="p-6 space-y-4 overflow-y-auto flex-1 max-h-[550px] bg-slate-50/30">
                {loadingMessages ? (
                  <div className="text-center py-12 text-xs text-slate-400">Loading transcript messages...</div>
                ) : messages.length === 0 ? (
                  <div className="text-center py-12 text-xs text-slate-400">No messages in this conversation.</div>
                ) : (
                  messages.map((msg) => {
                    const isUser = msg.senderRole === 'user' || msg.sender === 'user';
                    return (
                      <div
                        key={msg.id}
                        className={`flex gap-3 ${isUser ? 'justify-end' : 'justify-start'}`}
                      >
                        {!isUser && (
                          <div className="w-8 h-8 rounded-full bg-blue-600 text-white flex items-center justify-center shrink-0 text-xs shadow-xs">
                            <Bot size={16} />
                          </div>
                        )}

                        <div className={`max-w-[80%] rounded-2xl px-4 py-3 text-sm shadow-xs ${
                          isUser
                            ? 'bg-blue-600 text-white rounded-tr-xs'
                            : 'bg-white border border-slate-200 text-slate-800 rounded-tl-xs'
                        }`}>
                          {msg.intent && !isUser && (
                            <span className="inline-block text-[10px] font-mono font-semibold px-2 py-0.5 rounded bg-blue-50 text-blue-700 mb-1">
                              Intent: {msg.intent}
                            </span>
                          )}

                          <p className="whitespace-pre-wrap leading-relaxed">{msg.messageText || msg.text}</p>

                          <div className={`text-[10px] mt-1 text-right ${isUser ? 'text-blue-100' : 'text-slate-400'}`}>
                            {new Date(msg.sentAt || msg.timestamp).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}
                          </div>
                        </div>

                        {isUser && (
                          <div className="w-8 h-8 rounded-full bg-slate-200 text-slate-700 flex items-center justify-center shrink-0 text-xs">
                            <User size={16} />
                          </div>
                        )}
                      </div>
                    );
                  })
                )}
              </div>
            </>
          ) : (
            <div className="flex-1 flex items-center justify-center text-slate-400 text-sm">
              Select a conversation to view transcript
            </div>
          )}
        </div>
      </div>
    </div>
  );
};
