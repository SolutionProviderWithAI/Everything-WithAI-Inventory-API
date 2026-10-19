import React, { useEffect, useState } from 'react';
import { Settings, ShieldCheck, Save, RefreshCw, Clock, User, FileText, CheckCircle2 } from 'lucide-react';
import { api } from '../services/api';

export const SystemSettings: React.FC = () => {
  const [activeTab, setActiveTab] = useState<'settings' | 'audit'>('settings');
  const [settings, setSettings] = useState<any[]>([]);
  const [auditLogs, setAuditLogs] = useState<any[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [savingKey, setSavingKey] = useState<string | null>(null);
  const [saveSuccess, setSaveSuccess] = useState<string | null>(null);

  // Edit values buffer
  const [editValues, setEditValues] = useState<Record<string, string>>({});

  const loadData = async () => {
    try {
      setLoading(true);
      setError(null);
      const [sets, logs] = await Promise.all([
        api.getSettings(),
        api.getAuditLogs(1, 100)
      ]);
      setSettings(sets);
      setAuditLogs(logs.items || []);

      const buf: Record<string, string> = {};
      sets.forEach((s: any) => {
        buf[s.settingKey] = s.settingValue;
      });
      setEditValues(buf);
    } catch (err: any) {
      setError(err.message || 'Failed to load system settings');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    loadData();
  }, []);

  const handleSaveSetting = async (key: string, description?: string) => {
    try {
      setSavingKey(key);
      setError(null);
      await api.updateSetting(key, editValues[key], description);
      setSaveSuccess(key);
      setTimeout(() => setSaveSuccess(null), 3000);
      await loadData();
    } catch (err: any) {
      setError(err.message || `Failed to update setting ${key}`);
    } finally {
      setSavingKey(null);
    }
  };

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex flex-col sm:flex-row justify-between items-start sm:items-center gap-4">
        <div>
          <h2 className="text-xl font-bold text-slate-900 flex items-center gap-2">
            <Settings className="text-blue-600" size={24} />
            System Configuration & Audit Trail
          </h2>
          <p className="text-sm text-slate-500">
            Configure server-side tax rules, shipping parameters, and inspect immutable audit logs.
          </p>
        </div>

        <button
          onClick={loadData}
          className="flex items-center gap-1.5 px-3 py-2 text-sm font-medium text-slate-600 bg-white border border-slate-200 rounded-lg hover:bg-slate-50 shadow-xs"
        >
          <RefreshCw size={15} />
          Refresh
        </button>
      </div>

      {error && (
        <div className="p-4 bg-rose-50 border border-rose-200 text-rose-700 rounded-lg text-sm">
          {error}
        </div>
      )}

      {/* Tabs */}
      <div className="flex border-b border-slate-200 bg-white px-4 rounded-t-xl">
        <button
          onClick={() => setActiveTab('settings')}
          className={`py-3 px-4 text-sm font-semibold border-b-2 transition ${
            activeTab === 'settings'
              ? 'border-blue-600 text-blue-600'
              : 'border-transparent text-slate-500 hover:text-slate-800'
          }`}
        >
          Application Parameters
        </button>
        <button
          onClick={() => setActiveTab('audit')}
          className={`py-3 px-4 text-sm font-semibold border-b-2 transition ${
            activeTab === 'audit'
              ? 'border-blue-600 text-blue-600'
              : 'border-transparent text-slate-500 hover:text-slate-800'
          }`}
        >
          Immutable Audit Logs ({auditLogs.length})
        </button>
      </div>

      {/* Tab: Settings */}
      {activeTab === 'settings' && (
        <div className="bg-white rounded-b-xl border border-slate-200 shadow-xs p-6 space-y-6">
          <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
            {settings.map((s) => (
              <div key={s.settingKey} className="p-4 rounded-lg border border-slate-200 bg-slate-50/50 space-y-3">
                <div className="flex justify-between items-start">
                  <div>
                    <h4 className="font-mono text-sm font-bold text-slate-800">{s.settingKey}</h4>
                    <p className="text-xs text-slate-500 mt-0.5">{s.description || 'Configurable business parameter'}</p>
                  </div>
                  {saveSuccess === s.settingKey && (
                    <span className="flex items-center gap-1 text-xs font-semibold text-emerald-600 animate-in fade-in">
                      <CheckCircle2 size={14} /> Saved
                    </span>
                  )}
                </div>

                <div className="flex gap-2 items-center">
                  <input
                    type="text"
                    value={editValues[s.settingKey] ?? s.settingValue}
                    onChange={(e) => setEditValues({ ...editValues, [s.settingKey]: e.target.value })}
                    className="flex-1 px-3 py-1.5 text-sm font-semibold border border-slate-300 rounded-lg bg-white focus:ring-2 focus:ring-blue-500"
                  />
                  <button
                    onClick={() => handleSaveSetting(s.settingKey, s.description)}
                    disabled={savingKey === s.settingKey}
                    className="flex items-center gap-1 px-3 py-1.5 text-xs font-semibold text-white bg-blue-600 hover:bg-blue-700 rounded-lg disabled:opacity-50"
                  >
                    <Save size={14} />
                    {savingKey === s.settingKey ? 'Saving...' : 'Save'}
                  </button>
                </div>
              </div>
            ))}
          </div>
        </div>
      )}

      {/* Tab: Audit Logs */}
      {activeTab === 'audit' && (
        <div className="bg-white rounded-b-xl border border-slate-200 shadow-xs overflow-hidden">
          <div className="overflow-x-auto">
            <table className="w-full text-left text-sm text-slate-600">
              <thead className="bg-slate-50 text-slate-500 text-xs font-semibold uppercase tracking-wider border-b border-slate-200">
                <tr>
                  <th className="px-5 py-3">Timestamp</th>
                  <th className="px-5 py-3">Action</th>
                  <th className="px-5 py-3">Entity</th>
                  <th className="px-5 py-3">User</th>
                  <th className="px-5 py-3">Details / Changes</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-100">
                {loading ? (
                  <tr>
                    <td colSpan={5} className="px-5 py-8 text-center text-slate-400">
                      Loading audit logs...
                    </td>
                  </tr>
                ) : auditLogs.length === 0 ? (
                  <tr>
                    <td colSpan={5} className="px-5 py-8 text-center text-slate-400">
                      No audit events logged.
                    </td>
                  </tr>
                ) : (
                  auditLogs.map((log) => (
                    <tr key={log.id} className="hover:bg-slate-50 transition">
                      <td className="px-5 py-3 text-xs text-slate-500 whitespace-nowrap">
                        {new Date(log.createdAt).toLocaleString()}
                      </td>
                      <td className="px-5 py-3">
                        <span className="font-mono text-xs font-bold px-2 py-0.5 rounded bg-slate-100 text-slate-800">
                          {log.action}
                        </span>
                      </td>
                      <td className="px-5 py-3 text-xs font-semibold text-slate-800">
                        {log.entityName}
                      </td>
                      <td className="px-5 py-3 text-xs text-slate-600">
                        {log.performedBy || 'System'}
                      </td>
                      <td className="px-5 py-3 text-xs text-slate-600 max-w-md truncate font-mono">
                        {log.details || log.newValues || '—'}
                      </td>
                    </tr>
                  ))
                )}
              </tbody>
            </table>
          </div>
        </div>
      )}
    </div>
  );
};
