import React, { useEffect, useState } from 'react';
import { Users, Plus, Search, RefreshCw, Mail, Phone, Building, CreditCard, ShoppingBag, CheckCircle, XCircle } from 'lucide-react';
import { api } from '../services/api';
import { Customer } from '../types';

export const CustomerManagement: React.FC = () => {
  const [customers, setCustomers] = useState<Customer[]>([]);
  const [loading, setLoading] = useState(true);
  const [searchTerm, setSearchTerm] = useState('');
  const [isAddModalOpen, setIsAddModalOpen] = useState(false);
  const [selectedCustomer, setSelectedCustomer] = useState<Customer | null>(null);
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  // New Customer Form State
  const [newCustomer, setNewCustomer] = useState({
    name: '',
    email: '',
    phone: '',
    companyName: '',
    taxNumber: '',
    creditLimit: 50000,
    addressLine1: '',
    city: '',
    state: '',
    postalCode: ''
  });

  const loadCustomers = async () => {
    try {
      setLoading(true);
      setError(null);
      const data = await api.getCustomers();
      setCustomers(data);
    } catch (err: any) {
      setError(err.message || 'Failed to load customers');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    loadCustomers();
  }, []);

  const handleCreateCustomer = async (e: React.FormEvent) => {
    e.preventDefault();
    try {
      setSubmitting(true);
      setError(null);

      const payload = {
        name: newCustomer.name,
        email: newCustomer.email,
        phone: newCustomer.phone,
        companyName: newCustomer.companyName || undefined,
        taxNumber: newCustomer.taxNumber || undefined,
        creditLimit: Number(newCustomer.creditLimit),
        address: newCustomer.addressLine1 ? {
          line1: newCustomer.addressLine1,
          city: newCustomer.city,
          state: newCustomer.state,
          postalCode: newCustomer.postalCode,
          isDefault: true
        } : undefined
      };

      await api.createCustomer(payload);
      setIsAddModalOpen(false);
      setNewCustomer({
        name: '',
        email: '',
        phone: '',
        companyName: '',
        taxNumber: '',
        creditLimit: 50000,
        addressLine1: '',
        city: '',
        state: '',
        postalCode: ''
      });
      await loadCustomers();
    } catch (err: any) {
      setError(err.message || 'Failed to create customer');
    } finally {
      setSubmitting(false);
    }
  };

  const filteredCustomers = customers.filter(c =>
    c.name.toLowerCase().includes(searchTerm.toLowerCase()) ||
    c.email.toLowerCase().includes(searchTerm.toLowerCase()) ||
    c.customerCode.toLowerCase().includes(searchTerm.toLowerCase()) ||
    (c.companyName && c.companyName.toLowerCase().includes(searchTerm.toLowerCase()))
  );

  return (
    <div className="space-y-6">
      {/* Page Header */}
      <div className="flex flex-col sm:flex-row justify-between items-start sm:items-center gap-4">
        <div>
          <h2 className="text-xl font-bold text-slate-900 flex items-center gap-2">
            <Users className="text-blue-600" size={24} />
            Customer Directory
          </h2>
          <p className="text-sm text-slate-500">
            Manage customer accounts, verify credit limits, and view order frequency.
          </p>
        </div>

        <div className="flex items-center gap-3">
          <button
            onClick={loadCustomers}
            className="flex items-center gap-1.5 px-3 py-2 text-sm font-medium text-slate-600 bg-white border border-slate-200 rounded-lg hover:bg-slate-50 shadow-xs"
          >
            <RefreshCw size={15} />
            Refresh
          </button>
          <button
            onClick={() => setIsAddModalOpen(true)}
            className="flex items-center gap-2 px-4 py-2 text-sm font-semibold text-white bg-blue-600 rounded-lg hover:bg-blue-700 shadow-sm"
          >
            <Plus size={16} />
            Add Customer
          </button>
        </div>
      </div>

      {error && (
        <div className="p-4 bg-rose-50 border border-rose-200 text-rose-700 rounded-lg text-sm">
          {error}
        </div>
      )}

      {/* Filter and Search Bar */}
      <div className="bg-white p-4 rounded-xl border border-slate-200 shadow-xs">
        <div className="relative max-w-md">
          <Search className="absolute left-3 top-1/2 -translate-y-1/2 text-slate-400" size={16} />
          <input
            type="text"
            placeholder="Search by customer name, code, email, or company..."
            value={searchTerm}
            onChange={(e) => setSearchTerm(e.target.value)}
            className="w-full pl-9 pr-4 py-2 text-sm border border-slate-200 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent"
          />
        </div>
      </div>

      {/* Customer Table */}
      <div className="bg-white rounded-xl border border-slate-200 shadow-xs overflow-hidden">
        <div className="overflow-x-auto">
          <table className="w-full text-left text-sm text-slate-600">
            <thead className="bg-slate-50 text-slate-500 text-xs font-semibold uppercase tracking-wider border-b border-slate-200">
              <tr>
                <th className="px-5 py-3">Customer Code</th>
                <th className="px-5 py-3">Name & Company</th>
                <th className="px-5 py-3">Contact</th>
                <th className="px-5 py-3 text-right">Credit Limit</th>
                <th className="px-5 py-3 text-right">Outstanding</th>
                <th className="px-5 py-3 text-center">Orders</th>
                <th className="px-5 py-3 text-center">Status</th>
                <th className="px-5 py-3 text-right">Actions</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-100">
              {loading ? (
                <tr>
                  <td colSpan={8} className="px-5 py-8 text-center text-slate-400">
                    Loading customer records...
                  </td>
                </tr>
              ) : filteredCustomers.length === 0 ? (
                <tr>
                  <td colSpan={8} className="px-5 py-8 text-center text-slate-400">
                    No customers found matching your search.
                  </td>
                </tr>
              ) : (
                filteredCustomers.map((cust) => (
                  <tr key={cust.id} className="hover:bg-slate-50/80 transition">
                    <td className="px-5 py-4 font-mono font-medium text-slate-800">
                      {cust.customerCode}
                    </td>
                    <td className="px-5 py-4">
                      <div className="font-semibold text-slate-900">{cust.name}</div>
                      {cust.companyName && (
                        <div className="text-xs text-slate-500 flex items-center gap-1 mt-0.5">
                          <Building size={12} />
                          {cust.companyName}
                        </div>
                      )}
                    </td>
                    <td className="px-5 py-4">
                      <div className="flex items-center gap-1.5 text-xs text-slate-600">
                        <Mail size={12} className="text-slate-400" />
                        {cust.email}
                      </div>
                      {cust.phone && (
                        <div className="flex items-center gap-1.5 text-xs text-slate-500 mt-1">
                          <Phone size={12} className="text-slate-400" />
                          {cust.phone}
                        </div>
                      )}
                    </td>
                    <td className="px-5 py-4 text-right font-medium text-slate-800">
                      ₹{cust.creditLimit?.toLocaleString('en-IN') || '0'}
                    </td>
                    <td className="px-5 py-4 text-right font-medium">
                      <span className={cust.outstandingBalance > 0 ? 'text-amber-600 font-semibold' : 'text-slate-600'}>
                        ₹{cust.outstandingBalance?.toLocaleString('en-IN') || '0'}
                      </span>
                    </td>
                    <td className="px-5 py-4 text-center">
                      <span className="inline-flex items-center gap-1 px-2.5 py-0.5 rounded-full text-xs font-semibold bg-slate-100 text-slate-700">
                        <ShoppingBag size={12} />
                        {cust.orderCount || 0}
                      </span>
                    </td>
                    <td className="px-5 py-4 text-center">
                      {cust.isActive ? (
                        <span className="inline-flex items-center gap-1 px-2.5 py-0.5 rounded-full text-xs font-medium bg-emerald-50 text-emerald-700 border border-emerald-200">
                          <CheckCircle size={12} /> Active
                        </span>
                      ) : (
                        <span className="inline-flex items-center gap-1 px-2.5 py-0.5 rounded-full text-xs font-medium bg-slate-100 text-slate-600 border border-slate-200">
                          <XCircle size={12} /> Inactive
                        </span>
                      )}
                    </td>
                    <td className="px-5 py-4 text-right">
                      <button
                        onClick={() => setSelectedCustomer(cust)}
                        className="text-xs font-semibold text-blue-600 hover:text-blue-800 bg-blue-50 hover:bg-blue-100 px-3 py-1.5 rounded-md transition"
                      >
                        View Details
                      </button>
                    </td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>
      </div>

      {/* Add Customer Modal */}
      {isAddModalOpen && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50 p-4">
          <div className="bg-white rounded-xl shadow-xl w-full max-w-lg overflow-hidden animate-in fade-in zoom-in-95 duration-150">
            <div className="px-6 py-4 border-b border-slate-200 flex justify-between items-center bg-slate-50">
              <h3 className="font-bold text-slate-800 text-base flex items-center gap-2">
                <Users size={18} className="text-blue-600" />
                Add New Customer
              </h3>
              <button
                onClick={() => setIsAddModalOpen(false)}
                className="text-slate-400 hover:text-slate-600 text-lg font-bold"
              >
                &times;
              </button>
            </div>

            <form onSubmit={handleCreateCustomer} className="p-6 space-y-4">
              <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
                <div>
                  <label className="block text-xs font-semibold text-slate-700 mb-1">
                    Customer Name *
                  </label>
                  <input
                    type="text"
                    required
                    value={newCustomer.name}
                    onChange={(e) => setNewCustomer({ ...newCustomer, name: e.target.value })}
                    className="w-full px-3 py-2 text-sm border border-slate-200 rounded-lg focus:ring-2 focus:ring-blue-500"
                    placeholder="e.g. Rahul Sharma"
                  />
                </div>
                <div>
                  <label className="block text-xs font-semibold text-slate-700 mb-1">
                    Email Address *
                  </label>
                  <input
                    type="email"
                    required
                    value={newCustomer.email}
                    onChange={(e) => setNewCustomer({ ...newCustomer, email: e.target.value })}
                    className="w-full px-3 py-2 text-sm border border-slate-200 rounded-lg focus:ring-2 focus:ring-blue-500"
                    placeholder="rahul@example.com"
                  />
                </div>
              </div>

              <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
                <div>
                  <label className="block text-xs font-semibold text-slate-700 mb-1">
                    Phone Number *
                  </label>
                  <input
                    type="text"
                    required
                    value={newCustomer.phone}
                    onChange={(e) => setNewCustomer({ ...newCustomer, phone: e.target.value })}
                    className="w-full px-3 py-2 text-sm border border-slate-200 rounded-lg focus:ring-2 focus:ring-blue-500"
                    placeholder="+91 98765 43210"
                  />
                </div>
                <div>
                  <label className="block text-xs font-semibold text-slate-700 mb-1">
                    Company Name (Optional)
                  </label>
                  <input
                    type="text"
                    value={newCustomer.companyName}
                    onChange={(e) => setNewCustomer({ ...newCustomer, companyName: e.target.value })}
                    className="w-full px-3 py-2 text-sm border border-slate-200 rounded-lg focus:ring-2 focus:ring-blue-500"
                    placeholder="Apex Retailers"
                  />
                </div>
              </div>

              <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
                <div>
                  <label className="block text-xs font-semibold text-slate-700 mb-1">
                    Tax / GST Number
                  </label>
                  <input
                    type="text"
                    value={newCustomer.taxNumber}
                    onChange={(e) => setNewCustomer({ ...newCustomer, taxNumber: e.target.value })}
                    className="w-full px-3 py-2 text-sm border border-slate-200 rounded-lg focus:ring-2 focus:ring-blue-500"
                    placeholder="27ABCDE1234F1Z5"
                  />
                </div>
                <div>
                  <label className="block text-xs font-semibold text-slate-700 mb-1">
                    Credit Limit (₹)
                  </label>
                  <input
                    type="number"
                    min="0"
                    step="1000"
                    value={newCustomer.creditLimit}
                    onChange={(e) => setNewCustomer({ ...newCustomer, creditLimit: Number(e.target.value) })}
                    className="w-full px-3 py-2 text-sm border border-slate-200 rounded-lg focus:ring-2 focus:ring-blue-500"
                  />
                </div>
              </div>

              <div className="border-t border-slate-100 pt-3">
                <p className="text-xs font-bold text-slate-600 uppercase mb-2">Default Shipping Address</p>
                <div className="space-y-2">
                  <input
                    type="text"
                    placeholder="Address Line 1"
                    value={newCustomer.addressLine1}
                    onChange={(e) => setNewCustomer({ ...newCustomer, addressLine1: e.target.value })}
                    className="w-full px-3 py-2 text-sm border border-slate-200 rounded-lg"
                  />
                  <div className="grid grid-cols-3 gap-2">
                    <input
                      type="text"
                      placeholder="City"
                      value={newCustomer.city}
                      onChange={(e) => setNewCustomer({ ...newCustomer, city: e.target.value })}
                      className="px-3 py-2 text-sm border border-slate-200 rounded-lg"
                    />
                    <input
                      type="text"
                      placeholder="State"
                      value={newCustomer.state}
                      onChange={(e) => setNewCustomer({ ...newCustomer, state: e.target.value })}
                      className="px-3 py-2 text-sm border border-slate-200 rounded-lg"
                    />
                    <input
                      type="text"
                      placeholder="PIN Code"
                      value={newCustomer.postalCode}
                      onChange={(e) => setNewCustomer({ ...newCustomer, postalCode: e.target.value })}
                      className="px-3 py-2 text-sm border border-slate-200 rounded-lg"
                    />
                  </div>
                </div>
              </div>

              <div className="flex justify-end gap-3 pt-3 border-t border-slate-200">
                <button
                  type="button"
                  onClick={() => setIsAddModalOpen(false)}
                  className="px-4 py-2 text-sm text-slate-600 hover:bg-slate-100 rounded-lg"
                >
                  Cancel
                </button>
                <button
                  type="submit"
                  disabled={submitting}
                  className="px-4 py-2 text-sm font-semibold text-white bg-blue-600 hover:bg-blue-700 rounded-lg disabled:opacity-50"
                >
                  {submitting ? 'Saving...' : 'Create Customer'}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}

      {/* View Customer Details Modal */}
      {selectedCustomer && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50 p-4">
          <div className="bg-white rounded-xl shadow-xl w-full max-w-md overflow-hidden">
            <div className="px-6 py-4 border-b border-slate-200 flex justify-between items-center bg-slate-50">
              <div>
                <h3 className="font-bold text-slate-800">{selectedCustomer.name}</h3>
                <p className="text-xs text-slate-500 font-mono">{selectedCustomer.customerCode}</p>
              </div>
              <button
                onClick={() => setSelectedCustomer(null)}
                className="text-slate-400 hover:text-slate-600 text-lg font-bold"
              >
                &times;
              </button>
            </div>
            <div className="p-6 space-y-4 text-sm">
              <div className="grid grid-cols-2 gap-3 text-xs bg-slate-50 p-3 rounded-lg border border-slate-200">
                <div>
                  <span className="text-slate-500">Credit Limit:</span>
                  <div className="font-bold text-slate-800 text-sm">₹{selectedCustomer.creditLimit?.toLocaleString('en-IN')}</div>
                </div>
                <div>
                  <span className="text-slate-500">Outstanding:</span>
                  <div className="font-bold text-amber-600 text-sm">₹{selectedCustomer.outstandingBalance?.toLocaleString('en-IN')}</div>
                </div>
              </div>

              <div>
                <h4 className="font-semibold text-slate-700 mb-1">Contact Information</h4>
                <p className="text-slate-600">Email: {selectedCustomer.email}</p>
                <p className="text-slate-600">Phone: {selectedCustomer.phone}</p>
                {selectedCustomer.companyName && <p className="text-slate-600">Company: {selectedCustomer.companyName}</p>}
                {selectedCustomer.taxNumber && <p className="text-slate-600">Tax/GST: {selectedCustomer.taxNumber}</p>}
              </div>

              <div>
                <h4 className="font-semibold text-slate-700 mb-1">Addresses</h4>
                {selectedCustomer.addresses && selectedCustomer.addresses.length > 0 ? (
                  selectedCustomer.addresses.map((addr) => (
                    <div key={addr.id} className="text-xs text-slate-600 border-l-2 border-blue-500 pl-2 my-1">
                      {addr.line1}, {addr.city}, {addr.state} - {addr.postalCode}
                    </div>
                  ))
                ) : (
                  <p className="text-xs text-slate-400 italic">No saved addresses</p>
                )}
              </div>
            </div>
            <div className="px-6 py-3 bg-slate-50 border-t border-slate-200 text-right">
              <button
                onClick={() => setSelectedCustomer(null)}
                className="px-4 py-1.5 text-xs font-semibold bg-slate-200 hover:bg-slate-300 text-slate-700 rounded-lg"
              >
                Close
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};
