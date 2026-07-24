import { useCallback, useEffect, useRef, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { AnimatePresence, motion } from 'framer-motion';
import { FiBell, FiCheck } from 'react-icons/fi';
import { notificationsApi } from '../api';

const POLL_MS = 30000;

export default function NotificationBell() {
  const navigate = useNavigate();
  const [open, setOpen] = useState(false);
  const [unread, setUnread] = useState(0);
  const [items, setItems] = useState([]);
  const ref = useRef(null);

  const loadCount = useCallback(() => {
    notificationsApi.unreadCount().then(setUnread).catch(() => {});
  }, []);

  const loadList = useCallback(() => {
    notificationsApi.list({ pageSize: 8 }).then((d) => setItems(d.items)).catch(() => {});
  }, []);

  useEffect(() => {
    loadCount();
    const t = setInterval(loadCount, POLL_MS);
    return () => clearInterval(t);
  }, [loadCount]);

  useEffect(() => {
    const onClick = (e) => { if (ref.current && !ref.current.contains(e.target)) setOpen(false); };
    document.addEventListener('mousedown', onClick);
    return () => document.removeEventListener('mousedown', onClick);
  }, []);

  const toggle = () => {
    const next = !open;
    setOpen(next);
    if (next) loadList();
  };

  const openItem = async (n) => {
    setOpen(false);
    if (!n.isRead) {
      try { await notificationsApi.markRead(n.id); } catch { /* ignore */ }
      loadCount();
    }
    if (n.link) navigate(n.link);
  };

  const markAll = async () => {
    try { await notificationsApi.markAllRead(); } catch { /* ignore */ }
    setUnread(0);
    loadList();
  };

  return (
    <div className="relative" ref={ref}>
      <button onClick={toggle} className="relative rounded-lg p-2 text-slate-500 hover:bg-slate-100 hover:text-slate-700 dark:text-slate-400 dark:hover:bg-slate-800" aria-label="Notifications">
        <FiBell className="h-5 w-5" />
        {unread > 0 && (
          <span className="absolute -right-0.5 -top-0.5 flex h-4 min-w-4 items-center justify-center rounded-full bg-red-500 px-1 text-[10px] font-bold text-white">
            {unread > 9 ? '9+' : unread}
          </span>
        )}
      </button>

      <AnimatePresence>
        {open && (
          <motion.div
            initial={{ opacity: 0, y: -8, scale: 0.98 }}
            animate={{ opacity: 1, y: 0, scale: 1 }}
            exit={{ opacity: 0, y: -8, scale: 0.98 }}
            className="card absolute right-0 mt-2 w-80 overflow-hidden p-0 shadow-lg"
          >
            <div className="flex items-center justify-between border-b border-slate-100 px-4 py-3 dark:border-slate-800">
              <span className="font-semibold text-slate-800 dark:text-slate-100">Notifications</span>
              {unread > 0 && (
                <button onClick={markAll} className="flex items-center gap-1 text-xs text-brand-600 hover:underline">
                  <FiCheck className="h-3.5 w-3.5" /> Mark all read
                </button>
              )}
            </div>
            <div className="max-h-96 overflow-y-auto">
              {items.length ? items.map((n) => (
                <button
                  key={n.id}
                  onClick={() => openItem(n)}
                  className={`flex w-full flex-col items-start gap-0.5 border-b border-slate-50 px-4 py-3 text-left hover:bg-slate-50 dark:border-slate-800 dark:hover:bg-slate-800 ${!n.isRead ? 'bg-brand-50/40 dark:bg-brand-900/10' : ''}`}
                >
                  <div className="flex w-full items-center justify-between">
                    <span className="text-sm font-medium text-slate-800 dark:text-slate-100">{n.title}</span>
                    {!n.isRead && <span className="h-2 w-2 shrink-0 rounded-full bg-brand-500" />}
                  </div>
                  <span className="text-xs text-slate-500">{n.message}</span>
                  <span className="text-[11px] text-slate-400">{new Date(n.createdAt).toLocaleString()}</span>
                </button>
              )) : (
                <p className="px-4 py-8 text-center text-sm text-slate-400">No notifications</p>
              )}
            </div>
          </motion.div>
        )}
      </AnimatePresence>
    </div>
  );
}
