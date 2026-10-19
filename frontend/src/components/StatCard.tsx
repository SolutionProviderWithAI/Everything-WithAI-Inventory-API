import React from 'react';

interface StatCardProps {
  title: string;
  value: string | number;
  subtitle?: string;
  icon: React.ReactNode;
  variant?: 'default' | 'success' | 'warning' | 'danger' | 'primary';
}

export const StatCard: React.FC<StatCardProps> = ({
  title,
  value,
  subtitle,
  icon,
  variant = 'default'
}) => {
  const variantStyles = {
    default: 'bg-white border-slate-200 text-slate-700',
    primary: 'bg-blue-50/50 border-blue-200 text-blue-700',
    success: 'bg-emerald-50/50 border-emerald-200 text-emerald-700',
    warning: 'bg-amber-50/50 border-amber-200 text-amber-700',
    danger: 'bg-rose-50/50 border-rose-200 text-rose-700'
  };

  const iconBg = {
    default: 'bg-slate-100 text-slate-600',
    primary: 'bg-blue-100 text-blue-600',
    success: 'bg-emerald-100 text-emerald-600',
    warning: 'bg-amber-100 text-amber-600',
    danger: 'bg-rose-100 text-rose-600'
  };

  return (
    <div className={`p-4 rounded-xl border bg-white shadow-xs flex items-center justify-between transition hover:shadow-sm`}>
      <div>
        <p className="text-xs font-medium text-slate-500 mb-1">{title}</p>
        <p className="text-2xl font-bold text-slate-900 tracking-tight">{value}</p>
        {subtitle && <p className="text-xs text-slate-400 mt-1">{subtitle}</p>}
      </div>
      <div className={`p-3 rounded-lg ${iconBg[variant]}`}>
        {icon}
      </div>
    </div>
  );
};

interface ModalProps {
  isOpen: boolean;
  onClose: () => void;
  title: string;
  children: React.ReactNode;
  footer?: React.ReactNode;
}

export const Modal: React.FC<ModalProps> = ({
  isOpen,
  onClose,
  title,
  children,
  footer
}) => {
  if (!isOpen) return null;

  return (
    <div className="modal-overlay">
      <div className="modal-content animate-in fade-in duration-200">
        <div className="modal-header">
          <h3 className="text-base font-semibold text-slate-900">{title}</h3>
          <button
            onClick={onClose}
            className="text-slate-400 hover:text-slate-600 p-1 rounded-md hover:bg-slate-100 transition"
          >
            ✕
          </button>
        </div>
        <div className="modal-body">{children}</div>
        {footer && <div className="modal-footer">{footer}</div>}
      </div>
    </div>
  );
};
