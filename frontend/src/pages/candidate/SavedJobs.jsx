import { useCallback, useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { motion } from 'framer-motion';
import { FiBookmark, FiMapPin, FiTrash2 } from 'react-icons/fi';
import { applicationsApi } from '../../api';
import { getErrorMessage } from '../../api/client';
import { useToast } from '../../contexts/ToastContext';
import { PageHeader, EmptyState, Skeleton } from '../../components/ui';

export default function SavedJobs() {
  const { toast } = useToast();
  const [items, setItems] = useState(null);

  const load = useCallback(() => {
    applicationsApi.saved()
      .then(setItems)
      .catch((e) => { toast(getErrorMessage(e), 'error'); setItems([]); });
  }, [toast]);

  useEffect(() => { load(); }, [load]);

  const unsave = async (jobId) => {
    try {
      await applicationsApi.unsaveJob(jobId);
      setItems((prev) => prev.filter((i) => i.jobId !== jobId));
      toast('Removed from saved jobs.', 'success');
    } catch (e) {
      toast(getErrorMessage(e), 'error');
    }
  };

  return (
    <div>
      <PageHeader icon={FiBookmark} title="Saved Jobs" subtitle="Jobs you've bookmarked for later" />

      {items === null ? (
        <div className="grid grid-cols-1 gap-4 md:grid-cols-2">
          {Array.from({ length: 4 }).map((_, i) => <Skeleton key={i} className="h-28 w-full" />)}
        </div>
      ) : items.length ? (
        <div className="grid grid-cols-1 gap-4 md:grid-cols-2">
          {items.map((job, i) => (
            <motion.div
              key={job.id}
              initial={{ opacity: 0, y: 16 }}
              animate={{ opacity: 1, y: 0 }}
              transition={{ delay: i * 0.05 }}
              className="card card-hover p-5"
            >
              <div className="flex items-start justify-between">
                <div>
                  <Link to={`/jobs/${job.jobId}`} className="font-semibold text-slate-900 hover:text-brand-600 dark:text-white">
                    {job.jobTitle}
                  </Link>
                  <p className="text-sm text-slate-500">{job.organizationName}</p>
                </div>
                <button onClick={() => unsave(job.jobId)} className="text-slate-400 hover:text-red-500" aria-label="Remove">
                  <FiTrash2 className="h-4 w-4" />
                </button>
              </div>
              {job.location && (
                <p className="mt-3 flex items-center gap-1 text-sm text-slate-500"><FiMapPin className="h-4 w-4" /> {job.location}</p>
              )}
              <div className="mt-4 flex items-center justify-between">
                <span className="text-xs text-slate-400">Saved {new Date(job.savedAt).toLocaleDateString()}</span>
                <Link to={`/jobs/${job.jobId}`} className="text-sm font-medium text-brand-600 hover:underline">View &amp; apply →</Link>
              </div>
            </motion.div>
          ))}
        </div>
      ) : (
        <EmptyState title="No saved jobs" message="Save jobs while browsing to revisit them here." icon={FiBookmark}
          action={<Link to="/jobs" className="btn-primary mt-2">Browse jobs</Link>} />
      )}
    </div>
  );
}
