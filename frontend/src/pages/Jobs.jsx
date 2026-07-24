import { useEffect, useState } from 'react';
import { motion } from 'framer-motion';
import { FiSearch, FiMapPin, FiX, FiZap } from 'react-icons/fi';
import { jobsApi } from '../api';
import { getErrorMessage } from '../api/client';
import { useToast } from '../contexts/ToastContext';
import { EMPLOYMENT_TYPES, EXPERIENCE_LEVELS, humanize } from '../constants';
import JobCard from '../components/JobCard';
import { Pagination, EmptyState, Skeleton } from '../components/ui';

const EMPTY_FILTERS = {
  search: '', location: '', employmentType: '', experienceLevel: '', isRemote: '', page: 1, pageSize: 9,
};

export default function Jobs() {
  const { toast } = useToast();
  const [data, setData] = useState(null);
  const [loading, setLoading] = useState(true);
  const [filters, setFilters] = useState(EMPTY_FILTERS);

  useEffect(() => {
    let active = true;
    setLoading(true);
    const params = Object.fromEntries(
      Object.entries(filters).filter(([, v]) => v !== '' && v !== null && v !== undefined),
    );
    jobsApi.search(params)
      .then((res) => { if (active) setData(res); })
      .catch((e) => toast(getErrorMessage(e), 'error'))
      .finally(() => { if (active) setLoading(false); });
    return () => { active = false; };
  }, [filters, toast]);

  const update = (patch) => setFilters((f) => ({ ...f, ...patch, page: patch.page ?? 1 }));
  const hasFilters = filters.search || filters.location || filters.employmentType || filters.experienceLevel || filters.isRemote;

  return (
    <div className="overflow-hidden">
      {/* ===== Hero + search ===== */}
      <section className="relative isolate">
        <div className="pointer-events-none absolute inset-0 -z-10 overflow-hidden">
          <div className="absolute -left-24 -top-24 h-96 w-96 rounded-full bg-brand-300/40 blur-3xl animate-blob dark:bg-brand-600/20" />
          <div className="absolute right-0 top-0 h-96 w-96 rounded-full bg-accent-300/40 blur-3xl animate-blob dark:bg-accent-600/20" style={{ animationDelay: '3s' }} />
        </div>

        <div className="mx-auto max-w-4xl px-4 py-16 text-center sm:py-20">
          <motion.span initial={{ opacity: 0, y: 16 }} animate={{ opacity: 1, y: 0 }} className="eyebrow">
            <FiZap className="h-3.5 w-3.5" /> {data ? `${data.totalCount.toLocaleString()} open roles` : 'Open roles'}
          </motion.span>
          <motion.h1
            initial={{ opacity: 0, y: 16 }} animate={{ opacity: 1, y: 0 }} transition={{ delay: 0.05 }}
            className="mt-5 text-4xl font-extrabold tracking-tight text-slate-900 dark:text-white sm:text-5xl"
          >
            Find your next <span className="text-gradient">opportunity</span>
          </motion.h1>
          <motion.p
            initial={{ opacity: 0, y: 16 }} animate={{ opacity: 1, y: 0 }} transition={{ delay: 0.1 }}
            className="mx-auto mt-4 max-w-xl text-lg text-slate-600 dark:text-slate-300"
          >
            Search thousands of roles matched to your skills by AI.
          </motion.p>

          {/* big search bar */}
          <motion.div
            initial={{ opacity: 0, y: 16 }} animate={{ opacity: 1, y: 0 }} transition={{ delay: 0.15 }}
            className="mx-auto mt-8 flex max-w-2xl items-center gap-2 rounded-2xl border border-slate-200/80 bg-white p-2 shadow-[var(--shadow-card)] dark:border-slate-700 dark:bg-slate-900"
          >
            <div className="relative flex-1">
              <FiSearch className="pointer-events-none absolute left-4 top-1/2 h-5 w-5 -translate-y-1/2 text-slate-400" />
              <input
                className="w-full rounded-xl bg-transparent py-3 pl-12 pr-3 text-sm text-slate-900 placeholder:text-slate-400 focus:outline-none dark:text-slate-100"
                placeholder="Job title, company or keyword…"
                value={filters.search}
                onChange={(e) => update({ search: e.target.value })}
              />
            </div>
            <div className="relative hidden w-44 sm:block">
              <FiMapPin className="pointer-events-none absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-slate-400" />
              <input
                className="w-full rounded-xl border-l border-slate-200 bg-transparent py-3 pl-9 pr-3 text-sm text-slate-900 placeholder:text-slate-400 focus:outline-none dark:border-slate-700 dark:text-slate-100"
                placeholder="Location"
                value={filters.location}
                onChange={(e) => update({ location: e.target.value })}
              />
            </div>
            <button className="btn-primary shrink-0 !px-5 !py-3" onClick={() => update({})}>
              <FiSearch /> <span className="hidden sm:inline">Search</span>
            </button>
          </motion.div>
        </div>
      </section>

      {/* ===== Filters + results ===== */}
      <section className="mx-auto max-w-6xl px-4 pb-16">
        {/* filter bar */}
        <div className="mb-6 flex flex-wrap items-center gap-2">
          <select className="input w-auto" value={filters.employmentType} onChange={(e) => update({ employmentType: e.target.value })}>
            <option value="">Any type</option>
            {EMPLOYMENT_TYPES.map((t) => <option key={t} value={t}>{humanize(t)}</option>)}
          </select>
          <select className="input w-auto" value={filters.experienceLevel} onChange={(e) => update({ experienceLevel: e.target.value })}>
            <option value="">Any level</option>
            {EXPERIENCE_LEVELS.map((t) => <option key={t} value={t}>{humanize(t)}</option>)}
          </select>
          <button
            onClick={() => update({ isRemote: filters.isRemote === 'true' ? '' : 'true' })}
            className={`rounded-xl px-4 py-2.5 text-sm font-semibold ring-1 transition-all ${
              filters.isRemote === 'true'
                ? 'bg-brand-600 text-white ring-brand-600'
                : 'bg-white text-slate-700 ring-slate-200 hover:bg-slate-50 dark:bg-slate-800 dark:text-slate-100 dark:ring-slate-700'
            }`}
          >
            Remote only
          </button>
          {hasFilters && (
            <button onClick={() => setFilters(EMPTY_FILTERS)} className="btn-ghost btn-sm">
              <FiX /> Clear filters
            </button>
          )}
          {!loading && data && (
            <span className="ml-auto text-sm text-slate-500 dark:text-slate-400">
              <span className="font-semibold text-slate-700 dark:text-slate-200">{data.totalCount.toLocaleString()}</span> result{data.totalCount === 1 ? '' : 's'}
            </span>
          )}
        </div>

        {/* results */}
        {loading ? (
          <div className="grid grid-cols-1 gap-4 md:grid-cols-2 lg:grid-cols-3">
            {Array.from({ length: 6 }).map((_, i) => (
              <div key={i} className="card space-y-3 p-5">
                <Skeleton className="h-5 w-2/3" />
                <Skeleton className="h-4 w-1/3" />
                <Skeleton className="h-4 w-full" />
                <Skeleton className="h-4 w-1/2" />
              </div>
            ))}
          </div>
        ) : data?.items?.length ? (
          <>
            <div className="grid grid-cols-1 gap-4 md:grid-cols-2 lg:grid-cols-3">
              {data.items.map((job, i) => (
                <motion.div
                  key={job.id}
                  initial={{ opacity: 0, y: 16 }}
                  animate={{ opacity: 1, y: 0 }}
                  transition={{ delay: Math.min(i * 0.04, 0.3) }}
                >
                  <JobCard job={job} />
                </motion.div>
              ))}
            </div>
            <Pagination page={data.page} totalPages={data.totalPages} onChange={(p) => update({ page: p })} />
          </>
        ) : (
          <EmptyState
            title="No jobs found"
            message="Try adjusting your search or clearing the filters."
            icon={FiSearch}
            action={hasFilters ? <button onClick={() => setFilters(EMPTY_FILTERS)} className="btn-primary mt-2">Clear filters</button> : null}
          />
        )}
      </section>
    </div>
  );
}
