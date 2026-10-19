import React, { useEffect, useState } from 'react';
import { Plus, Search, Filter, Edit2, Trash2, CheckCircle2, AlertCircle } from 'lucide-react';
import { api } from '../services/api';
import { Modal } from '../components/StatCard';
import { Product, Category } from '../types';

export const ProductManagement: React.FC = () => {
  const [products, setProducts] = useState<Product[]>([]);
  const [categories, setCategories] = useState<Category[]>([]);
  const [searchQuery, setSearchQuery] = useState('');
  const [selectedCategory, setSelectedCategory] = useState<string>('');
  const [loading, setLoading] = useState(true);

  // Modals state
  const [isAddModalOpen, setIsAddModalOpen] = useState(false);
  const [isEditModalOpen, setIsEditModalOpen] = useState(false);
  const [editingProduct, setEditingProduct] = useState<Product | null>(null);

  // Add Product Form state
  const [newSku, setNewSku] = useState('');
  const [newName, setNewName] = useState('');
  const [newDesc, setNewDesc] = useState('');
  const [newCatId, setNewCatId] = useState('');
  const [newCost, setNewCost] = useState('60');
  const [newPrice, setNewPrice] = useState('100');
  const [newReorderLevel, setNewReorderLevel] = useState('10');
  const [newReorderQty, setNewReorderQty] = useState('50');
  const [newInitialStock, setNewInitialStock] = useState('50');
  const [formError, setFormError] = useState<string | null>(null);

  const loadProducts = async () => {
    setLoading(true);
    try {
      const [prodRes, cats] = await Promise.all([
        api.getProducts(searchQuery, selectedCategory || undefined),
        api.getCategories()
      ]);
      setProducts(prodRes.items || []);
      setCategories(cats || []);
      if (cats.length > 0 && !newCatId) {
        setNewCatId(cats[0].id);
      }
    } catch (e) {
      console.error(e);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    loadProducts();
  }, [searchQuery, selectedCategory]);

  const handleCreateProduct = async (e: React.FormEvent) => {
    e.preventDefault();
    setFormError(null);

    try {
      await api.createProduct({
        sku: newSku.trim().toUpperCase(),
        name: newName.trim(),
        description: newDesc,
        categoryId: newCatId,
        costPrice: parseFloat(newCost) || 0,
        sellingPrice: parseFloat(newPrice) || 0,
        reorderLevel: parseInt(newReorderLevel) || 10,
        reorderQuantity: parseInt(newReorderQty) || 50,
        initialStock: parseInt(newInitialStock) || 0
      });

      setIsAddModalOpen(false);
      // Reset form
      setNewSku('');
      setNewName('');
      setNewDesc('');
      await loadProducts();
    } catch (err: any) {
      setFormError(err.message || 'Failed to create product');
    }
  };

  const handleUpdateProduct = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!editingProduct) return;
    setFormError(null);

    try {
      await api.updateProduct(editingProduct.id, {
        name: editingProduct.name,
        description: editingProduct.description,
        categoryId: editingProduct.categoryId,
        unitOfMeasurement: editingProduct.unitOfMeasurement,
        costPrice: editingProduct.costPrice,
        sellingPrice: editingProduct.sellingPrice,
        reorderLevel: editingProduct.reorderLevel,
        reorderQuantity: editingProduct.reorderQuantity,
        isActive: editingProduct.isActive
      });

      setIsEditModalOpen(false);
      setEditingProduct(null);
      await loadProducts();
    } catch (err: any) {
      setFormError(err.message || 'Failed to update product');
    }
  };

  const handleDeactivate = async (id: string, name: string) => {
    if (confirm(`Are you sure you want to deactivate ${name}?`)) {
      try {
        await api.deactivateProduct(id);
        await loadProducts();
      } catch (err: any) {
        alert(err.message);
      }
    }
  };

  return (
    <div className="space-y-6">
      {/* Header and Controls */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4 bg-white p-5 rounded-2xl border border-slate-200 shadow-xs">
        <div>
          <h2 className="text-lg font-bold text-slate-900">Product Catalog Management</h2>
          <p className="text-xs text-slate-500">Manage SKUs, categories, pricing, and reorder levels</p>
        </div>

        <button
          onClick={() => {
            setFormError(null);
            setIsAddModalOpen(true);
          }}
          className="btn btn-primary text-xs"
        >
          <Plus size={16} />
          Add New Product
        </button>
      </div>

      {/* Filter and Search Bar */}
      <div className="flex flex-col sm:flex-row gap-3 bg-white p-4 rounded-xl border border-slate-200 shadow-xs">
        <div className="relative flex-1">
          <Search size={16} className="absolute left-3 top-1/2 -translate-y-1/2 text-slate-400" />
          <input
            type="text"
            value={searchQuery}
            onChange={(e) => setSearchQuery(e.target.value)}
            placeholder="Search products by SKU, name, or description..."
            className="input pl-9 text-xs"
          />
        </div>

        <div className="w-full sm:w-64">
          <select
            value={selectedCategory}
            onChange={(e) => setSelectedCategory(e.target.value)}
            className="select text-xs"
          >
            <option value="">All Categories</option>
            {categories.map((c) => (
              <option key={c.id} value={c.id}>{c.name}</option>
            ))}
          </select>
        </div>
      </div>

      {/* Products Table */}
      <div className="bg-white rounded-2xl border border-slate-200 shadow-xs overflow-hidden">
        <div className="table-container border-0">
          <table>
            <thead>
              <tr>
                <th>SKU</th>
                <th>Product Name</th>
                <th>Category</th>
                <th>Selling Price</th>
                <th>Cost Price</th>
                <th>Available / On-Hand</th>
                <th>Reorder Level</th>
                <th>Status</th>
                <th className="text-right">Actions</th>
              </tr>
            </thead>
            <tbody>
              {products.length === 0 ? (
                <tr>
                  <td colSpan={9} className="text-center py-8 text-slate-400 text-xs">
                    No products found matching criteria.
                  </td>
                </tr>
              ) : (
                products.map((p) => {
                  const isLow = p.availableToSell <= p.reorderLevel && p.availableToSell > 0;
                  const isOut = p.availableToSell <= 0;

                  return (
                    <tr key={p.id}>
                      <td className="font-mono text-xs font-bold text-slate-900">{p.sku}</td>
                      <td>
                        <div className="font-semibold text-slate-800">{p.name}</div>
                        {p.description && <div className="text-[11px] text-slate-400 truncate max-w-xs">{p.description}</div>}
                      </td>
                      <td className="text-xs text-slate-600">{p.categoryName}</td>
                      <td className="font-bold text-slate-900">₹{p.sellingPrice.toFixed(2)}</td>
                      <td className="text-xs text-slate-500">₹{p.costPrice.toFixed(2)}</td>
                      <td>
                        <span className="font-bold text-slate-800">{p.availableToSell}</span>
                        <span className="text-xs text-slate-400"> / {p.totalQuantityOnHand}</span>
                      </td>
                      <td className="text-xs text-slate-600">{p.reorderLevel} units</td>
                      <td>
                        {isOut ? (
                          <span className="badge badge-danger">Out of Stock</span>
                        ) : isLow ? (
                          <span className="badge badge-warning">Low Stock</span>
                        ) : (
                          <span className="badge badge-success">In Stock</span>
                        )}
                      </td>
                      <td className="text-right">
                        <div className="flex items-center justify-end gap-1.5">
                          <button
                            onClick={() => {
                              setEditingProduct({ ...p });
                              setFormError(null);
                              setIsEditModalOpen(true);
                            }}
                            className="p-1.5 text-slate-500 hover:text-blue-600 hover:bg-blue-50 rounded-md transition"
                            title="Edit"
                          >
                            <Edit2 size={15} />
                          </button>
                          <button
                            onClick={() => handleDeactivate(p.id, p.name)}
                            className="p-1.5 text-slate-500 hover:text-rose-600 hover:bg-rose-50 rounded-md transition"
                            title="Deactivate"
                          >
                            <Trash2 size={15} />
                          </button>
                        </div>
                      </td>
                    </tr>
                  );
                })
              )}
            </tbody>
          </table>
        </div>
      </div>

      {/* Add Product Modal */}
      <Modal
        isOpen={isAddModalOpen}
        onClose={() => setIsAddModalOpen(false)}
        title="Create New Product"
      >
        <form onSubmit={handleCreateProduct} className="space-y-4 text-xs">
          {formError && (
            <div className="p-3 bg-rose-50 border border-rose-200 text-rose-700 rounded-lg flex items-center gap-2">
              <AlertCircle size={16} />
              <span>{formError}</span>
            </div>
          )}

          <div className="grid grid-cols-2 gap-3">
            <div>
              <label className="block text-slate-700 font-semibold mb-1">Product SKU *</label>
              <input
                type="text"
                required
                value={newSku}
                onChange={(e) => setNewSku(e.target.value)}
                placeholder="e.g. PROD-C-003"
                className="input"
              />
            </div>
            <div>
              <label className="block text-slate-700 font-semibold mb-1">Product Name *</label>
              <input
                type="text"
                required
                value={newName}
                onChange={(e) => setNewName(e.target.value)}
                placeholder="e.g. Industrial Barcode Scanner"
                className="input"
              />
            </div>
          </div>

          <div>
            <label className="block text-slate-700 font-semibold mb-1">Category *</label>
            <select
              value={newCatId}
              onChange={(e) => setNewCatId(e.target.value)}
              className="select"
            >
              {categories.map((c) => (
                <option key={c.id} value={c.id}>{c.name}</option>
              ))}
            </select>
          </div>

          <div>
            <label className="block text-slate-700 font-semibold mb-1">Description</label>
            <textarea
              rows={2}
              value={newDesc}
              onChange={(e) => setNewDesc(e.target.value)}
              placeholder="Detailed product specifications..."
              className="textarea"
            />
          </div>

          <div className="grid grid-cols-2 gap-3">
            <div>
              <label className="block text-slate-700 font-semibold mb-1">Selling Price (₹) *</label>
              <input
                type="number"
                step="0.01"
                required
                value={newPrice}
                onChange={(e) => setNewPrice(e.target.value)}
                className="input"
              />
            </div>
            <div>
              <label className="block text-slate-700 font-semibold mb-1">Cost Price (₹) *</label>
              <input
                type="number"
                step="0.01"
                required
                value={newCost}
                onChange={(e) => setNewCost(e.target.value)}
                className="input"
              />
            </div>
          </div>

          <div className="grid grid-cols-3 gap-3">
            <div>
              <label className="block text-slate-700 font-semibold mb-1">Opening Stock</label>
              <input
                type="number"
                value={newInitialStock}
                onChange={(e) => setNewInitialStock(e.target.value)}
                className="input"
              />
            </div>
            <div>
              <label className="block text-slate-700 font-semibold mb-1">Reorder Level</label>
              <input
                type="number"
                value={newReorderLevel}
                onChange={(e) => setNewReorderLevel(e.target.value)}
                className="input"
              />
            </div>
            <div>
              <label className="block text-slate-700 font-semibold mb-1">Reorder Qty</label>
              <input
                type="number"
                value={newReorderQty}
                onChange={(e) => setNewReorderQty(e.target.value)}
                className="input"
              />
            </div>
          </div>

          <div className="flex justify-end gap-2 pt-3 border-t border-slate-100">
            <button
              type="button"
              onClick={() => setIsAddModalOpen(false)}
              className="btn btn-secondary text-xs"
            >
              Cancel
            </button>
            <button
              type="submit"
              className="btn btn-primary text-xs"
            >
              Create Product
            </button>
          </div>
        </form>
      </Modal>

      {/* Edit Product Modal */}
      {editingProduct && (
        <Modal
          isOpen={isEditModalOpen}
          onClose={() => setIsEditModalOpen(false)}
          title={`Edit Product: ${editingProduct.sku}`}
        >
          <form onSubmit={handleUpdateProduct} className="space-y-4 text-xs">
            {formError && (
              <div className="p-3 bg-rose-50 border border-rose-200 text-rose-700 rounded-lg flex items-center gap-2">
                <AlertCircle size={16} />
                <span>{formError}</span>
              </div>
            )}

            <div>
              <label className="block text-slate-700 font-semibold mb-1">Product Name</label>
              <input
                type="text"
                required
                value={editingProduct.name}
                onChange={(e) => setEditingProduct({ ...editingProduct, name: e.target.value })}
                className="input"
              />
            </div>

            <div>
              <label className="block text-slate-700 font-semibold mb-1">Description</label>
              <textarea
                rows={2}
                value={editingProduct.description || ''}
                onChange={(e) => setEditingProduct({ ...editingProduct, description: e.target.value })}
                className="textarea"
              />
            </div>

            <div className="grid grid-cols-2 gap-3">
              <div>
                <label className="block text-slate-700 font-semibold mb-1">Selling Price (₹)</label>
                <input
                  type="number"
                  step="0.01"
                  required
                  value={editingProduct.sellingPrice}
                  onChange={(e) => setEditingProduct({ ...editingProduct, sellingPrice: parseFloat(e.target.value) || 0 })}
                  className="input"
                />
              </div>
              <div>
                <label className="block text-slate-700 font-semibold mb-1">Cost Price (₹)</label>
                <input
                  type="number"
                  step="0.01"
                  required
                  value={editingProduct.costPrice}
                  onChange={(e) => setEditingProduct({ ...editingProduct, costPrice: parseFloat(e.target.value) || 0 })}
                  className="input"
                />
              </div>
            </div>

            <div className="grid grid-cols-2 gap-3">
              <div>
                <label className="block text-slate-700 font-semibold mb-1">Reorder Level</label>
                <input
                  type="number"
                  value={editingProduct.reorderLevel}
                  onChange={(e) => setEditingProduct({ ...editingProduct, reorderLevel: parseInt(e.target.value) || 0 })}
                  className="input"
                />
              </div>
              <div>
                <label className="block text-slate-700 font-semibold mb-1">Reorder Quantity</label>
                <input
                  type="number"
                  value={editingProduct.reorderQuantity}
                  onChange={(e) => setEditingProduct({ ...editingProduct, reorderQuantity: parseInt(e.target.value) || 0 })}
                  className="input"
                />
              </div>
            </div>

            <div className="flex justify-end gap-2 pt-3 border-t border-slate-100">
              <button
                type="button"
                onClick={() => setIsEditModalOpen(false)}
                className="btn btn-secondary text-xs"
              >
                Cancel
              </button>
              <button
                type="submit"
                className="btn btn-primary text-xs"
              >
                Save Changes
              </button>
            </div>
          </form>
        </Modal>
      )}
    </div>
  );
};
