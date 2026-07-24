import { useCallback, useEffect, useState } from 'react';
import { useParams, Link } from 'react-router-dom';
import { useForm } from 'react-hook-form';
import {
  FiArrowLeft, FiCheck, FiX, FiStar, FiBriefcase, FiBook, FiCode, FiCalendar, FiMessageSquare,
  FiFileText, FiDownload,
} from 'react-icons/fi';
import { hiringManagerApi } from '../../api';
import { getErrorMessage } from '../../api/client';
import { useToast } from '../../contexts/ToastContext';
import { humanize } from '../../constants';
import { LoadingScreen, Spinner, StatusBadge } from '../../components/ui';

const RECOMMENDATIONS = ['StrongYes', 'Yes', 'No', 'StrongNo'];
const decided = (status) => ['Hired', 'Rejected', 'Withdrawn'].includes(status);

export default function CandidateReview() {
  const { applicationId } = useParams();
  const { toast } = useToast();
  const [detail, setDetail] = useState(null);

  const load = useCallback(() => {
    hiringManagerApi.candidateDetail(applicationId)
      .then(setDetail)
      .catch((e) => toast(getErrorMessage(e), 'error'));
  }, [applicationId, toast]);

  useEffect(() => { load(); }, [load]);

  if (!detail) return <LoadingScreen />;

  return (
    <div>
      <Link to="/hiring-manager/candidates" className="mb-4 inline-flex items-center gap-1 text-sm text-slate-500 hover:text-brand-600">
        <FiArrowLeft /> Back to review queue
      </Link>

      <div className="mb-6 flex flex-col gap-2 sm:flex-row sm:items-center sm:justify-between">
        <div>
          <h1 className="text-2xl font-bold text-slate-900 dark:text-white">{detail.candidateName}</h1>
          <p className="text-sm text-slate-500">{detail.headline || detail.candidateEmail} · applied for {detail.jobTitle}</p>
        </div>
        <StatusBadge status={detail.status} />
      </div>

      <div className="grid grid-cols-1 gap-6 lg:grid-cols-3">
        {/* Left: candidate profile */}
        <div className="space-y-6 lg:col-span-2">
          <ProfileCard detail={detail} />
          <InterviewsCard detail={detail} onSaved={load} />
        </div>

        {/* Right: evaluation + decision */}
        <div className="space-y-6">
          <EvaluationCard detail={detail} onSaved={load} />
          <DecisionCard detail={detail} onDecided={load} />
        </div>
      </div>
    </div>
  );
}

function ProfileCard({ detail }) {
  const { toast } = useToast();
  const [downloadingId, setDownloadingId] = useState(null);

  const downloadResume = async (resume) => {
    setDownloadingId(resume.id);
    try {
      const blob = await hiringManagerApi.downloadResume(detail.applicationId, resume.id);
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

  return (
    <div className="card p-5">
      <h2 className="mb-3 font-semibold text-slate-800 dark:text-slate-100">Candidate profile</h2>
      <dl className="grid grid-cols-2 gap-3 text-sm">
        <Info label="Location" value={detail.location} />
        <Info label="Current position" value={detail.currentPosition} />
        <Info label="Experience" value={`${detail.yearsOfExperience} yrs`} />
        <Info label="Email" value={detail.candidateEmail} />
      </dl>
      {detail.summary && <p className="mt-3 text-sm text-slate-600 dark:text-slate-300">{detail.summary}</p>}

      <SubList icon={FiCode} title="Skills">
        {detail.skills.length ? (
          <div className="flex flex-wrap gap-2">
            {detail.skills.map((s) => (
              <span key={s.id} className="badge bg-brand-50 text-brand-700 dark:bg-brand-900/30 dark:text-brand-300">
                {s.skillName} · {humanize(s.proficiencyLevel)}
              </span>
            ))}
          </div>
        ) : <Empty />}
      </SubList>

      <SubList icon={FiBriefcase} title="Experience">
        {detail.experience.length ? detail.experience.map((e) => (
          <div key={e.id} className="border-l-2 border-brand-200 pl-3">
            <div className="text-sm font-medium text-slate-800 dark:text-slate-100">{e.title} · {e.company}</div>
            <div className="text-xs text-slate-400">{fmt(e.startDate)} – {e.isCurrent ? 'Present' : fmt(e.endDate)}</div>
          </div>
        )) : <Empty />}
      </SubList>

      <SubList icon={FiBook} title="Education">
        {detail.education.length ? detail.education.map((e) => (
          <div key={e.id} className="border-l-2 border-brand-200 pl-3">
            <div className="text-sm font-medium text-slate-800 dark:text-slate-100">{e.degree}</div>
            <div className="text-xs text-slate-500">{e.institution}</div>
          </div>
        )) : <Empty />}
      </SubList>

      <SubList icon={FiFileText} title="Resume">
        {detail.resumes?.length ? (
          <ul className="space-y-2">
            {detail.resumes.map((r) => (
              <li key={r.id} className="flex items-center justify-between rounded-lg border border-slate-200 p-3 dark:border-slate-700">
                <div className="flex min-w-0 items-center gap-3">
                  <span className="flex h-9 w-9 shrink-0 items-center justify-center rounded-lg bg-red-50 text-xs font-semibold text-red-500 dark:bg-red-900/20">PDF</span>
                  <div className="min-w-0">
                    <div className="flex items-center gap-2 text-sm font-medium text-slate-800 dark:text-slate-100">
                      <span className="truncate">{r.fileName}</span>
                      {r.isPrimary && <span className="badge-emerald shrink-0">Primary</span>}
                    </div>
                    <div className="text-xs text-slate-400">{(r.fileSize / 1024).toFixed(0)} KB · {new Date(r.uploadedAt).toLocaleDateString()}</div>
                  </div>
                </div>
                <button onClick={() => downloadResume(r)} className="btn-secondary btn-sm shrink-0" disabled={downloadingId === r.id}>
                  {downloadingId === r.id ? <Spinner className="h-4 w-4" /> : <FiDownload />} Download
                </button>
              </li>
            ))}
          </ul>
        ) : <Empty />}
      </SubList>

      {detail.coverLetter && (
        <SubList icon={FiMessageSquare} title="Cover letter">
          <p className="whitespace-pre-line text-sm text-slate-600 dark:text-slate-300">{detail.coverLetter}</p>
        </SubList>
      )}
    </div>
  );
}

function EvaluationCard({ detail, onSaved }) {
  const { toast } = useToast();
  const ev = detail.evaluation;
  const { register, handleSubmit, watch, formState: { isSubmitting } } = useForm({
    defaultValues: {
      technicalScore: ev?.technicalScore ?? 70,
      communicationScore: ev?.communicationScore ?? 70,
      cultureFitScore: ev?.cultureFitScore ?? 70,
      comments: ev?.comments ?? '',
    },
  });
  const [t, c, cf] = [watch('technicalScore'), watch('communicationScore'), watch('cultureFitScore')].map(Number);
  const overall = Math.round((t + c + cf) / 3);

  const submit = async (values) => {
    try {
      await hiringManagerApi.submitEvaluation({
        applicationId: detail.applicationId,
        technicalScore: Number(values.technicalScore),
        communicationScore: Number(values.communicationScore),
        cultureFitScore: Number(values.cultureFitScore),
        comments: values.comments || null,
      });
      toast('Evaluation saved.', 'success');
      onSaved();
    } catch (e) {
      toast(getErrorMessage(e), 'error');
    }
  };

  return (
    <div className="card p-5">
      <div className="mb-4 flex items-center justify-between">
        <h2 className="font-semibold text-slate-800 dark:text-slate-100">Evaluation</h2>
        <div className="text-right">
          <div className="text-3xl font-bold text-gradient">{overall}</div>
          <div className="text-xs text-slate-400">Overall</div>
        </div>
      </div>
      <form onSubmit={handleSubmit(submit)} className="space-y-3">
        <ScoreInput label="Technical" reg={register('technicalScore')} value={t} />
        <ScoreInput label="Communication" reg={register('communicationScore')} value={c} />
        <ScoreInput label="Culture fit" reg={register('cultureFitScore')} value={cf} />
        <div>
          <label className="label">Comments</label>
          <textarea className="input min-h-[80px]" {...register('comments')} placeholder="Overall assessment…" />
        </div>
        <button type="submit" className="btn-primary w-full" disabled={isSubmitting}>
          {isSubmitting && <Spinner className="h-4 w-4 text-white" />} {ev ? 'Update evaluation' : 'Save evaluation'}
        </button>
      </form>
    </div>
  );
}

function DecisionCard({ detail, onDecided }) {
  const { toast } = useToast();
  const [comments, setComments] = useState('');
  const [busy, setBusy] = useState(null);
  const isDecided = decided(detail.status);
  const canDecide = !!detail.evaluation && !isDecided;

  const decide = async (approve) => {
    setBusy(approve ? 'approve' : 'reject');
    try {
      const fn = approve ? hiringManagerApi.approve : hiringManagerApi.reject;
      await fn(detail.applicationId, { comments: comments || null });
      toast(approve ? 'Candidate hired.' : 'Candidate rejected.', 'success');
      onDecided();
    } catch (e) {
      toast(getErrorMessage(e), 'error');
    } finally {
      setBusy(null);
    }
  };

  return (
    <div className="card p-5">
      <h2 className="mb-3 font-semibold text-slate-800 dark:text-slate-100">Hiring decision</h2>
      {isDecided ? (
        <div className="rounded-lg bg-slate-50 p-4 text-center dark:bg-slate-800">
          <StatusBadge status={detail.status} />
          <p className="mt-2 text-sm text-slate-500">A decision has been recorded for this candidate.</p>
        </div>
      ) : (
        <>
          {!detail.evaluation && (
            <p className="mb-3 rounded-lg bg-amber-50 p-3 text-xs text-amber-700 dark:bg-amber-900/30 dark:text-amber-300">
              Submit an evaluation before making a decision.
            </p>
          )}
          <textarea className="input mb-3 min-h-[70px]" placeholder="Decision notes (optional)…"
            value={comments} onChange={(e) => setComments(e.target.value)} />
          <div className="flex gap-2">
            <button className="btn-primary flex-1" disabled={!canDecide || busy} onClick={() => decide(true)}>
              {busy === 'approve' ? <Spinner className="h-4 w-4 text-white" /> : <FiCheck />} Approve hire
            </button>
            <button className="btn-danger flex-1" disabled={!canDecide || busy} onClick={() => decide(false)}>
              {busy === 'reject' ? <Spinner className="h-4 w-4 text-white" /> : <FiX />} Reject
            </button>
          </div>
        </>
      )}
    </div>
  );
}

function InterviewsCard({ detail, onSaved }) {
  if (!detail.interviews.length) {
    return (
      <div className="card p-5">
        <h2 className="mb-2 flex items-center gap-2 font-semibold text-slate-800 dark:text-slate-100"><FiCalendar className="text-brand-600" /> Interviews</h2>
        <p className="text-sm text-slate-400">No interviews scheduled yet.</p>
      </div>
    );
  }
  return (
    <div className="card p-5">
      <h2 className="mb-4 flex items-center gap-2 font-semibold text-slate-800 dark:text-slate-100"><FiCalendar className="text-brand-600" /> Interviews</h2>
      <div className="space-y-4">
        {detail.interviews.map((iv) => <InterviewBlock key={iv.id} interview={iv} onSaved={onSaved} />)}
      </div>
    </div>
  );
}

function InterviewBlock({ interview, onSaved }) {
  const { toast } = useToast();
  const existing = interview.feedback?.[0];
  const [open, setOpen] = useState(false);
  const { register, handleSubmit, formState: { isSubmitting } } = useForm({
    defaultValues: {
      rating: existing?.rating ?? 4,
      recommendation: existing?.recommendation ?? 'Yes',
      strengths: existing?.strengths ?? '',
      weaknesses: existing?.weaknesses ?? '',
      comments: existing?.comments ?? '',
    },
  });

  const submit = async (values) => {
    try {
      await hiringManagerApi.submitFeedback({
        interviewScheduleId: interview.id,
        rating: Number(values.rating),
        recommendation: values.recommendation,
        strengths: values.strengths || null,
        weaknesses: values.weaknesses || null,
        comments: values.comments || null,
      });
      toast('Feedback saved.', 'success');
      setOpen(false);
      onSaved();
    } catch (e) {
      toast(getErrorMessage(e), 'error');
    }
  };

  return (
    <div className="rounded-lg border border-slate-200 p-4 dark:border-slate-700">
      <div className="flex items-center justify-between">
        <div>
          <div className="text-sm font-medium text-slate-800 dark:text-slate-100">{interview.title}</div>
          <div className="text-xs text-slate-400">
            {new Date(interview.scheduledAt).toLocaleString()} · {interview.mode} · {humanize(interview.status)}
          </div>
        </div>
        <button className="btn-secondary" onClick={() => setOpen((o) => !o)}>
          {existing ? 'Edit feedback' : 'Add feedback'}
        </button>
      </div>

      {existing && !open && (
        <div className="mt-3 flex items-center gap-2 text-sm text-slate-600 dark:text-slate-300">
          <span className="flex items-center gap-1 text-amber-500">{Array.from({ length: existing.rating }).map((_, i) => <FiStar key={i} className="h-4 w-4 fill-current" />)}</span>
          <span className="badge bg-slate-100 text-slate-600 dark:bg-slate-800 dark:text-slate-300">{humanize(existing.recommendation)}</span>
        </div>
      )}

      {open && (
        <form onSubmit={handleSubmit(submit)} className="mt-3 space-y-3">
          <div className="grid grid-cols-2 gap-3">
            <div>
              <label className="label">Rating (1-5)</label>
              <input type="number" min="1" max="5" className="input" {...register('rating')} />
            </div>
            <div>
              <label className="label">Recommendation</label>
              <select className="input" {...register('recommendation')}>
                {RECOMMENDATIONS.map((r) => <option key={r} value={r}>{humanize(r)}</option>)}
              </select>
            </div>
          </div>
          <div><label className="label">Strengths</label><input className="input" {...register('strengths')} /></div>
          <div><label className="label">Weaknesses</label><input className="input" {...register('weaknesses')} /></div>
          <div><label className="label">Comments</label><textarea className="input min-h-[60px]" {...register('comments')} /></div>
          <div className="flex justify-end gap-2">
            <button type="button" className="btn-secondary" onClick={() => setOpen(false)}>Cancel</button>
            <button type="submit" className="btn-primary" disabled={isSubmitting}>
              {isSubmitting && <Spinner className="h-4 w-4 text-white" />} Save feedback
            </button>
          </div>
        </form>
      )}
    </div>
  );
}

function ScoreInput({ label, reg, value }) {
  return (
    <div>
      <div className="mb-1 flex items-center justify-between">
        <label className="label mb-0">{label}</label>
        <span className="text-sm font-semibold text-slate-600 dark:text-slate-300">{value}</span>
      </div>
      <input type="range" min="0" max="100" step="5" className="w-full accent-brand-600" {...reg} />
    </div>
  );
}

const Info = ({ label, value }) => (
  <div>
    <dt className="text-xs text-slate-400">{label}</dt>
    <dd className="text-slate-700 dark:text-slate-200">{value || '—'}</dd>
  </div>
);
const SubList = ({ icon: Icon, title, children }) => (
  <div className="mt-4 border-t border-slate-100 pt-4 dark:border-slate-800">
    <h3 className="mb-2 flex items-center gap-2 text-sm font-semibold text-slate-700 dark:text-slate-200"><Icon className="h-4 w-4 text-brand-500" /> {title}</h3>
    <div className="space-y-2">{children}</div>
  </div>
);
const Empty = () => <p className="text-sm text-slate-400">None provided.</p>;
const fmt = (d) => (d ? new Date(d).toLocaleDateString(undefined, { year: 'numeric', month: 'short' }) : '');
