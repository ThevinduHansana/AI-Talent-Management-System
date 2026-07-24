import { useEffect, useState } from 'react';
import { useParams, useNavigate, Link } from 'react-router-dom';
import { motion } from 'framer-motion';
import {
  FiMapPin, FiBriefcase, FiClock, FiDollarSign, FiArrowLeft, FiBookmark, FiUsers, FiCheckCircle,
} from 'react-icons/fi';
import { jobsApi, applicationsApi } from '../api';
import { getErrorMessage } from '../api/client';
import { useAuth } from '../contexts/AuthContext';
import { useToast } from '../contexts/ToastContext';
import { ROLES, humanize } from '../constants';
import { LoadingScreen, Spinner } from '../components/ui';
import Modal from '../components/Modal';

export default function JobDetails() {
  const { id } = useParams();
  const navigate = useNavigate();
  const { toast } = useToast();
  const { isAuthenticated, hasRole } = useAuth();

  const [job, setJob] = useState(null);
  const [loading, setLoading] = useState(true);
  const [applyOpen, setApplyOpen] = useState(false);
  const [coverLetter, setCoverLetter] = useState('');
  const [submitting, setSubmitting] = useState(false);
  const [saving, setSaving] = useState(false);

  const isCandidate = isAuthenticated && hasRole(ROLES.CANDIDATE);

  useEffect(() => {
    let active = true;
    setLoading(true);
    jobsApi.getById(id)
      .then((res) => { if (active) setJob(res); })
      .catch((e) => { toast(getErrorMessage(e), 'error'); navigate('/jobs'); })
      .finally(() => { if (active) setLoading(false); });
    return () => { active = false; };
  }, [id, navigate, toast]);

  const apply = async () => {
    setSubmitting(true);
    try {
      await applicationsApi.apply({ jobId: id, resumeId: null, coverLetter: coverLetter || null });
      toast('Application submitted!', 'success');
      setApplyOpen(false);
      navigate('/candidate/applications');
    } catch (e) {
      toast(getErrorMessage(e, 'Could not submit application.'), 'error');
    } finally {
      setSubmitting(false);
    }
  };

  const save = async () => {
    setSaving(true);
    try {
      await applicationsApi.saveJob(id);
      toast('Job saved.', 'success');
    } catch (e) {
      toast(getErrorMessage(e), 'error');
    } finally {
      setSaving(false);
    }
  };

  if (loading) return <LoadingScreen />;
  if (!job) return null;

  const salary = (job.salaryMin || job.salaryMax)
    ? `${job.currency} ${Number(job.salaryMin || job.salaryMax).toLocaleString()}${job.salaryMax ? ` – ${job.currency} ${Number(job.salaryMax).toLocaleString()}` : ''}`
    : null;

  // Action buttons — reused in the hero and the sticky sidebar.
  const Actions = ({ variant = 'light' }) => {
    if (!isAuthenticated) {
      return <Link to="/login" className={variant === 'light' ? 'btn-lg inline-flex items-center gap-2 rounded-xl bg-white px-6 font-semibold text-brand-700 shadow-lg transition hover:-translate-y-0.5' : 'btn-primary w-full'}>Sign in to apply</Link>;
    }
    if (!isCandidate) return null;
    if (variant === 'light') {
      return (
        <div className="flex flex-wrap gap-2">
          <button className="btn-lg inline-flex items-center gap-2 rounded-xl bg-white px-6 font-semibold text-brand-700 shadow-lg transition hover:-translate-y-0.5" onClick={() => setApplyOpen(true)}>Apply now</button>
          <button className="btn-lg inline-flex items-center gap-2 rounded-xl border border-white/40 px-5 font-semibold text-white transition hover:bg-white/10" onClick={save} disabled={saving}>
            {saving ? <Spinner className="h-4 w-4 text-white" /> : <FiBookmark />} Save
          </button>
        </div>
      );
    }
    return (
      <div className="flex flex-col gap-2">
        <button className="btn-primary w-full btn-lg" onClick={() => setApplyOpen(true)}>Apply now</button>
        <button className="btn-secondary w-full" onClick={save} disabled={saving}>
          {saving ? <Spinner className="h-4 w-4" /> : <FiBookmark />} Save job
        </button>
      </div>
    );
  };

  const facts = [
    { icon: FiBriefcase, label: 'Employment', value: humanize(job.employmentType) },
    { icon: FiClock, label: 'Experience', value: humanize(job.experienceLevel) },
    { icon: FiMapPin, label: 'Location', value: `${job.location || 'Not specified'}${job.isRemote ? ' · Remote' : ''}` },
    ...(salary ? [{ icon: FiDollarSign, label: 'Salary', value: salary }] : []),
    ...(job.applicationCount != null ? [{ icon: FiUsers, label: 'Applicants', value: `${job.applicationCount}` }] : []),
  ];

  return (
    <div className="mx-auto max-w-5xl px-4 py-8">
      <Link to="/jobs" className="mb-4 inline-flex items-center gap-1 text-sm text-slate-500 transition-colors hover:text-brand-600">
        <FiArrowLeft /> Back to jobs
      </Link>

      {/* ===== Gradient hero header ===== */}
      <motion.div
        initial={{ opacity: 0, y: 16 }}
        animate={{ opacity: 1, y: 0 }}
        transition={{ duration: 0.4 }}
        className="relative overflow-hidden rounded-3xl bg-gradient-to-br from-brand-600 via-brand-500 to-accent-500 p-6 text-white shadow-[var(--shadow-lift)] sm:p-8"
      >
        <div className="pointer-events-none absolute -right-16 -top-16 h-56 w-56 rounded-full bg-white/10 blur-2xl" />
        <div className="pointer-events-none absolute -bottom-20 -left-10 h-56 w-56 rounded-full bg-white/10 blur-2xl" />

        <div className="relative flex flex-col gap-6 lg:flex-row lg:items-start lg:justify-between">
          <div className="flex items-start gap-4">
            <span className="flex h-14 w-14 shrink-0 items-center justify-center rounded-2xl bg-white/20 text-2xl font-bold backdrop-blur">
              {job.organizationName?.[0] || 'J'}
            </span>
            <div>
              <h1 className="text-2xl font-bold leading-tight sm:text-3xl">{job.title}</h1>
              <p className="mt-1 text-white/85">{job.organizationName}{job.departmentName ? ` · ${job.departmentName}` : ''}</p>
              <div className="mt-4 flex flex-wrap gap-2">
                {job.isRemote && <span className="rounded-full bg-white/20 px-3 py-1 text-xs font-semibold backdrop-blur">Remote</span>}
                <span className="rounded-full bg-white/20 px-3 py-1 text-xs font-semibold backdrop-blur">{humanize(job.employmentType)}</span>
                <span className="rounded-full bg-white/20 px-3 py-1 text-xs font-semibold backdrop-blur">{humanize(job.experienceLevel)}</span>
                {salary && <span className="rounded-full bg-white/20 px-3 py-1 text-xs font-semibold backdrop-blur">{salary}</span>}
              </div>
            </div>
          </div>
          <div className="shrink-0"><Actions variant="light" /></div>
        </div>
      </motion.div>

      {/* ===== Body: content + sticky sidebar ===== */}
      <div className="mt-6 grid gap-6 lg:grid-cols-[1fr_20rem]">
        <motion.div
          initial={{ opacity: 0, y: 16 }}
          animate={{ opacity: 1, y: 0 }}
          transition={{ duration: 0.4, delay: 0.1 }}
          className="card space-y-6 p-6 sm:p-8"
        >
          <Section title="Job description" body={job.description} />
          <Section title="Responsibilities" body={job.responsibilities} />
          <Section title="Requirements" body={job.requirements} />
        </motion.div>

        <motion.aside
          initial={{ opacity: 0, y: 16 }}
          animate={{ opacity: 1, y: 0 }}
          transition={{ duration: 0.4, delay: 0.15 }}
          className="space-y-4 lg:sticky lg:top-24 lg:self-start"
        >
          {/* apply CTA */}
          <div className="card p-5">
            <Actions variant="dark" />
          </div>

          {/* overview facts */}
          <div className="card p-5">
            <h3 className="mb-3 font-semibold text-slate-800 dark:text-slate-100">Job overview</h3>
            <dl className="space-y-3">
              {facts.map((f) => (
                <div key={f.label} className="flex items-start gap-3">
                  <span className="flex h-9 w-9 shrink-0 items-center justify-center rounded-lg bg-brand-50 text-brand-600 dark:bg-brand-900/30">
                    <f.icon className="h-4 w-4" />
                  </span>
                  <div>
                    <dt className="text-xs text-slate-400">{f.label}</dt>
                    <dd className="text-sm font-medium text-slate-700 dark:text-slate-200">{f.value}</dd>
                  </div>
                </div>
              ))}
            </dl>
          </div>

          {/* required skills */}
          {job.skills?.length > 0 && (
            <div className="card p-5">
              <h3 className="mb-3 font-semibold text-slate-800 dark:text-slate-100">Skills</h3>
              <div className="flex flex-wrap gap-2">
                {job.skills.map((s) => (
                  <span key={s.skillId} className={s.isRequired ? 'badge-brand' : 'badge bg-slate-100 text-slate-600 dark:bg-slate-800 dark:text-slate-300'}>
                    {s.isRequired && <FiCheckCircle className="h-3 w-3" />} {s.skillName}
                  </span>
                ))}
              </div>
            </div>
          )}
        </motion.aside>
      </div>

      <Modal
        open={applyOpen}
        onClose={() => setApplyOpen(false)}
        title={`Apply to ${job.title}`}
        footer={
          <>
            <button className="btn-secondary" onClick={() => setApplyOpen(false)}>Cancel</button>
            <button className="btn-primary" onClick={apply} disabled={submitting}>
              {submitting && <Spinner className="h-4 w-4 text-white" />} Submit application
            </button>
          </>
        }
      >
        <label className="label" htmlFor="coverLetter">Cover letter (optional)</label>
        <textarea
          id="coverLetter"
          className="input min-h-[140px]"
          placeholder="Tell the recruiter why you're a great fit…"
          value={coverLetter}
          onChange={(e) => setCoverLetter(e.target.value)}
        />
      </Modal>
    </div>
  );
}

function Section({ title, body }) {
  if (!body) return null;
  return (
    <div>
      <h2 className="mb-2 text-lg font-semibold text-slate-900 dark:text-white">{title}</h2>
      <p className="whitespace-pre-line text-sm leading-relaxed text-slate-600 dark:text-slate-300">{body}</p>
    </div>
  );
}
