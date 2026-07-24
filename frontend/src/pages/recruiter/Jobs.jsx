import { useCallback, useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { motion } from 'framer-motion';
import { FiPlus, FiLayers, FiUsers, FiEdit2, FiTrash2, FiUsers as FiPipeline } from 'react-icons/fi';
import { recruiterApi } from '../../api';
import { getErrorMessage } from '../../api/client';
import { useToast } from '../../contexts/ToastContext';
import { humanize } from '../../constants';
import { PageHeader, Pagination, EmptyState, Skeleton } from '../../components/ui';
import Modal from '../../components/Modal';

const STATUS_STYLE = {
  Draft: 'bg-slate-200 text-slate-600 dark:bg-slate-700 dark:text-slate-300',
  Open: 'bg-emerald-100 text-emerald-700 dark:bg-emerald-900/40 dark:text-emerald-300',
  OnHold: 'bg-amber-100 text-amber-700 dark:bg-amber-900/40 dark:text-amber-300',
  Closed: 'bg-red-100 text-red-700 dark:bg-red-900/40 dark:text-red-300',
};

export default function RecruiterJobs() {
  const { toast } = useToast();
  const [data, setData] = useState(null);
  const [loading, setLoading] = useState(true);
  const [status, setStatus] = useState('');
  const [page, setPage] = useState(1);
  const [toDelete, setToDelete] = useState(null);

  const load = useCallback(() => {
    setLoading(true);
    recruiterApi.myJobs({ page, pageSize: 8, ...(status ? { status } : {}) })
      .then(setData)
      .catch((e) => toast(getErrorMessage(e), 'error'))
      .finally(() => setLoading(false));
  }, [page, status, toast]);

  useEffect(() => { load(); }, [load]);

  const confirmDelete = async () => {
    try {
      await recruiterApi.deleteJob(toDelete.id);
      toast('Job deleted.', 'success');
      setToDelete(null);
      load();
    } catch (e) {
      toast(getErrorMessage(e), 'error');
    }
  };

  return (
    <div>
      <PageHeader icon={FiLayers} title="My Jobs" subtitle="Manage your job postings"
        actions={<Link to="/recruiter/jobs/new" className="btn-primary"><FiPlus /> Post a job</Link>} />

      <div className="mb-4 flex flex-wrap gap-2">
        {['', 'Draft', 'Open', 'OnHold', 'Closed'].map((s) => (
          <button key={s || 'all'} onClick={() => { setStatus(s); setPage(1); }}
            className={`badge cursor-pointer ${status === s ? 'bg-brand-600 text-white' : 'bg-slate-100 text-slate-600 dark:bg-slate-800 dark:text-slate-300'}`}>
            {s ? humanize(s) : 'All'}
          </button>
        ))}
      </div>

      {loading ? (
        <div className="space-y-3">{Array.from({ length: 4 }).map((_, i) => <Skeleton key={i} className="h-24 w-full" />)}</div>
      ) : data?.items?.length ? (
        <>
          <div className="grid grid-cols-1 gap-4 md:grid-cols-2">
            {data.items.map((job, i) => (
              <motion.div
                key={job.id}
                initial={{ opacity: 0, y: 16 }}
                animate={{ opacity: 1, y: 0 }}
                transition={{ delay: i * 0.05 }}
                className="card card-hover p-5"
              >
                <div className="flex items-start justify-between">
                  <div>
                    <Link to={`/recruiter/jobs/${job.id}/pipeline`} className="font-semibold text-slate-900 hover:text-brand-600 dark:text-white">
                      {job.title}
                    </Link>
                    <div className="mt-1 text-xs text-slate-500">{humanize(job.employmentType)} · {humanize(job.experienceLevel)} · {job.location || '—'}</div>
                  </div>
                  <span className={`badge ${STATUS_STYLE[job.status]}`}>{humanize(job.status)}</span>
                </div>

                <div className="mt-4 flex items-center gap-4 text-sm text-slate-500">
                  <span className="flex items-center gap-1"><FiUsers className="h-4 w-4" /> {job.applicationCount} applicant{job.applicationCount === 1 ? '' : 's'}</span>
                  <span className="flex items-center gap-1"><FiLayers className="h-4 w-4" /> {job.shortlistedCount} shortlisted</span>
                </div>

                <div className="mt-4 flex items-center gap-2 border-t border-slate-100 pt-3 dark:border-slate-800">
                  <Link to={`/recruiter/jobs/${job.id}/pipeline`} className="btn-secondary flex-1"><FiPipeline /> Pipeline</Link>
                  <Link to={`/recruiter/jobs/${job.id}/edit`} className="btn-secondary" aria-label="Edit"><FiEdit2 /></Link>
                  <button onClick={() => setToDelete(job)} className="btn-secondary text-red-600" aria-label="Delete"><FiTrash2 /></button>
                </div>
              </motion.div>
            ))}
          </div>
          <Pagination page={data.page} totalPages={data.totalPages} onChange={setPage} />
        </>
      ) : (
        <EmptyState title="No jobs yet" message="Post your first job to start receiving applications." icon={FiLayers}
          action={<Link to="/recruiter/jobs/new" className="btn-primary mt-2"><FiPlus /> Post a job</Link>} />
      )}

      <Modal open={!!toDelete} onClose={() => setToDelete(null)} title="Delete job"
        footer={<><button className="btn-secondary" onClick={() => setToDelete(null)}>Cancel</button>
          <button className="btn-danger" onClick={confirmDelete}>Delete</button></>}>
        <p className="text-sm text-slate-600 dark:text-slate-300">
          Delete <strong>{toDelete?.title}</strong>? This also removes its applications and cannot be undone.
        </p>
      </Modal>
    </div>
  );
}
