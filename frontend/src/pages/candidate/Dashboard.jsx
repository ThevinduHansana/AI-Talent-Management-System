import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { motion } from 'framer-motion';
import { FiFileText, FiBookmark, FiCheckCircle, FiSearch, FiUser } from 'react-icons/fi';
import {
  BarChart, Bar, XAxis, YAxis, Tooltip, ResponsiveContainer, PieChart, Pie, Cell,
} from 'recharts';
import { applicationsApi, candidateApi } from '../../api';
import { getErrorMessage } from '../../api/client';
import { useAuth } from '../../contexts/AuthContext';
import { useToast } from '../../contexts/ToastContext';
import { humanize } from '../../constants';
import { PageHeader, LoadingScreen, StatusBadge, EmptyState, StatCard, SectionHeading } from '../../components/ui';

const PIE_COLORS = ['#6366f1', '#22c55e', '#f59e0b', '#ef4444', '#06b6d4', '#a855f7', '#64748b'];

export default function Dashboard() {
  const { user } = useAuth();
  const { toast } = useToast();
  const [state, setState] = useState({ loading: true, applications: [], saved: [], profile: null });

  useEffect(() => {
    let active = true;
    Promise.all([
      applicationsApi.mine({ pageSize: 100 }),
      applicationsApi.saved(),
      candidateApi.getProfile(),
    ])
      .then(([apps, saved, profile]) => {
        if (active) setState({ loading: false, applications: apps.items, saved, profile });
      })
      .catch((e) => { toast(getErrorMessage(e), 'error'); if (active) setState((s) => ({ ...s, loading: false })); });
    return () => { active = false; };
  }, [toast]);

  if (state.loading) return <LoadingScreen />;

  const { applications, saved, profile } = state;
  const active = applications.filter((a) => !['Rejected', 'Withdrawn', 'Hired'].includes(a.status));
  const interviews = applications.filter((a) => ['InterviewScheduled', 'Interviewed'].includes(a.status));

  const statusCounts = applications.reduce((acc, a) => {
    acc[a.status] = (acc[a.status] || 0) + 1;
    return acc;
  }, {});
  const pieData = Object.entries(statusCounts).map(([name, value]) => ({ name: humanize(name), value }));

  const profileFields = [profile?.headline, profile?.summary, profile?.location, profile?.currentPosition];
  const filled = profileFields.filter(Boolean).length + (profile?.skills?.length ? 1 : 0) + (profile?.experience?.length ? 1 : 0);
  const completeness = Math.round((filled / 6) * 100);

  const barData = [
    { name: 'Applied', value: applications.length },
    { name: 'Active', value: active.length },
    { name: 'Interviews', value: interviews.length },
    { name: 'Saved', value: saved.length },
  ];

  const stats = [
    { label: 'Applications', value: applications.length, icon: FiFileText, tone: 'brand' },
    { label: 'Active', value: active.length, icon: FiCheckCircle, tone: 'emerald' },
    { label: 'Interviews', value: interviews.length, icon: FiUser, tone: 'amber' },
    { label: 'Saved Jobs', value: saved.length, icon: FiBookmark, tone: 'violet' },
  ];

  return (
    <div>
      <PageHeader
        title={`Welcome back, ${user?.firstName}`}
        subtitle="Here's a snapshot of your job search"
        actions={<Link to="/jobs" className="btn-primary"><FiSearch /> Find jobs</Link>}
      />

      <div className="grid grid-cols-2 gap-4 lg:grid-cols-4">
        {stats.map((s, i) => (
          <StatCard key={s.label} index={i} label={s.label} value={s.value} icon={s.icon} tone={s.tone} />
        ))}
      </div>

      <div className="mt-6 grid grid-cols-1 gap-6 lg:grid-cols-3">
        <div className="card p-5 lg:col-span-2">
          <SectionHeading title="Application overview" />
          <ResponsiveContainer width="100%" height={240}>
            <BarChart data={barData}>
              <XAxis dataKey="name" tick={{ fontSize: 12 }} stroke="#94a3b8" />
              <YAxis allowDecimals={false} tick={{ fontSize: 12 }} stroke="#94a3b8" />
              <Tooltip cursor={{ fill: 'rgba(99,102,241,0.08)' }} />
              <Bar dataKey="value" fill="#6366f1" radius={[6, 6, 0, 0]} />
            </BarChart>
          </ResponsiveContainer>
        </div>

        <div className="card p-5">
          <SectionHeading title="By status" />
          {pieData.length ? (
            <ResponsiveContainer width="100%" height={240}>
              <PieChart>
                <Pie data={pieData} dataKey="value" nameKey="name" innerRadius={45} outerRadius={80} paddingAngle={3}>
                  {pieData.map((_, i) => <Cell key={i} fill={PIE_COLORS[i % PIE_COLORS.length]} />)}
                </Pie>
                <Tooltip />
              </PieChart>
            </ResponsiveContainer>
          ) : (
            <p className="py-16 text-center text-sm text-slate-400">No applications yet</p>
          )}
        </div>
      </div>

      <div className="mt-6 grid grid-cols-1 gap-6 lg:grid-cols-3">
        <div className="card p-5 lg:col-span-2">
          <SectionHeading title="Recent applications" action={<Link to="/candidate/applications" className="text-sm font-medium text-brand-600 hover:underline">View all</Link>} />
          {applications.length ? (
            <div className="divide-y divide-slate-100 dark:divide-slate-800">
              {applications.slice(0, 5).map((a) => (
                <div key={a.id} className="flex items-center justify-between py-3">
                  <div>
                    <div className="font-medium text-slate-800 dark:text-slate-100">{a.jobTitle}</div>
                    <div className="text-xs text-slate-500">{a.organizationName}</div>
                  </div>
                  <StatusBadge status={a.status} />
                </div>
              ))}
            </div>
          ) : (
            <EmptyState title="No applications yet" message="Browse jobs and apply to get started." icon={FiFileText}
              action={<Link to="/jobs" className="btn-primary mt-2">Browse jobs</Link>} />
          )}
        </div>

        <div className="card p-5">
          <SectionHeading title="Profile strength" />
          <div className="mb-2 flex items-center justify-between text-sm">
            <span className="text-slate-500">Completeness</span>
            <span className="font-semibold text-brand-600">{completeness}%</span>
          </div>
          <div className="h-2.5 overflow-hidden rounded-full bg-slate-100 dark:bg-slate-800">
            <motion.div
              className="h-full rounded-full bg-gradient-to-r from-brand-600 to-accent-500"
              initial={{ width: 0 }}
              animate={{ width: `${completeness}%` }}
              transition={{ duration: 0.8, ease: 'easeOut' }}
            />
          </div>
          <Link to="/candidate/profile" className="btn-secondary mt-4 w-full">Complete your profile</Link>
        </div>
      </div>
    </div>
  );
}
