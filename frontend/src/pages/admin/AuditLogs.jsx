import { useCallback, useEffect, useState } from 'react';
import { FiFileText, FiSearch } from 'react-icons/fi';
import { adminApi } from '../../api';
import { getErrorMessage } from '../../api/client';
import { useToast } from '../../contexts/ToastContext';
import { PageHeader, Pagination, EmptyState, Skeleton } from '../../components/ui';

// Colour-code common audit actions.
const actionStyle = (action) => {
  const a = action.toLowerCase();
  if (a.includes('login') || a.includes('registered')) return 'bg-emerald-100 text-emerald-700 dark:bg-emerald-900/40 dark:text-emerald-300';
  if (a.includes('created')) return 'bg-brand-100 text-brand-700 dark:bg-brand-900/40 dark:text-brand-300';
  if (a.includes('updated') || a.includes('reset')) return 'bg-amber-100 text-amber-700 dark:bg-amber-900/40 dark:text-amber-300';
  if (a.includes('delete')) return 'bg-red-100 text-red-700 dark:bg-red-900/40 dark:text-red-300';
  return 'bg-slate-100 text-slate-600 dark:bg-slate-800 dark:text-slate-300';
};

export default function AuditLogs() {
  const { toast } = useToast();
  const [data, setData] = useState(null);
  const [loading, setLoading] = useState(true);
  const [search, setSearch] = useState('');
  const [page, setPage] = useState(1);

  const load = useCallback(() => {
    setLoading(true);
    const params = { page, pageSize: 20 };
    if (search) params.search = search;
    adminApi.auditLogs(params).then(setData).catch((e) => toast(getErrorMessage(e), 'error')).finally(() => setLoading(false));
  }, [page, search, toast]);

  useEffect(() => { load(); }, [load]);

  return (
    <div>
      <PageHeader icon={FiFileText} title="Audit Logs" subtitle="Security- and data-relevant events" />

      <div className="card mb-4 p-4">
        <div className="relative">
          <FiSearch className="pointer-events-none absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-slate-400" />
          <input className="input pl-9" placeholder="Search action or details…" value={search}
            onChange={(e) => { setSearch(e.target.value); setPage(1); }} />
        </div>
      </div>

      {loading ? (
        <div className="space-y-2">{Array.from({ length: 8 }).map((_, i) => <Skeleton key={i} className="h-12 w-full" />)}</div>
      ) : data?.items?.length ? (
        <>
          <div className="table-wrap">
            <table className="table-base">
              <thead>
                <tr>
                  <th>Time</th>
                  <th>Action</th>
                  <th>Entity</th>
                  <th>User</th>
                  <th>Details</th>
                </tr>
              </thead>
              <tbody>
                {data.items.map((log) => (
                  <tr key={log.id}>
                    <td className="whitespace-nowrap text-xs text-slate-500">{new Date(log.createdAt).toLocaleString()}</td>
                    <td><span className={`badge ${actionStyle(log.action)}`}>{log.action}</span></td>
                    <td className="text-slate-500">{log.entityName || '—'}</td>
                    <td className="text-slate-500">{log.userEmail || '—'}</td>
                    <td className="text-slate-600 dark:text-slate-300">{log.details || '—'}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
          <Pagination page={data.page} totalPages={data.totalPages} onChange={setPage} />
        </>
      ) : (
        <EmptyState title="No audit logs" icon={FiFileText} />
      )}
    </div>
  );
}
