import { useCallback, useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { motion } from 'framer-motion';
import { FiFileText, FiExternalLink, FiMessageSquare, FiXCircle, FiClock, FiZap } from 'react-icons/fi';
import { applicationsApi } from '../../api';
import { getErrorMessage } from '../../api/client';
import { useToast } from '../../contexts/ToastContext';
import { humanize } from '../../constants';
import { PageHeader, StatusBadge, Pagination, EmptyState, Skeleton } from '../../components/ui';
import AiFeedbackModal from '../../components/AiFeedbackModal';

const STATUSES = ['Applied', 'UnderReview', 'Shortlisted', 'InterviewScheduled', 'Interviewed', 'Offered', 'Hired', 'Rejected', 'Withdrawn'];

export default function Applications() {
  const { toast } = useToast();
  const [data, setData] = useState(null);
  const [loading, setLoading] = useState(true);
  const [status, setStatus] = useState('');
  const [page, setPage] = useState(1);
  const [feedbackFor, setFeedbackFor] = useState(null);

  const load = useCallback(() => {
    setLoading(true);
    const params = { page, pageSize: 10, ...(status ? { status } : {}) };
    applicationsApi.mine(params)
      .then(setData)
      .catch((e) => toast(getErrorMessage(e), 'error'))
      .finally(() => setLoading(false));
  }, [page, status, toast]);

  useEffect(() => { load(); }, [load]);

  const withdraw = async (id) => {
    try {
      await applicationsApi.withdraw(id);
      toast('Application withdrawn.', 'success');
      load();
    } catch (e) {
      toast(getErrorMessage(e), 'error');
    }
  };

  return (
    <div>
      <PageHeader icon={FiFileText} title="My Applications" subtitle="Track the status of your job applications" />

      {/* status filter pills */}
      <div className="mb-5 flex flex-wrap gap-2">
        {['', ...STATUSES].map((s) => {
          const active = status === s;
          return (
            <button
              key={s || 'all'}
              onClick={() => { setStatus(s); setPage(1); }}
              className={`rounded-full px-3.5 py-1.5 text-xs font-semibold transition-all ${
                active
                  ? 'bg-gradient-to-r from-brand-600 to-accent-500 text-white shadow-sm'
                  : 'bg-white text-slate-600 ring-1 ring-slate-200 hover:bg-slate-50 dark:bg-slate-800 dark:text-slate-300 dark:ring-slate-700 dark:hover:bg-slate-700'
              }`}
            >
              {s ? humanize(s) : 'All'}
            </button>
          );
        })}
      </div>

      {loading ? (
        <div className="space-y-3">
          {Array.from({ length: 4 }).map((_, i) => <Skeleton key={i} className="h-24 w-full rounded-2xl" />)}
        </div>
      ) : data?.items?.length ? (
        <>
          <div className="space-y-3">
            {data.items.map((a, i) => {
              const canWithdraw = !['Hired', 'Rejected', 'Withdrawn'].includes(a.status);
              return (
                <motion.div
                  key={a.id}
                  initial={{ opacity: 0, y: 14 }}
                  animate={{ opacity: 1, y: 0 }}
                  transition={{ delay: Math.min(i * 0.04, 0.25) }}
                  className="card card-hover p-4 sm:p-5"
                >
                  <div className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
                    {/* left: avatar + title */}
                    <div className="flex min-w-0 items-center gap-4">
                      <span className="flex h-12 w-12 shrink-0 items-center justify-center rounded-xl bg-gradient-to-br from-brand-500 to-accent-500 text-lg font-bold text-white shadow-[0_6px_16px_-4px_rgba(79,70,229,0.5)]">
                        {a.organizationName?.[0] || a.jobTitle?.[0] || 'J'}
                      </span>
                      <div className="min-w-0">
                        <Link to={`/jobs/${a.jobId}`} className="block truncate font-semibold text-slate-900 transition-colors hover:text-brand-600 dark:text-white">
                          {a.jobTitle}
                        </Link>
                        <div className="mt-0.5 flex flex-wrap items-center gap-x-2 gap-y-0.5 text-xs text-slate-500 dark:text-slate-400">
                          <span className="truncate">{a.organizationName}</span>
                          <span className="text-slate-300 dark:text-slate-600">·</span>
                          <span className="flex items-center gap-1"><FiClock className="h-3 w-3" /> Applied {new Date(a.appliedAt).toLocaleDateString()}</span>
                        </div>
                      </div>
                    </div>

                    {/* right: status + action buttons */}
                    <div className="flex items-center justify-between gap-3 sm:justify-end">
                      <StatusBadge status={a.status} />
                      <div className="flex items-center gap-2">
                        <button
                          onClick={() => setFeedbackFor(a.id)}
                          title="Get AI feedback on this application"
                          className="btn-secondary btn-sm"
                        >
                          <FiZap /> <span className="hidden sm:inline">AI feedback</span>
                        </button>
                        <Link to={`/jobs/${a.jobId}`} className="btn-secondary btn-sm" title="View job">
                          <FiExternalLink /> <span className="hidden sm:inline">View</span>
                        </Link>
                        {a.recruiterUserId && (
                          <Link to={`/messages/${a.recruiterUserId}`} className="btn-secondary btn-sm" title="Message recruiter">
                            <FiMessageSquare /> <span className="hidden sm:inline">Message</span>
                          </Link>
                        )}
                        {canWithdraw && (
                          <button
                            onClick={() => withdraw(a.id)}
                            title="Withdraw application"
                            className="inline-flex items-center gap-1.5 rounded-xl px-3 py-1.5 text-xs font-semibold text-red-600 ring-1 ring-red-200 transition-all hover:-translate-y-0.5 hover:bg-red-600 hover:text-white hover:ring-red-600 dark:text-red-400 dark:ring-red-900/50"
                          >
                            <FiXCircle /> <span className="hidden sm:inline">Withdraw</span>
                          </button>
                        )}
                      </div>
                    </div>
                  </div>
                </motion.div>
              );
            })}
          </div>
          <Pagination page={data.page} totalPages={data.totalPages} onChange={setPage} />
        </>
      ) : (
        <EmptyState title="No applications" message="You haven't applied to any jobs yet." icon={FiFileText}
          action={<Link to="/jobs" className="btn-primary mt-2">Browse jobs</Link>} />
      )}

      <AiFeedbackModal applicationId={feedbackFor} onClose={() => setFeedbackFor(null)} />
    </div>
  );
}
