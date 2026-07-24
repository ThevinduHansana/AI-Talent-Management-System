import { useCallback, useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { motion } from 'framer-motion';
import { FiZap, FiMapPin, FiBriefcase, FiTarget } from 'react-icons/fi';
import { aiApi } from '../../api';
import { getErrorMessage } from '../../api/client';
import { useToast } from '../../contexts/ToastContext';
import { humanize } from '../../constants';
import { PageHeader, EmptyState, Skeleton } from '../../components/ui';

function MatchRing({ score }) {
  const color = score >= 70 ? 'text-emerald-500' : score >= 40 ? 'text-amber-500' : 'text-slate-400';
  return (
    <div className="relative flex h-14 w-14 items-center justify-center">
      <svg className="h-14 w-14 -rotate-90" viewBox="0 0 36 36">
        <circle cx="18" cy="18" r="16" fill="none" className="stroke-slate-100 dark:stroke-slate-700" strokeWidth="3" />
        <circle cx="18" cy="18" r="16" fill="none" className={`${color} stroke-current`} strokeWidth="3"
          strokeDasharray={`${(score / 100) * 100.53} 100.53`} strokeLinecap="round" />
      </svg>
      <span className={`absolute text-xs font-bold ${color}`}>{Math.round(score)}%</span>
    </div>
  );
}

export default function Recommendations() {
  const { toast } = useToast();
  const [items, setItems] = useState(null);

  const load = useCallback(() => {
    aiApi.recommendations(8).then(setItems).catch((e) => { toast(getErrorMessage(e), 'error'); setItems([]); });
  }, [toast]);

  useEffect(() => { load(); }, [load]);

  return (
    <div>
      <PageHeader icon={FiZap} title="Recommended for you"
        subtitle="AI-matched jobs based on your skills and profile" />

      {items === null ? (
        <div className="grid grid-cols-1 gap-4 md:grid-cols-2">{Array.from({ length: 4 }).map((_, i) => <Skeleton key={i} className="h-40 w-full" />)}</div>
      ) : items.length ? (
        <div className="grid grid-cols-1 gap-4 md:grid-cols-2">
          {items.map(({ job, matchScore, matchingSkills }, i) => (
            <motion.div
              key={job.id}
              initial={{ opacity: 0, y: 16 }}
              animate={{ opacity: 1, y: 0 }}
              transition={{ delay: i * 0.05 }}
              className="card card-hover p-5"
            >
              <div className="flex items-start justify-between gap-3">
                <div className="min-w-0">
                  <Link to={`/jobs/${job.id}`} className="font-semibold text-slate-900 hover:text-brand-600 dark:text-white">{job.title}</Link>
                  <p className="text-sm text-slate-500">{job.organizationName}</p>
                </div>
                <MatchRing score={matchScore} />
              </div>

              <div className="mt-3 flex flex-wrap gap-x-4 gap-y-1 text-sm text-slate-500">
                {job.location && <span className="flex items-center gap-1"><FiMapPin className="h-4 w-4" /> {job.location}</span>}
                <span className="flex items-center gap-1"><FiBriefcase className="h-4 w-4" /> {humanize(job.employmentType)}</span>
              </div>

              {matchingSkills.length > 0 && (
                <div className="mt-3">
                  <div className="mb-1 flex items-center gap-1 text-xs font-medium text-slate-400"><FiTarget className="h-3.5 w-3.5" /> Matching skills</div>
                  <div className="flex flex-wrap gap-1">
                    {matchingSkills.map((s) => (
                      <span key={s} className="badge bg-emerald-100 text-emerald-700 dark:bg-emerald-900/40 dark:text-emerald-300">{s}</span>
                    ))}
                  </div>
                </div>
              )}

              <Link to={`/jobs/${job.id}`} className="btn-primary mt-4 w-full">View &amp; apply</Link>
            </motion.div>
          ))}
        </div>
      ) : (
        <EmptyState title="No recommendations yet" icon={FiZap}
          message="Add skills to your profile or analyze your resume to get AI-matched jobs."
          action={<Link to="/candidate/profile" className="btn-primary mt-2">Update profile</Link>} />
      )}
    </div>
  );
}
