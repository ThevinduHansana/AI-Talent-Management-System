import { useCallback, useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { FiInbox, FiArrowRight } from 'react-icons/fi';
import { hiringManagerApi } from '../../api';
import { getErrorMessage } from '../../api/client';
import { useToast } from '../../contexts/ToastContext';
import { humanize } from '../../constants';
import { PageHeader, Pagination, EmptyState, Skeleton, StatusBadge } from '../../components/ui';

const STATUS_FILTERS = ['', 'Shortlisted', 'InterviewScheduled', 'Interviewed', 'Offered'];

export default function ReviewQueue() {
  const { toast } = useToast();
  const [data, setData] = useState(null);
  const [loading, setLoading] = useState(true);
  const [status, setStatus] = useState('');
  const [page, setPage] = useState(1);

  const load = useCallback(() => {
    setLoading(true);
    hiringManagerApi.reviewQueue({ page, pageSize: 10, ...(status ? { status } : {}) })
      .then(setData)
      .catch((e) => toast(getErrorMessage(e), 'error'))
      .finally(() => setLoading(false));
  }, [page, status, toast]);

  useEffect(() => { load(); }, [load]);

  return (
    <div>
      <PageHeader icon={FiInbox} title="Review Candidates" subtitle="Shortlisted candidates in your organization" />

      <div className="mb-4 flex flex-wrap gap-2">
        {STATUS_FILTERS.map((s) => (
          <button key={s || 'all'} onClick={() => { setStatus(s); setPage(1); }}
            className={`badge cursor-pointer ${status === s ? 'bg-brand-600 text-white' : 'bg-slate-100 text-slate-600 dark:bg-slate-800 dark:text-slate-300'}`}>
            {s ? humanize(s) : 'All'}
          </button>
        ))}
      </div>

      {loading ? (
        <div className="space-y-3">{Array.from({ length: 4 }).map((_, i) => <Skeleton key={i} className="h-20 w-full" />)}</div>
      ) : data?.items?.length ? (
        <>
          <div className="card divide-y divide-slate-100 dark:divide-slate-800">
            {data.items.map((c) => (
              <Link key={c.applicationId} to={`/hiring-manager/candidates/${c.applicationId}`}
                className="flex items-center justify-between p-4 hover:bg-slate-50 dark:hover:bg-slate-800/50">
                <div className="flex items-center gap-3">
                  <div className="flex h-10 w-10 items-center justify-center rounded-full bg-gradient-to-br from-brand-600 to-accent-500 text-sm font-semibold text-white">
                    {c.candidateName?.split(' ').map((n) => n[0]).slice(0, 2).join('')}
                  </div>
                  <div>
                    <div className="font-medium text-slate-800 dark:text-slate-100">{c.candidateName}</div>
                    <div className="text-xs text-slate-500">{c.headline || c.candidateEmail} · applied for {c.jobTitle}</div>
                  </div>
                </div>
                <div className="flex items-center gap-4">
                  {c.matchScore != null && (
                    <span className="hidden text-xs font-semibold text-slate-500 sm:block">Match {c.matchScore}%</span>
                  )}
                  {c.hasEvaluation && (
                    <span className="badge bg-cyan-100 text-cyan-700 dark:bg-cyan-900/40 dark:text-cyan-300">Scored {c.overallScore}</span>
                  )}
                  <StatusBadge status={c.status} />
                  <FiArrowRight className="text-slate-400" />
                </div>
              </Link>
            ))}
          </div>
          <Pagination page={data.page} totalPages={data.totalPages} onChange={setPage} />
        </>
      ) : (
        <EmptyState title="No candidates to review" message="Candidates appear here once recruiters shortlist them." icon={FiInbox} />
      )}
    </div>
  );
}
