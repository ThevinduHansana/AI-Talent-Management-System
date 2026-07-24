import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { FiLayers, FiUsers, FiCheckCircle, FiCalendar, FiPlus } from 'react-icons/fi';
import { BarChart, Bar, XAxis, YAxis, Tooltip, ResponsiveContainer, PieChart, Pie, Cell } from 'recharts';
import { recruiterApi } from '../../api';
import { getErrorMessage } from '../../api/client';
import { useAuth } from '../../contexts/AuthContext';
import { useToast } from '../../contexts/ToastContext';
import { humanize } from '../../constants';
import { PageHeader, LoadingScreen, EmptyState, StatCard, SectionHeading } from '../../components/ui';

const PIE_COLORS = { Draft: '#94a3b8', Open: '#22c55e', OnHold: '#f59e0b', Closed: '#ef4444' };

export default function RecruiterDashboard() {
  const { user } = useAuth();
  const { toast } = useToast();
  const [state, setState] = useState({ loading: true, jobs: [], interviews: [] });

  useEffect(() => {
    Promise.all([recruiterApi.myJobs({ pageSize: 100 }), recruiterApi.upcomingInterviews()])
      .then(([jobs, interviews]) => setState({ loading: false, jobs: jobs.items, interviews }))
      .catch((e) => { toast(getErrorMessage(e), 'error'); setState((s) => ({ ...s, loading: false })); });
  }, [toast]);

  if (state.loading) return <LoadingScreen />;

  const { jobs, interviews } = state;
  const openJobs = jobs.filter((j) => j.status === 'Open');
  const totalApplicants = jobs.reduce((sum, j) => sum + j.applicationCount, 0);
  const totalShortlisted = jobs.reduce((sum, j) => sum + j.shortlistedCount, 0);

  const statusCounts = jobs.reduce((acc, j) => { acc[j.status] = (acc[j.status] || 0) + 1; return acc; }, {});
  const pieData = Object.entries(statusCounts).map(([name, value]) => ({ name, value }));
  const barData = [...jobs].sort((a, b) => b.applicationCount - a.applicationCount).slice(0, 6)
    .map((j) => ({ name: j.title.length > 16 ? j.title.slice(0, 16) + '…' : j.title, applicants: j.applicationCount }));

  const stats = [
    { label: 'Open Jobs', value: openJobs.length, icon: FiLayers, tone: 'brand' },
    { label: 'Total Applicants', value: totalApplicants, icon: FiUsers, tone: 'emerald' },
    { label: 'Shortlisted', value: totalShortlisted, icon: FiCheckCircle, tone: 'violet' },
    { label: 'Upcoming Interviews', value: interviews.length, icon: FiCalendar, tone: 'amber' },
  ];

  return (
    <div>
      <PageHeader title={`Welcome, ${user?.firstName}`} subtitle="Your recruitment activity at a glance"
        actions={<Link to="/recruiter/jobs/new" className="btn-primary"><FiPlus /> Post a job</Link>} />

      <div className="grid grid-cols-2 gap-4 lg:grid-cols-4">
        {stats.map((s, i) => (
          <StatCard key={s.label} index={i} label={s.label} value={s.value} icon={s.icon} tone={s.tone} />
        ))}
      </div>

      <div className="mt-6 grid grid-cols-1 gap-6 lg:grid-cols-3">
        <div className="card p-5 lg:col-span-2">
          <h3 className="mb-4 font-semibold text-slate-800 dark:text-slate-100">Applicants per job</h3>
          {barData.length ? (
            <ResponsiveContainer width="100%" height={260}>
              <BarChart data={barData}>
                <XAxis dataKey="name" tick={{ fontSize: 11 }} stroke="#94a3b8" />
                <YAxis allowDecimals={false} tick={{ fontSize: 12 }} stroke="#94a3b8" />
                <Tooltip cursor={{ fill: 'rgba(99,102,241,0.08)' }} />
                <Bar dataKey="applicants" fill="#6366f1" radius={[6, 6, 0, 0]} />
              </BarChart>
            </ResponsiveContainer>
          ) : <p className="py-16 text-center text-sm text-slate-400">No jobs yet</p>}
        </div>

        <div className="card p-5">
          <h3 className="mb-4 font-semibold text-slate-800 dark:text-slate-100">Jobs by status</h3>
          {pieData.length ? (
            <ResponsiveContainer width="100%" height={260}>
              <PieChart>
                <Pie data={pieData} dataKey="value" nameKey="name" innerRadius={45} outerRadius={80} paddingAngle={3}>
                  {pieData.map((d) => <Cell key={d.name} fill={PIE_COLORS[d.name] || '#6366f1'} />)}
                </Pie>
                <Tooltip />
              </PieChart>
            </ResponsiveContainer>
          ) : <p className="py-16 text-center text-sm text-slate-400">No jobs yet</p>}
        </div>
      </div>

      <div className="mt-6 grid grid-cols-1 gap-6 lg:grid-cols-2">
        <div className="card p-5">
          <SectionHeading title="Recent jobs" action={<Link to="/recruiter/jobs" className="text-sm font-medium text-brand-600 hover:underline">View all</Link>} />
          {jobs.length ? (
            <div className="divide-y divide-slate-100 dark:divide-slate-800">
              {jobs.slice(0, 5).map((j) => (
                <Link key={j.id} to={`/recruiter/jobs/${j.id}/pipeline`} className="flex items-center justify-between py-3 hover:opacity-80">
                  <div>
                    <div className="font-medium text-slate-800 dark:text-slate-100">{j.title}</div>
                    <div className="text-xs text-slate-500">{j.applicationCount} applicants · {humanize(j.status)}</div>
                  </div>
                  <span className="text-sm text-brand-600">Pipeline →</span>
                </Link>
              ))}
            </div>
          ) : (
            <EmptyState title="No jobs" message="Post a job to get started." icon={FiLayers}
              action={<Link to="/recruiter/jobs/new" className="btn-primary mt-2">Post a job</Link>} />
          )}
        </div>

        <div className="card p-5">
          <SectionHeading title="Upcoming interviews" action={<Link to="/recruiter/interviews" className="text-sm font-medium text-brand-600 hover:underline">View all</Link>} />
          {interviews.length ? (
            <div className="divide-y divide-slate-100 dark:divide-slate-800">
              {interviews.slice(0, 5).map((iv) => (
                <div key={iv.id} className="flex items-center justify-between py-3">
                  <div>
                    <div className="font-medium text-slate-800 dark:text-slate-100">{iv.candidateName}</div>
                    <div className="text-xs text-slate-500">{iv.jobTitle}</div>
                  </div>
                  <div className="text-right text-xs text-slate-500">
                    {new Date(iv.scheduledAt).toLocaleDateString()}<br />
                    {new Date(iv.scheduledAt).toLocaleTimeString(undefined, { hour: '2-digit', minute: '2-digit' })}
                  </div>
                </div>
              ))}
            </div>
          ) : (
            <EmptyState title="No interviews scheduled" icon={FiCalendar} />
          )}
        </div>
      </div>
    </div>
  );
}
