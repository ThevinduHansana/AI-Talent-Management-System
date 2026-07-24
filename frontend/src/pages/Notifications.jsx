import { useCallback, useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { FiBell, FiCheck } from 'react-icons/fi';
import { notificationsApi } from '../api';
import { getErrorMessage } from '../api/client';
import { useToast } from '../contexts/ToastContext';
import { PageHeader, Pagination, EmptyState, Skeleton } from '../components/ui';

export default function Notifications() {
  const navigate = useNavigate();
  const { toast } = useToast();
  const [data, setData] = useState(null);
  const [loading, setLoading] = useState(true);
  const [unreadOnly, setUnreadOnly] = useState(false);
  const [page, setPage] = useState(1);

  const load = useCallback(() => {
    setLoading(true);
    notificationsApi.list({ page, pageSize: 15, ...(unreadOnly ? { unreadOnly: true } : {}) })
      .then(setData)
      .catch((e) => toast(getErrorMessage(e), 'error'))
      .finally(() => setLoading(false));
  }, [page, unreadOnly, toast]);

  useEffect(() => { load(); }, [load]);

  const open = async (n) => {
    if (!n.isRead) { try { await notificationsApi.markRead(n.id); } catch { /* ignore */ } }
    if (n.link) navigate(n.link);
    else load();
  };

  const markAll = async () => {
    try { await notificationsApi.markAllRead(); toast('All marked read.', 'success'); load(); }
    catch (e) { toast(getErrorMessage(e), 'error'); }
  };

  return (
    <div>
      <PageHeader icon={FiBell} title="Notifications" subtitle="Stay up to date on your activity"
        actions={<button className="btn-secondary" onClick={markAll}><FiCheck /> Mark all read</button>} />

      <div className="mb-4 flex gap-2">
        {[['All', false], ['Unread', true]].map(([label, val]) => (
          <button key={label} onClick={() => { setUnreadOnly(val); setPage(1); }}
            className={`badge cursor-pointer ${unreadOnly === val ? 'bg-brand-600 text-white' : 'bg-slate-100 text-slate-600 dark:bg-slate-800 dark:text-slate-300'}`}>
            {label}
          </button>
        ))}
      </div>

      {loading ? (
        <div className="space-y-2">{Array.from({ length: 6 }).map((_, i) => <Skeleton key={i} className="h-16 w-full" />)}</div>
      ) : data?.items?.length ? (
        <>
          <div className="card divide-y divide-slate-100 dark:divide-slate-800">
            {data.items.map((n) => (
              <button key={n.id} onClick={() => open(n)}
                className={`flex w-full items-start gap-3 p-4 text-left hover:bg-slate-50 dark:hover:bg-slate-800/50 ${!n.isRead ? 'bg-brand-50/40 dark:bg-brand-900/10' : ''}`}>
                <span className={`mt-1 flex h-9 w-9 shrink-0 items-center justify-center rounded-xl ${n.isRead ? 'bg-slate-100 text-slate-400 dark:bg-slate-800' : 'bg-gradient-to-br from-brand-500 to-accent-500 text-white'}`}>
                  <FiBell className="h-4 w-4" />
                </span>
                <div className="flex-1">
                  <div className="flex items-center justify-between">
                    <span className="text-sm font-medium text-slate-800 dark:text-slate-100">{n.title}</span>
                    {!n.isRead && <span className="h-2 w-2 rounded-full bg-brand-500" />}
                  </div>
                  <p className="text-sm text-slate-500">{n.message}</p>
                  <span className="text-xs text-slate-400">{new Date(n.createdAt).toLocaleString()}</span>
                </div>
              </button>
            ))}
          </div>
          <Pagination page={data.page} totalPages={data.totalPages} onChange={setPage} />
        </>
      ) : (
        <EmptyState title="No notifications" icon={FiBell} />
      )}
    </div>
  );
}
