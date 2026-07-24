import { useCallback, useEffect, useState } from 'react';
import { motion } from 'framer-motion';
import { FiCalendar, FiVideo, FiMapPin, FiPhone, FiX } from 'react-icons/fi';
import { recruiterApi } from '../../api';
import { getErrorMessage } from '../../api/client';
import { useToast } from '../../contexts/ToastContext';
import { PageHeader, EmptyState, Skeleton } from '../../components/ui';

const MODE_ICON = { Video: FiVideo, Onsite: FiMapPin, Phone: FiPhone };

export default function Interviews() {
  const { toast } = useToast();
  const [items, setItems] = useState(null);

  const load = useCallback(() => {
    recruiterApi.upcomingInterviews()
      .then(setItems)
      .catch((e) => { toast(getErrorMessage(e), 'error'); setItems([]); });
  }, [toast]);

  useEffect(() => { load(); }, [load]);

  const cancel = async (interviewId) => {
    try {
      await recruiterApi.cancelInterview(interviewId);
      setItems((prev) => prev.filter((i) => i.id !== interviewId));
      toast('Interview cancelled.', 'success');
    } catch (e) {
      toast(getErrorMessage(e), 'error');
    }
  };

  return (
    <div>
      <PageHeader icon={FiCalendar} title="Upcoming Interviews" subtitle="Scheduled interviews across your jobs" />

      {items === null ? (
        <div className="space-y-3">{Array.from({ length: 3 }).map((_, i) => <Skeleton key={i} className="h-20 w-full" />)}</div>
      ) : items.length ? (
        <div className="space-y-3">
          {items.map((iv, idx) => {
            const Icon = MODE_ICON[iv.mode] || FiCalendar;
            const when = new Date(iv.scheduledAt);
            return (
              <motion.div
                key={iv.id}
                initial={{ opacity: 0, y: 12 }}
                animate={{ opacity: 1, y: 0 }}
                transition={{ delay: idx * 0.05 }}
                className="card card-hover flex items-center justify-between p-4"
              >
                <div className="flex items-center gap-4">
                  <div className="flex h-12 w-12 flex-col items-center justify-center rounded-xl bg-gradient-to-br from-brand-500 to-accent-500 text-white">
                    <span className="text-xs font-medium uppercase">{when.toLocaleString(undefined, { month: 'short' })}</span>
                    <span className="text-lg font-bold leading-none">{when.getDate()}</span>
                  </div>
                  <div>
                    <div className="font-medium text-slate-800 dark:text-slate-100">{iv.title}</div>
                    <div className="text-sm text-slate-500">{iv.candidateName} · {iv.jobTitle}</div>
                    <div className="mt-1 flex items-center gap-2 text-xs text-slate-400">
                      <Icon className="h-3.5 w-3.5" /> {iv.mode} · {when.toLocaleString(undefined, { hour: '2-digit', minute: '2-digit' })} · {iv.durationMinutes} min
                    </div>
                  </div>
                </div>
                <button onClick={() => cancel(iv.id)} className="btn-secondary text-red-600">
                  <FiX /> Cancel
                </button>
              </motion.div>
            );
          })}
        </div>
      ) : (
        <EmptyState title="No upcoming interviews" message="Schedule interviews from a job's pipeline." icon={FiCalendar} />
      )}
    </div>
  );
}
