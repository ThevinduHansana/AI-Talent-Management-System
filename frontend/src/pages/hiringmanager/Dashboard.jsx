import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { motion } from 'framer-motion';
import { FiInbox, FiClipboard, FiUserCheck, FiUserX, FiClock } from 'react-icons/fi';
import { hiringManagerApi } from '../../api';
import { getErrorMessage } from '../../api/client';
import { useAuth } from '../../contexts/AuthContext';
import { useToast } from '../../contexts/ToastContext';
import { PageHeader, LoadingScreen, StatCard } from '../../components/ui';

export default function HiringManagerDashboard() {
  const { user } = useAuth();
  const { toast } = useToast();
  const [data, setData] = useState(null);

  useEffect(() => {
    hiringManagerApi.dashboard()
      .then(setData)
      .catch((e) => { toast(getErrorMessage(e), 'error'); setData({}); });
  }, [toast]);

  if (!data) return <LoadingScreen />;

  const stats = [
    { label: 'To Review', value: data.toReview ?? 0, icon: FiInbox, tone: 'brand' },
    { label: 'Evaluated', value: data.evaluated ?? 0, icon: FiClipboard, tone: 'accent' },
    { label: 'Pending Decision', value: data.pendingDecision ?? 0, icon: FiClock, tone: 'amber' },
    { label: 'Hired', value: data.hired ?? 0, icon: FiUserCheck, tone: 'emerald' },
    { label: 'Rejected', value: data.rejected ?? 0, icon: FiUserX, tone: 'rose' },
  ];

  return (
    <div>
      <PageHeader title={`Welcome, ${user?.firstName}`} subtitle="Candidates awaiting your review and decisions"
        actions={<Link to="/hiring-manager/candidates" className="btn-primary"><FiUserCheck /> Review candidates</Link>} />

      <div className="grid grid-cols-2 gap-4 md:grid-cols-3 lg:grid-cols-5">
        {stats.map((s, i) => (
          <StatCard key={s.label} index={i} label={s.label} value={s.value} icon={s.icon} tone={s.tone} />
        ))}
      </div>

      <motion.div
        initial={{ opacity: 0, y: 16 }}
        animate={{ opacity: 1, y: 0 }}
        transition={{ delay: 0.3 }}
        className="card mt-6 overflow-hidden p-8 text-center"
      >
        <span className="mx-auto mb-4 flex h-16 w-16 items-center justify-center rounded-2xl bg-gradient-to-br from-brand-500 to-accent-500 text-white shadow-lg">
          <FiUserCheck className="h-8 w-8" />
        </span>
        <h3 className="text-lg font-semibold text-slate-800 dark:text-slate-100">
          {data.toReview ? `${data.toReview} candidate${data.toReview === 1 ? '' : 's'} awaiting review` : 'You are all caught up'}
        </h3>
        <p className="mx-auto mt-1 max-w-md text-sm text-slate-500">Review shortlisted candidates, record evaluations and make hiring decisions.</p>
        <Link to="/hiring-manager/candidates" className="btn-primary mt-5">Go to review queue</Link>
      </motion.div>
    </div>
  );
}
