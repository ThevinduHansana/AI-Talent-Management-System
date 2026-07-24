import { useEffect, useState } from 'react';
import {
  FiUsers, FiBriefcase, FiFileText, FiCalendar, FiUserCheck, FiTrendingUp,
} from 'react-icons/fi';
import {
  ResponsiveContainer, AreaChart, Area, XAxis, YAxis, CartesianGrid, Tooltip, Legend,
  PieChart, Pie, Cell, BarChart, Bar,
} from 'recharts';
import { adminApi } from '../../api';
import { getErrorMessage } from '../../api/client';
import { useToast } from '../../contexts/ToastContext';
import { humanize } from '../../constants';
import { PageHeader, LoadingScreen, StatCard, SectionHeading } from '../../components/ui';

const STATUS_COLORS = ['#6366f1', '#f59e0b', '#8b5cf6', '#6366f1', '#06b6d4', '#10b981', '#22c55e', '#ef4444', '#94a3b8'];

export default function AnalyticsDashboard() {
  const { toast } = useToast();
  const [data, setData] = useState(null);

  useEffect(() => {
    adminApi.analytics().then(setData).catch((e) => { toast(getErrorMessage(e), 'error'); setData({}); });
  }, [toast]);

  if (!data) return <LoadingScreen />;

  const kpis = [
    { label: 'Candidates', value: data.totalCandidates ?? 0, icon: FiUsers, tone: 'brand' },
    { label: 'Active Jobs', value: data.activeJobs ?? 0, icon: FiBriefcase, tone: 'emerald' },
    { label: 'Applications', value: data.totalApplications ?? 0, icon: FiFileText, tone: 'violet' },
    { label: 'Interviews', value: data.totalInterviews ?? 0, icon: FiCalendar, tone: 'amber' },
    { label: 'Hires', value: data.hires ?? 0, icon: FiUserCheck, tone: 'emerald' },
    { label: 'Hiring Rate', value: `${data.hiringRate ?? 0}%`, icon: FiTrendingUp, tone: 'accent' },
  ];

  // Merge the two monthly series by period for a combined chart.
  const trend = (data.monthlyApplications || []).map((m, i) => ({
    period: m.period,
    Applications: m.value,
    Hires: data.monthlyHires?.[i]?.value ?? 0,
  }));

  const statusData = (data.applicationsByStatus || []).map((s) => ({ name: humanize(s.status), value: s.count }));

  return (
    <div>
      <PageHeader icon={FiTrendingUp} title="Analytics" subtitle="Platform-wide recruitment performance" />

      <div className="grid grid-cols-2 gap-4 md:grid-cols-3 lg:grid-cols-6">
        {kpis.map((k, i) => (
          <StatCard key={k.label} index={i} label={k.label} value={k.value} icon={k.icon} tone={k.tone} />
        ))}
      </div>

      <div className="mt-6 grid grid-cols-1 gap-6 lg:grid-cols-3">
        <div className="card p-5 lg:col-span-2">
          <SectionHeading title="Applications & hires (last 6 months)" />
          <ResponsiveContainer width="100%" height={280}>
            <AreaChart data={trend}>
              <defs>
                <linearGradient id="gApps" x1="0" y1="0" x2="0" y2="1">
                  <stop offset="5%" stopColor="#6366f1" stopOpacity={0.4} />
                  <stop offset="95%" stopColor="#6366f1" stopOpacity={0} />
                </linearGradient>
                <linearGradient id="gHires" x1="0" y1="0" x2="0" y2="1">
                  <stop offset="5%" stopColor="#22c55e" stopOpacity={0.4} />
                  <stop offset="95%" stopColor="#22c55e" stopOpacity={0} />
                </linearGradient>
              </defs>
              <CartesianGrid strokeDasharray="3 3" stroke="rgba(148,163,184,0.2)" />
              <XAxis dataKey="period" tick={{ fontSize: 12 }} stroke="#94a3b8" />
              <YAxis allowDecimals={false} tick={{ fontSize: 12 }} stroke="#94a3b8" />
              <Tooltip />
              <Legend />
              <Area type="monotone" dataKey="Applications" stroke="#6366f1" fill="url(#gApps)" strokeWidth={2} />
              <Area type="monotone" dataKey="Hires" stroke="#22c55e" fill="url(#gHires)" strokeWidth={2} />
            </AreaChart>
          </ResponsiveContainer>
        </div>

        <div className="card p-5">
          <SectionHeading title="Applications by status" />
          {statusData.length ? (
            <ResponsiveContainer width="100%" height={280}>
              <PieChart>
                <Pie data={statusData} dataKey="value" nameKey="name" innerRadius={50} outerRadius={90} paddingAngle={2}>
                  {statusData.map((_, i) => <Cell key={i} fill={STATUS_COLORS[i % STATUS_COLORS.length]} />)}
                </Pie>
                <Tooltip />
              </PieChart>
            </ResponsiveContainer>
          ) : <Empty />}
        </div>
      </div>

      <div className="mt-6 grid grid-cols-1 gap-6 lg:grid-cols-2">
        <ChartCard title="Top skills in demand">
          {data.topSkills?.length ? (
            <ResponsiveContainer width="100%" height={260}>
              <BarChart data={data.topSkills} layout="vertical" margin={{ left: 20 }}>
                <XAxis type="number" allowDecimals={false} tick={{ fontSize: 12 }} stroke="#94a3b8" />
                <YAxis type="category" dataKey="label" width={90} tick={{ fontSize: 12 }} stroke="#94a3b8" />
                <Tooltip cursor={{ fill: 'rgba(99,102,241,0.08)' }} />
                <Bar dataKey="value" fill="#6366f1" radius={[0, 6, 6, 0]} />
              </BarChart>
            </ResponsiveContainer>
          ) : <Empty />}
        </ChartCard>

        <ChartCard title="Department hiring">
          {data.departmentHiring?.length ? (
            <ResponsiveContainer width="100%" height={260}>
              <BarChart data={data.departmentHiring}>
                <XAxis dataKey="label" tick={{ fontSize: 12 }} stroke="#94a3b8" />
                <YAxis allowDecimals={false} tick={{ fontSize: 12 }} stroke="#94a3b8" />
                <Tooltip cursor={{ fill: 'rgba(34,197,94,0.08)' }} />
                <Bar dataKey="value" fill="#22c55e" radius={[6, 6, 0, 0]} />
              </BarChart>
            </ResponsiveContainer>
          ) : <Empty message="No hires recorded yet" />}
        </ChartCard>
      </div>

      <div className="mt-6">
        <SectionHeading title="Recruiter performance" />
        {data.recruiterPerformance?.length ? (
          <div className="table-wrap">
            <table className="table-base">
              <thead>
                <tr>
                  <th>Recruiter</th>
                  <th className="text-right">Jobs Posted</th>
                  <th className="text-right">Applications</th>
                  <th className="text-right">Hires</th>
                </tr>
              </thead>
              <tbody>
                {data.recruiterPerformance.map((r) => (
                  <tr key={r.recruiterName}>
                    <td className="font-medium text-slate-700 dark:text-slate-200">{r.recruiterName}</td>
                    <td className="text-right">{r.jobsPosted}</td>
                    <td className="text-right">{r.applications}</td>
                    <td className="text-right font-semibold text-emerald-600">{r.hires}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        ) : <div className="card"><Empty /></div>}
      </div>
    </div>
  );
}

const ChartCard = ({ title, children }) => (
  <div className="card p-5">
    <SectionHeading title={title} />
    {children}
  </div>
);
const Empty = ({ message = 'No data yet' }) => <p className="py-16 text-center text-sm text-slate-400">{message}</p>;
