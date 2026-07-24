import { motion } from 'framer-motion';
import { FiInbox, FiCheckCircle, FiAlertCircle, FiInfo, FiAlertTriangle } from 'react-icons/fi';
import { APPLICATION_STATUS_STYLES, humanize } from '../constants';

export function Spinner({ className = 'h-5 w-5' }) {
  return (
    <svg className={`animate-spin text-brand-600 ${className}`} viewBox="0 0 24 24" fill="none" aria-hidden="true">
      <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4" />
      <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8v4a4 4 0 00-4 4H4z" />
    </svg>
  );
}

export function LoadingScreen({ label = 'Loading…' }) {
  return (
    <div className="flex min-h-[40vh] flex-col items-center justify-center gap-3 text-slate-500">
      <Spinner className="h-8 w-8" />
      <span className="text-sm">{label}</span>
    </div>
  );
}

/* Shimmering skeleton block for loading states. */
export function Skeleton({ className = '' }) {
  return (
    <div className={`relative overflow-hidden rounded-lg bg-slate-200/80 dark:bg-slate-700/60 ${className}`}>
      <div className="absolute inset-0 shimmer dark:opacity-10" />
    </div>
  );
}

/* A card-shaped skeleton, handy for dashboard/table loading placeholders. */
export function SkeletonCard({ lines = 3 }) {
  return (
    <div className="card p-5">
      <Skeleton className="h-4 w-1/3" />
      <div className="mt-4 space-y-2.5">
        {Array.from({ length: lines }).map((_, i) => (
          <Skeleton key={i} className={`h-3 ${i === lines - 1 ? 'w-2/3' : 'w-full'}`} />
        ))}
      </div>
    </div>
  );
}

export function PageHeader({ title, subtitle, actions, icon: Icon }) {
  return (
    <motion.div
      initial={{ opacity: 0, y: -8 }}
      animate={{ opacity: 1, y: 0 }}
      transition={{ duration: 0.35 }}
      className="mb-6 flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between"
    >
      <div className="flex items-center gap-3">
        {Icon ? (
          <span className="flex h-11 w-11 shrink-0 items-center justify-center rounded-xl bg-gradient-to-br from-brand-500 to-accent-500 text-white shadow-[0_6px_16px_-4px_rgba(79,70,229,0.5)]">
            <Icon className="h-5 w-5" />
          </span>
        ) : (
          <span className="h-9 w-1.5 shrink-0 rounded-full bg-gradient-to-b from-brand-500 to-accent-500" />
        )}
        <div>
          <h1 className="text-2xl font-bold tracking-tight text-slate-900 dark:text-white">{title}</h1>
          {subtitle && <p className="mt-1 text-sm text-slate-500 dark:text-slate-400">{subtitle}</p>}
        </div>
      </div>
      {actions && <div className="flex flex-wrap items-center gap-2">{actions}</div>}
    </motion.div>
  );
}

/* Small section heading used inside cards / dashboard blocks. */
export function SectionHeading({ title, action }) {
  return (
    <div className="mb-4 flex items-center justify-between">
      <h3 className="font-semibold text-slate-800 dark:text-slate-100">{title}</h3>
      {action}
    </div>
  );
}

export function EmptyState({ title = 'Nothing here yet', message, icon: Icon = FiInbox, action }) {
  return (
    <div className="card flex flex-col items-center justify-center gap-3 p-10 text-center">
      <span className="flex h-14 w-14 items-center justify-center rounded-2xl bg-slate-100 text-slate-400 dark:bg-slate-800 dark:text-slate-500">
        <Icon className="h-7 w-7" />
      </span>
      <h3 className="font-semibold text-slate-700 dark:text-slate-200">{title}</h3>
      {message && <p className="max-w-md text-sm text-slate-500 dark:text-slate-400">{message}</p>}
      {action}
    </div>
  );
}

export function StatusBadge({ status }) {
  const style = APPLICATION_STATUS_STYLES[status] || 'bg-slate-200 text-slate-600';
  return <span className={`badge ${style}`}>{humanize(status)}</span>;
}

/* Reusable dashboard stat card with an icon chip and optional trend/animation. */
const STAT_TONES = {
  brand: 'bg-brand-50 text-brand-600 dark:bg-brand-900/30 dark:text-brand-300',
  accent: 'bg-accent-50 text-accent-600 dark:bg-accent-900/30 dark:text-accent-300',
  emerald: 'bg-emerald-50 text-emerald-600 dark:bg-emerald-900/30 dark:text-emerald-300',
  amber: 'bg-amber-50 text-amber-600 dark:bg-amber-900/30 dark:text-amber-300',
  violet: 'bg-violet-50 text-violet-600 dark:bg-violet-900/30 dark:text-violet-300',
  rose: 'bg-rose-50 text-rose-600 dark:bg-rose-900/30 dark:text-rose-300',
};

export function StatCard({ label, value, icon: Icon, tone = 'brand', hint, index = 0 }) {
  return (
    <motion.div
      initial={{ opacity: 0, y: 16 }}
      animate={{ opacity: 1, y: 0 }}
      transition={{ delay: index * 0.06, duration: 0.35 }}
      className="card card-hover flex items-center gap-4 p-5"
    >
      {Icon && (
        <span className={`flex h-12 w-12 shrink-0 items-center justify-center rounded-xl ${STAT_TONES[tone] || STAT_TONES.brand}`}>
          <Icon className="h-6 w-6" />
        </span>
      )}
      <div className="min-w-0">
        <div className="truncate text-2xl font-bold text-slate-900 dark:text-white">{value}</div>
        <div className="truncate text-xs font-medium text-slate-500 dark:text-slate-400">{label}</div>
        {hint && <div className="mt-0.5 text-[11px] text-slate-400">{hint}</div>}
      </div>
    </motion.div>
  );
}

/* Contextual inline alert. */
const ALERT_STYLES = {
  success: { icon: FiCheckCircle, cls: 'border-emerald-200 bg-emerald-50 text-emerald-800 dark:border-emerald-900/50 dark:bg-emerald-900/20 dark:text-emerald-200', ic: 'text-emerald-500' },
  error: { icon: FiAlertCircle, cls: 'border-red-200 bg-red-50 text-red-800 dark:border-red-900/50 dark:bg-red-900/20 dark:text-red-200', ic: 'text-red-500' },
  warning: { icon: FiAlertTriangle, cls: 'border-amber-200 bg-amber-50 text-amber-800 dark:border-amber-900/50 dark:bg-amber-900/20 dark:text-amber-200', ic: 'text-amber-500' },
  info: { icon: FiInfo, cls: 'border-brand-200 bg-brand-50 text-brand-800 dark:border-brand-900/50 dark:bg-brand-900/20 dark:text-brand-200', ic: 'text-brand-500' },
};

export function Alert({ type = 'info', title, children }) {
  const s = ALERT_STYLES[type] || ALERT_STYLES.info;
  const Icon = s.icon;
  return (
    <div className={`flex items-start gap-3 rounded-xl border p-4 text-sm ${s.cls}`} role="alert">
      <Icon className={`mt-0.5 h-5 w-5 shrink-0 ${s.ic}`} />
      <div>
        {title && <p className="font-semibold">{title}</p>}
        {children && <div className={title ? 'mt-0.5 opacity-90' : ''}>{children}</div>}
      </div>
    </div>
  );
}

export function Pagination({ page, totalPages, onChange }) {
  if (totalPages <= 1) return null;
  return (
    <div className="mt-4 flex items-center justify-between">
      <button className="btn-secondary btn-sm" disabled={page <= 1} onClick={() => onChange(page - 1)}>
        Previous
      </button>
      <span className="text-sm text-slate-500 dark:text-slate-400">
        Page {page} of {totalPages}
      </span>
      <button className="btn-secondary btn-sm" disabled={page >= totalPages} onClick={() => onChange(page + 1)}>
        Next
      </button>
    </div>
  );
}
