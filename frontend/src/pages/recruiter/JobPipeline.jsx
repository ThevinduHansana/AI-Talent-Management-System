import { useCallback, useEffect, useState } from 'react';
import { useParams, Link } from 'react-router-dom';
import { useForm } from 'react-hook-form';
import { FiArrowLeft, FiZap, FiCalendar, FiMail, FiMessageSquare, FiFileText } from 'react-icons/fi';
import { recruiterApi } from '../../api';
import { getErrorMessage } from '../../api/client';
import { useToast } from '../../contexts/ToastContext';
import { humanize } from '../../constants';
import { PageHeader, LoadingScreen, StatusBadge, EmptyState, Spinner } from '../../components/ui';
import Modal from '../../components/Modal';

const RECRUITER_STATUSES = ['UnderReview', 'Shortlisted', 'Interviewed', 'Offered', 'Rejected'];

function MatchMeter({ score }) {
  if (score == null) return <span className="text-xs text-slate-400">Not ranked</span>;
  const color = score >= 70 ? 'bg-emerald-500' : score >= 40 ? 'bg-amber-500' : 'bg-red-500';
  return (
    <div className="flex items-center gap-2">
      <div className="h-2 w-24 overflow-hidden rounded-full bg-slate-100 dark:bg-slate-700">
        <div className={`h-full ${color}`} style={{ width: `${score}%` }} />
      </div>
      <span className="text-xs font-semibold text-slate-600 dark:text-slate-300">{score}%</span>
    </div>
  );
}

export default function JobPipeline() {
  const { id } = useParams();
  const { toast } = useToast();
  const [job, setJob] = useState(null);
  const [apps, setApps] = useState(null);
  const [ranking, setRanking] = useState(false);
  const [scheduleFor, setScheduleFor] = useState(null);
  const [downloadingId, setDownloadingId] = useState(null);

  const load = useCallback(() => {
    Promise.all([recruiterApi.getJob(id), recruiterApi.applicationsForJob(id, { pageSize: 100 })])
      .then(([j, a]) => { setJob(j); setApps(a.items); })
      .catch((e) => toast(getErrorMessage(e), 'error'));
  }, [id, toast]);

  useEffect(() => { load(); }, [load]);

  const rank = async () => {
    setRanking(true);
    try {
      const ranked = await recruiterApi.rank(id);
      setApps(ranked);
      toast('Candidates ranked by AI match score.', 'success');
    } catch (e) {
      toast(getErrorMessage(e), 'error');
    } finally {
      setRanking(false);
    }
  };

  const downloadResume = async (application) => {
    const resume = application.resumes?.[0];
    if (!resume) return;
    setDownloadingId(application.id);
    try {
      const blob = await recruiterApi.downloadResume(application.id, resume.id);
      const url = URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.href = url;
      a.download = resume.fileName;
      a.click();
      URL.revokeObjectURL(url);
    } catch (e) {
      toast(getErrorMessage(e, 'Could not download resume.'), 'error');
    } finally {
      setDownloadingId(null);
    }
  };

  const changeStatus = async (appId, status) => {
    try {
      const updated = await recruiterApi.updateApplicationStatus(appId, { status, notes: null });
      setApps((prev) => prev.map((a) => (a.id === appId ? updated : a)));
      toast(`Marked as ${humanize(status)}.`, 'success');
    } catch (e) {
      toast(getErrorMessage(e), 'error');
    }
  };

  if (!job || !apps) return <LoadingScreen />;

  return (
    <div>
      <Link to="/recruiter/jobs" className="mb-4 inline-flex items-center gap-1 text-sm text-slate-500 hover:text-brand-600">
        <FiArrowLeft /> All jobs
      </Link>
      <PageHeader
        title={job.title}
        subtitle={`${apps.length} applicant${apps.length === 1 ? '' : 's'} · ${humanize(job.status)}`}
        actions={
          <>
            <Link to={`/recruiter/jobs/${id}/edit`} className="btn-secondary">Edit job</Link>
            <button className="btn-primary" onClick={rank} disabled={ranking || apps.length === 0}>
              {ranking ? <Spinner className="h-4 w-4 text-white" /> : <FiZap />} AI Rank
            </button>
          </>
        }
      />

      {apps.length === 0 ? (
        <EmptyState title="No applicants yet" message="Applications will appear here as candidates apply." icon={FiMail} />
      ) : (
        <div className="card divide-y divide-slate-100 dark:divide-slate-800">
          {apps.map((a) => (
            <div key={a.id} className="flex flex-col gap-3 p-4 transition-colors hover:bg-slate-50/70 dark:hover:bg-slate-800/40 sm:flex-row sm:items-center sm:justify-between">
              <div className="flex items-start gap-3">
                <div className="flex h-10 w-10 shrink-0 items-center justify-center rounded-full bg-gradient-to-br from-brand-600 to-accent-500 text-sm font-semibold text-white">
                  {a.candidateName?.split(' ').map((n) => n[0]).slice(0, 2).join('')}
                </div>
                <div>
                  <div className="font-medium text-slate-800 dark:text-slate-100">{a.candidateName}</div>
                  <div className="text-xs text-slate-500">{a.headline || a.candidateEmail}</div>
                  <div className="mt-1"><MatchMeter score={a.matchScore} /></div>
                </div>
              </div>

              <div className="flex flex-wrap items-center gap-2">
                <StatusBadge status={a.status} />
                <select
                  className="input h-9 w-auto py-1 text-sm"
                  value=""
                  onChange={(e) => e.target.value && changeStatus(a.id, e.target.value)}
                >
                  <option value="">Set status…</option>
                  {RECRUITER_STATUSES.map((s) => <option key={s} value={s}>{humanize(s)}</option>)}
                </select>
                {a.resumes?.length > 0 && (
                  <button className="btn-secondary" onClick={() => downloadResume(a)} disabled={downloadingId === a.id} title="Download resume">
                    {downloadingId === a.id ? <Spinner className="h-4 w-4" /> : <FiFileText />} Resume
                  </button>
                )}
                <button className="btn-secondary" onClick={() => setScheduleFor(a)}>
                  <FiCalendar /> Interview
                </button>
                <Link to={`/messages/${a.candidateUserId}`} className="btn-secondary" title="Message candidate">
                  <FiMessageSquare />
                </Link>
              </div>
            </div>
          ))}
        </div>
      )}

      <ScheduleModal
        application={scheduleFor}
        onClose={() => setScheduleFor(null)}
        onScheduled={() => { setScheduleFor(null); load(); }}
      />
    </div>
  );
}

function ScheduleModal({ application, onClose, onScheduled }) {
  const { toast } = useToast();
  const { register, handleSubmit, reset, formState: { isSubmitting } } = useForm({
    defaultValues: { title: 'Interview', scheduledAt: '', durationMinutes: 60, mode: 'Video', meetingLink: '', notes: '' },
  });

  useEffect(() => { if (application) reset({ title: `Interview — ${application.candidateName}`, scheduledAt: '', durationMinutes: 60, mode: 'Video', meetingLink: '', notes: '' }); }, [application, reset]);

  const submit = async (values) => {
    try {
      await recruiterApi.scheduleInterview({
        applicationId: application.id,
        title: values.title,
        scheduledAt: new Date(values.scheduledAt).toISOString(),
        durationMinutes: Number(values.durationMinutes) || 60,
        mode: values.mode,
        location: null,
        meetingLink: values.meetingLink || null,
        interviewerUserId: null,
        notes: values.notes || null,
      });
      toast('Interview scheduled.', 'success');
      onScheduled();
    } catch (e) {
      toast(getErrorMessage(e), 'error');
    }
  };

  return (
    <Modal
      open={!!application}
      onClose={onClose}
      title="Schedule interview"
      footer={
        <>
          <button className="btn-secondary" onClick={onClose}>Cancel</button>
          <button className="btn-primary" onClick={handleSubmit(submit)} disabled={isSubmitting}>
            {isSubmitting && <Spinner className="h-4 w-4 text-white" />} Schedule
          </button>
        </>
      }
    >
      <div className="space-y-3">
        <div><label className="label">Title</label><input className="input" {...register('title', { required: true })} /></div>
        <div className="grid grid-cols-2 gap-3">
          <div><label className="label">Date &amp; time</label><input type="datetime-local" className="input" {...register('scheduledAt', { required: true })} /></div>
          <div><label className="label">Duration (min)</label><input type="number" min="15" max="480" className="input" {...register('durationMinutes')} /></div>
        </div>
        <div className="grid grid-cols-2 gap-3">
          <div>
            <label className="label">Mode</label>
            <select className="input" {...register('mode')}>{['Video', 'Onsite', 'Phone'].map((m) => <option key={m} value={m}>{m}</option>)}</select>
          </div>
          <div><label className="label">Meeting link</label><input className="input" placeholder="https://…" {...register('meetingLink')} /></div>
        </div>
        <div><label className="label">Notes</label><textarea className="input min-h-[70px]" {...register('notes')} /></div>
      </div>
    </Modal>
  );
}
