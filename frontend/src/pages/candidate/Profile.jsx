import { useCallback, useEffect, useRef, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useForm } from 'react-hook-form';
import {
  FiPlus, FiTrash2, FiUploadCloud, FiStar, FiDownload, FiAward, FiBook, FiBriefcase, FiCode, FiZap, FiCheckCircle,
  FiShield, FiAlertTriangle,
} from 'react-icons/fi';
import { candidateApi, resumesApi, aiApi, accountApi } from '../../api';
import client, { getErrorMessage } from '../../api/client';
import { useToast } from '../../contexts/ToastContext';
import { useAuth } from '../../contexts/AuthContext';
import {
  AVAILABILITY_STATUSES, GENDERS, PROFICIENCY_LEVELS, EMPLOYMENT_TYPES, humanize,
} from '../../constants';
import { PageHeader, LoadingScreen, Spinner } from '../../components/ui';
import Modal from '../../components/Modal';

export default function Profile() {
  const { toast } = useToast();
  const [profile, setProfile] = useState(null);
  const [loading, setLoading] = useState(true);

  const load = useCallback(() => {
    candidateApi.getProfile()
      .then(setProfile)
      .catch((e) => toast(getErrorMessage(e), 'error'))
      .finally(() => setLoading(false));
  }, [toast]);

  useEffect(() => { load(); }, [load]);

  if (loading) return <LoadingScreen />;
  if (!profile) return null;

  return (
    <div className="space-y-6">
      <PageHeader title="My Profile" subtitle="Keep your profile up to date for better job matches" />
      <DetailsSection profile={profile} onSaved={setProfile} />
      <SkillsSection profile={profile} reload={load} />
      <div className="grid grid-cols-1 gap-6 lg:grid-cols-2">
        <ExperienceSection profile={profile} reload={load} />
        <EducationSection profile={profile} reload={load} />
      </div>
      <ResumesSection profile={profile} reload={load} />
      <CertificatesSection profile={profile} reload={load} />
      <PrivacySection />
    </div>
  );
}

/**
 * Data-privacy controls: download a copy of all personal data (right to access) and permanently
 * delete the account (right to erasure). Deletion signs the user out and returns them home.
 */
function PrivacySection() {
  const { toast } = useToast();
  const { logout } = useAuth();
  const navigate = useNavigate();
  const [exporting, setExporting] = useState(false);
  const [confirmOpen, setConfirmOpen] = useState(false);
  const [confirmText, setConfirmText] = useState('');
  const [deleting, setDeleting] = useState(false);

  const exportData = async () => {
    setExporting(true);
    try {
      const blob = await accountApi.exportData();
      const url = URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.href = url;
      a.download = `getcareers-data-export-${new Date().toISOString().slice(0, 10)}.json`;
      a.click();
      URL.revokeObjectURL(url);
      toast('Your data export has been downloaded.', 'success');
    } catch (e) {
      toast(getErrorMessage(e), 'error');
    } finally {
      setExporting(false);
    }
  };

  const deleteAccount = async () => {
    setDeleting(true);
    try {
      await accountApi.deleteAccount();
      // Clear the local session so nothing lingers, then leave the authenticated area.
      await logout();
      toast('Your account has been deleted.', 'success');
      navigate('/', { replace: true });
    } catch (e) {
      toast(getErrorMessage(e), 'error');
      setDeleting(false);
    }
  };

  return (
    <SectionCard title="Privacy & Data" icon={FiShield}>
      <div className="space-y-5">
        <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
          <div>
            <div className="font-medium text-slate-800 dark:text-slate-100">Download your data</div>
          </div>
          <button onClick={exportData} disabled={exporting} className="btn-secondary shrink-0">
            {exporting ? <Spinner className="h-4 w-4" /> : <FiDownload />} Download my data
          </button>
        </div>

        <div className="border-t border-slate-200 pt-5 dark:border-slate-700">
          <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
            <div>
              <div className="flex items-center gap-2 font-medium text-red-600 dark:text-red-400">
                <FiAlertTriangle className="h-4 w-4" /> Delete your account
              </div>
              <p className="text-sm text-slate-500 dark:text-slate-400">
                Permanently erases your personal data and signs you out. This cannot be undone.
              </p>
            </div>
            <button
              onClick={() => { setConfirmText(''); setConfirmOpen(true); }}
              className="inline-flex shrink-0 items-center gap-1.5 rounded-xl px-4 py-2 text-sm font-semibold text-red-600 ring-1 ring-red-200 transition-all hover:bg-red-600 hover:text-white hover:ring-red-600 dark:text-red-400 dark:ring-red-900/50"
            >
              <FiTrash2 /> Delete account
            </button>
          </div>
        </div>
      </div>

      <Modal
        open={confirmOpen}
        onClose={() => !deleting && setConfirmOpen(false)}
        title="Delete your account?"
        footer={
          <>
            <button className="btn-secondary" onClick={() => setConfirmOpen(false)} disabled={deleting}>
              Cancel
            </button>
            <button
              onClick={deleteAccount}
              disabled={deleting || confirmText !== 'DELETE'}
              className="inline-flex items-center gap-1.5 rounded-xl bg-red-600 px-4 py-2 text-sm font-semibold text-white transition-colors hover:bg-red-700 disabled:cursor-not-allowed disabled:opacity-50"
            >
              {deleting && <Spinner className="h-4 w-4 text-white" />} Delete my account
            </button>
          </>
        }
      >
        <div className="space-y-3">
          <p className="text-sm text-slate-600 dark:text-slate-300">
            This permanently erases your personal data — profile, resumes and contact details — and
            signs you out. Your past applications are kept in an anonymised form for the companies
            you applied to. <strong>This cannot be undone.</strong>
          </p>
          <p className="text-sm text-slate-600 dark:text-slate-300">
            Type <strong>DELETE</strong> to confirm.
          </p>
          <input
            className="input"
            value={confirmText}
            onChange={(e) => setConfirmText(e.target.value)}
            placeholder="DELETE"
            autoComplete="off"
          />
        </div>
      </Modal>
    </SectionCard>
  );
}

function SectionCard({ title, icon: Icon, action, children }) {
  return (
    <div className="card p-5">
      <div className="mb-4 flex items-center justify-between">
        <h2 className="flex items-center gap-2.5 font-semibold text-slate-800 dark:text-slate-100">
          {Icon && (
            <span className="flex h-9 w-9 items-center justify-center rounded-xl bg-gradient-to-br from-brand-500 to-accent-500 text-white">
              <Icon className="h-5 w-5" />
            </span>
          )}
          {title}
        </h2>
        {action}
      </div>
      {children}
    </div>
  );
}

function DetailsSection({ profile, onSaved }) {
  const { toast } = useToast();
  const { register, handleSubmit, formState: { isSubmitting, isDirty } } = useForm({
    defaultValues: {
      headline: profile.headline || '', summary: profile.summary || '', location: profile.location || '',
      currentPosition: profile.currentPosition || '', yearsOfExperience: profile.yearsOfExperience || 0,
      expectedSalary: profile.expectedSalary || '', preferredCurrency: profile.preferredCurrency || 'USD',
      linkedInUrl: profile.linkedInUrl || '', portfolioUrl: profile.portfolioUrl || '',
      gender: profile.gender || 'NotSpecified', availabilityStatus: profile.availabilityStatus || 'Available',
      phoneNumber: profile.phoneNumber || '',
      dateOfBirth: profile.dateOfBirth ? profile.dateOfBirth.substring(0, 10) : '',
    },
  });

  const onSubmit = async (values) => {
    try {
      const payload = {
        ...values,
        yearsOfExperience: Number(values.yearsOfExperience) || 0,
        expectedSalary: values.expectedSalary ? Number(values.expectedSalary) : null,
        dateOfBirth: values.dateOfBirth ? new Date(values.dateOfBirth).toISOString() : null,
      };
      const updated = await candidateApi.updateProfile(payload);
      onSaved(updated);
      toast('Profile updated.', 'success');
    } catch (e) {
      toast(getErrorMessage(e), 'error');
    }
  };

  return (
    <SectionCard title="Personal Details" icon={FiBriefcase}>
      <form onSubmit={handleSubmit(onSubmit)} className="grid grid-cols-1 gap-4 sm:grid-cols-2">
        <Field label="Headline" className="sm:col-span-2"><input className="input" {...register('headline')} placeholder="e.g. Senior Full Stack Engineer" /></Field>
        <Field label="Summary" className="sm:col-span-2"><textarea className="input min-h-[90px]" {...register('summary')} /></Field>
        <Field label="Current position"><input className="input" {...register('currentPosition')} /></Field>
        <Field label="Location"><input className="input" {...register('location')} /></Field>
        <Field label="Years of experience"><input type="number" min="0" className="input" {...register('yearsOfExperience')} /></Field>
        <Field label="Phone"><input className="input" {...register('phoneNumber')} /></Field>
        <Field label="Expected salary"><input type="number" min="0" className="input" {...register('expectedSalary')} /></Field>
        <Field label="Currency"><input className="input" {...register('preferredCurrency')} /></Field>
        <Field label="LinkedIn URL"><input className="input" {...register('linkedInUrl')} /></Field>
        <Field label="Portfolio URL"><input className="input" {...register('portfolioUrl')} /></Field>
        <Field label="Date of birth"><input type="date" className="input" {...register('dateOfBirth')} /></Field>
        <Field label="Gender">
          <select className="input" {...register('gender')}>
            {GENDERS.map((g) => <option key={g} value={g}>{humanize(g)}</option>)}
          </select>
        </Field>
        <Field label="Availability">
          <select className="input" {...register('availabilityStatus')}>
            {AVAILABILITY_STATUSES.map((s) => <option key={s} value={s}>{humanize(s)}</option>)}
          </select>
        </Field>
        <div className="flex items-end sm:col-span-2">
          <button type="submit" className="btn-primary" disabled={isSubmitting || !isDirty}>
            {isSubmitting && <Spinner className="h-4 w-4 text-white" />} Save changes
          </button>
        </div>
      </form>
    </SectionCard>
  );
}

function Field({ label, className = '', children }) {
  return (
    <div className={className}>
      <label className="label">{label}</label>
      {children}
    </div>
  );
}

function SkillsSection({ profile, reload }) {
  const { toast } = useToast();
  const [open, setOpen] = useState(false);
  const { register, handleSubmit, reset, formState: { isSubmitting } } = useForm({
    defaultValues: { skillName: '', category: '', proficiencyLevel: 'Intermediate', yearsOfExperience: 1 },
  });

  const add = async (values) => {
    try {
      await candidateApi.addSkill({ ...values, yearsOfExperience: Number(values.yearsOfExperience) || 0 });
      toast('Skill added.', 'success');
      setOpen(false); reset(); reload();
    } catch (e) { toast(getErrorMessage(e), 'error'); }
  };

  const remove = async (id) => {
    try { await candidateApi.removeSkill(id); reload(); } catch (e) { toast(getErrorMessage(e), 'error'); }
  };

  return (
    <SectionCard title="Skills" icon={FiCode}
      action={<button className="btn-secondary" onClick={() => setOpen(true)}><FiPlus /> Add</button>}>
      {profile.skills?.length ? (
        <div className="flex flex-wrap gap-2">
          {profile.skills.map((s) => (
            <span key={s.id} className="badge group bg-brand-50 text-brand-700 dark:bg-brand-900/30 dark:text-brand-300">
              {s.skillName} · {humanize(s.proficiencyLevel)}
              <button onClick={() => remove(s.id)} className="ml-1.5 text-brand-400 hover:text-red-500" aria-label="Remove skill">×</button>
            </span>
          ))}
        </div>
      ) : <p className="text-sm text-slate-400">No skills added yet.</p>}

      <Modal open={open} onClose={() => setOpen(false)} title="Add skill"
        footer={<><button className="btn-secondary" onClick={() => setOpen(false)}>Cancel</button>
          <button className="btn-primary" onClick={handleSubmit(add)} disabled={isSubmitting}>{isSubmitting && <Spinner className="h-4 w-4 text-white" />} Add</button></>}>
        <div className="space-y-3">
          <Field label="Skill name"><input className="input" {...register('skillName', { required: true })} placeholder="e.g. React" /></Field>
          <Field label="Category"><input className="input" {...register('category')} placeholder="e.g. Frontend" /></Field>
          <Field label="Proficiency">
            <select className="input" {...register('proficiencyLevel')}>
              {PROFICIENCY_LEVELS.map((p) => <option key={p} value={p}>{p}</option>)}
            </select>
          </Field>
          <Field label="Years of experience"><input type="number" min="0" className="input" {...register('yearsOfExperience')} /></Field>
        </div>
      </Modal>
    </SectionCard>
  );
}

function ExperienceSection({ profile, reload }) {
  const { toast } = useToast();
  const [open, setOpen] = useState(false);
  const { register, handleSubmit, reset, formState: { isSubmitting } } = useForm({
    defaultValues: { company: '', title: '', location: '', employmentType: 'FullTime', startDate: '', endDate: '', isCurrent: false, description: '' },
  });

  const add = async (values) => {
    try {
      await candidateApi.addExperience({
        ...values,
        startDate: new Date(values.startDate).toISOString(),
        endDate: values.endDate ? new Date(values.endDate).toISOString() : null,
      });
      toast('Experience added.', 'success');
      setOpen(false); reset(); reload();
    } catch (e) { toast(getErrorMessage(e), 'error'); }
  };
  const remove = async (id) => { try { await candidateApi.removeExperience(id); reload(); } catch (e) { toast(getErrorMessage(e), 'error'); } };

  return (
    <SectionCard title="Experience" icon={FiBriefcase}
      action={<button className="btn-secondary" onClick={() => setOpen(true)}><FiPlus /> Add</button>}>
      {profile.experience?.length ? (
        <ul className="space-y-3">
          {profile.experience.map((e) => (
            <li key={e.id} className="flex items-start justify-between border-l-2 border-brand-200 pl-3">
              <div>
                <div className="font-medium text-slate-800 dark:text-slate-100">{e.title}</div>
                <div className="text-sm text-slate-500">{e.company}{e.location ? ` · ${e.location}` : ''}</div>
                <div className="text-xs text-slate-400">{fmtRange(e.startDate, e.endDate, e.isCurrent)}</div>
              </div>
              <button onClick={() => remove(e.id)} className="text-slate-400 hover:text-red-500"><FiTrash2 className="h-4 w-4" /></button>
            </li>
          ))}
        </ul>
      ) : <p className="text-sm text-slate-400">No experience added yet.</p>}

      <Modal open={open} onClose={() => setOpen(false)} title="Add experience"
        footer={<><button className="btn-secondary" onClick={() => setOpen(false)}>Cancel</button>
          <button className="btn-primary" onClick={handleSubmit(add)} disabled={isSubmitting}>{isSubmitting && <Spinner className="h-4 w-4 text-white" />} Add</button></>}>
        <div className="space-y-3">
          <Field label="Job title"><input className="input" {...register('title', { required: true })} /></Field>
          <Field label="Company"><input className="input" {...register('company', { required: true })} /></Field>
          <Field label="Location"><input className="input" {...register('location')} /></Field>
          <Field label="Employment type">
            <select className="input" {...register('employmentType')}>{EMPLOYMENT_TYPES.map((t) => <option key={t} value={t}>{humanize(t)}</option>)}</select>
          </Field>
          <div className="grid grid-cols-2 gap-3">
            <Field label="Start date"><input type="date" className="input" {...register('startDate', { required: true })} /></Field>
            <Field label="End date"><input type="date" className="input" {...register('endDate')} /></Field>
          </div>
          <label className="flex items-center gap-2 text-sm text-slate-600 dark:text-slate-300">
            <input type="checkbox" {...register('isCurrent')} /> I currently work here
          </label>
          <Field label="Description"><textarea className="input min-h-[80px]" {...register('description')} /></Field>
        </div>
      </Modal>
    </SectionCard>
  );
}

function EducationSection({ profile, reload }) {
  const { toast } = useToast();
  const [open, setOpen] = useState(false);
  const { register, handleSubmit, reset, formState: { isSubmitting } } = useForm({
    defaultValues: { institution: '', degree: '', fieldOfStudy: '', startDate: '', endDate: '', isCurrent: false, grade: '', description: '' },
  });

  const add = async (values) => {
    try {
      await candidateApi.addEducation({
        ...values,
        startDate: new Date(values.startDate).toISOString(),
        endDate: values.endDate ? new Date(values.endDate).toISOString() : null,
      });
      toast('Education added.', 'success');
      setOpen(false); reset(); reload();
    } catch (e) { toast(getErrorMessage(e), 'error'); }
  };
  const remove = async (id) => { try { await candidateApi.removeEducation(id); reload(); } catch (e) { toast(getErrorMessage(e), 'error'); } };

  return (
    <SectionCard title="Education" icon={FiBook}
      action={<button className="btn-secondary" onClick={() => setOpen(true)}><FiPlus /> Add</button>}>
      {profile.education?.length ? (
        <ul className="space-y-3">
          {profile.education.map((e) => (
            <li key={e.id} className="flex items-start justify-between border-l-2 border-brand-200 pl-3">
              <div>
                <div className="font-medium text-slate-800 dark:text-slate-100">{e.degree}</div>
                <div className="text-sm text-slate-500">{e.institution}{e.fieldOfStudy ? ` · ${e.fieldOfStudy}` : ''}</div>
                <div className="text-xs text-slate-400">{fmtRange(e.startDate, e.endDate, e.isCurrent)}</div>
              </div>
              <button onClick={() => remove(e.id)} className="text-slate-400 hover:text-red-500"><FiTrash2 className="h-4 w-4" /></button>
            </li>
          ))}
        </ul>
      ) : <p className="text-sm text-slate-400">No education added yet.</p>}

      <Modal open={open} onClose={() => setOpen(false)} title="Add education"
        footer={<><button className="btn-secondary" onClick={() => setOpen(false)}>Cancel</button>
          <button className="btn-primary" onClick={handleSubmit(add)} disabled={isSubmitting}>{isSubmitting && <Spinner className="h-4 w-4 text-white" />} Add</button></>}>
        <div className="space-y-3">
          <Field label="Degree"><input className="input" {...register('degree', { required: true })} /></Field>
          <Field label="Institution"><input className="input" {...register('institution', { required: true })} /></Field>
          <Field label="Field of study"><input className="input" {...register('fieldOfStudy')} /></Field>
          <div className="grid grid-cols-2 gap-3">
            <Field label="Start date"><input type="date" className="input" {...register('startDate', { required: true })} /></Field>
            <Field label="End date"><input type="date" className="input" {...register('endDate')} /></Field>
          </div>
          <label className="flex items-center gap-2 text-sm text-slate-600 dark:text-slate-300">
            <input type="checkbox" {...register('isCurrent')} /> Currently studying
          </label>
          <Field label="Grade"><input className="input" {...register('grade')} /></Field>
        </div>
      </Modal>
    </SectionCard>
  );
}

function ResumesSection({ profile, reload }) {
  const { toast } = useToast();
  const fileRef = useRef();
  const [uploading, setUploading] = useState(false);
  const [analyzingId, setAnalyzingId] = useState(null);
  const [analysis, setAnalysis] = useState(null);

  const analyze = async (id) => {
    setAnalyzingId(id);
    try {
      const result = await aiApi.analyzeResume(id, true);
      setAnalysis(result);
      reload();
      if (result.skillsAddedToProfile.length) {
        toast(`AI added ${result.skillsAddedToProfile.length} skill(s) to your profile.`, 'success');
      }
    } catch (e) {
      toast(getErrorMessage(e), 'error');
    } finally {
      setAnalyzingId(null);
    }
  };

  const upload = async (e) => {
    const file = e.target.files?.[0];
    if (!file) return;
    setUploading(true);
    try {
      await resumesApi.upload(file, profile.resumes.length === 0);
      toast('Resume uploaded.', 'success');
      reload();
    } catch (err) {
      toast(getErrorMessage(err), 'error');
    } finally {
      setUploading(false);
      if (fileRef.current) fileRef.current.value = '';
    }
  };

  const download = async (id, fileName) => {
    try {
      const res = await client.get(`/resumes/${id}`, { responseType: 'blob' });
      const url = URL.createObjectURL(res.data);
      const a = document.createElement('a');
      a.href = url; a.download = fileName; a.click();
      URL.revokeObjectURL(url);
    } catch (e) { toast(getErrorMessage(e), 'error'); }
  };

  const setPrimary = async (id) => { try { await resumesApi.setPrimary(id); reload(); } catch (e) { toast(getErrorMessage(e), 'error'); } };
  const remove = async (id) => { try { await resumesApi.remove(id); reload(); } catch (e) { toast(getErrorMessage(e), 'error'); } };

  return (
    <SectionCard title="Resumes" icon={FiUploadCloud}
      action={
        <>
          <input ref={fileRef} type="file" accept=".pdf,.doc,.docx" className="hidden" onChange={upload} />
          <button className="btn-secondary" onClick={() => fileRef.current?.click()} disabled={uploading}>
            {uploading ? <Spinner className="h-4 w-4" /> : <FiUploadCloud />} Upload
          </button>
        </>
      }>
      {profile.resumes?.length ? (
        <ul className="divide-y divide-slate-100 dark:divide-slate-800">
          {profile.resumes.map((r) => (
            <li key={r.id} className="flex items-center justify-between py-3">
              <div className="flex items-center gap-3">
                <span className="flex h-9 w-9 items-center justify-center rounded-lg bg-red-50 text-red-500 dark:bg-red-900/20">PDF</span>
                <div>
                  <div className="flex items-center gap-2 text-sm font-medium text-slate-800 dark:text-slate-100">
                    {r.fileName}
                    {r.isPrimary && <span className="badge bg-emerald-100 text-emerald-700 dark:bg-emerald-900/40 dark:text-emerald-300">Primary</span>}
                  </div>
                  <div className="text-xs text-slate-400">{(r.fileSize / 1024).toFixed(0)} KB · {new Date(r.uploadedAt).toLocaleDateString()}</div>
                </div>
              </div>
              <div className="flex items-center gap-3 text-slate-400">
                <button onClick={() => analyze(r.id)} className="hover:text-brand-600" title="Analyze with AI" disabled={analyzingId === r.id}>
                  {analyzingId === r.id ? <Spinner className="h-4 w-4" /> : <FiZap className="h-4 w-4" />}
                </button>
                <button onClick={() => download(r.id, r.fileName)} className="hover:text-brand-600" title="Download"><FiDownload className="h-4 w-4" /></button>
                {!r.isPrimary && <button onClick={() => setPrimary(r.id)} className="hover:text-amber-500" title="Set primary"><FiStar className="h-4 w-4" /></button>}
                <button onClick={() => remove(r.id)} className="hover:text-red-500" title="Delete"><FiTrash2 className="h-4 w-4" /></button>
              </div>
            </li>
          ))}
        </ul>
      ) : <p className="text-sm text-slate-400">Upload a PDF or Word resume (max 5 MB).</p>}

      <Modal open={!!analysis} onClose={() => setAnalysis(null)} title="AI Resume Analysis"
        footer={<button className="btn-primary" onClick={() => setAnalysis(null)}>Done</button>}>
        {analysis && (
          <div className="space-y-4">
            <div className="flex items-center gap-4">
              <div className="flex h-16 w-16 flex-col items-center justify-center rounded-xl bg-brand-50 dark:bg-brand-900/30">
                <span className="text-xl font-bold text-brand-600">{analysis.completenessScore}</span>
                <span className="text-[10px] text-slate-400">/ 100</span>
              </div>
              <div className="text-sm text-slate-600 dark:text-slate-300">
                <div className="font-medium text-slate-800 dark:text-slate-100">{analysis.fileName}</div>
                <div>{analysis.wordCount} words · {analysis.detectedSkills.length} skills · {analysis.detectedSections.length} sections</div>
              </div>
            </div>

            {analysis.skillsAddedToProfile.length > 0 && (
              <div className="rounded-lg bg-emerald-50 p-3 dark:bg-emerald-900/20">
                <div className="mb-1 flex items-center gap-1 text-sm font-medium text-emerald-700 dark:text-emerald-300"><FiCheckCircle /> Added to your profile</div>
                <div className="flex flex-wrap gap-1">
                  {analysis.skillsAddedToProfile.map((s) => <span key={s} className="badge bg-emerald-100 text-emerald-700 dark:bg-emerald-900/40 dark:text-emerald-300">{s}</span>)}
                </div>
              </div>
            )}

            <div>
              <div className="mb-1 text-sm font-medium text-slate-700 dark:text-slate-200">Detected skills</div>
              <div className="flex flex-wrap gap-1">
                {analysis.detectedSkills.map((s) => <span key={s} className="badge bg-brand-50 text-brand-700 dark:bg-brand-900/30 dark:text-brand-300">{s}</span>)}
              </div>
            </div>

            <div>
              <div className="mb-1 text-sm font-medium text-slate-700 dark:text-slate-200">Suggestions</div>
              <ul className="space-y-1 text-sm text-slate-600 dark:text-slate-300">
                {analysis.insights.map((t, i) => <li key={i} className="flex items-start gap-2"><span className="text-brand-500">•</span> {t}</li>)}
              </ul>
            </div>
          </div>
        )}
      </Modal>
    </SectionCard>
  );
}

function CertificatesSection({ profile, reload }) {
  const { toast } = useToast();
  const [open, setOpen] = useState(false);
  const { register, handleSubmit, reset, formState: { isSubmitting } } = useForm({
    defaultValues: { name: '', issuingOrganization: '', issueDate: '', expiryDate: '', credentialId: '', credentialUrl: '' },
  });

  const add = async (values) => {
    try {
      await candidateApi.addCertificate({
        ...values,
        issueDate: values.issueDate ? new Date(values.issueDate).toISOString() : null,
        expiryDate: values.expiryDate ? new Date(values.expiryDate).toISOString() : null,
      });
      toast('Certificate added.', 'success');
      setOpen(false); reset(); reload();
    } catch (e) { toast(getErrorMessage(e), 'error'); }
  };
  const remove = async (id) => { try { await candidateApi.removeCertificate(id); reload(); } catch (e) { toast(getErrorMessage(e), 'error'); } };

  return (
    <SectionCard title="Certificates" icon={FiAward}
      action={<button className="btn-secondary" onClick={() => setOpen(true)}><FiPlus /> Add</button>}>
      {profile.certificates?.length ? (
        <ul className="grid grid-cols-1 gap-3 sm:grid-cols-2">
          {profile.certificates.map((c) => (
            <li key={c.id} className="flex items-start justify-between rounded-lg border border-slate-200 p-3 dark:border-slate-700">
              <div>
                <div className="font-medium text-slate-800 dark:text-slate-100">{c.name}</div>
                <div className="text-sm text-slate-500">{c.issuingOrganization}</div>
              </div>
              <button onClick={() => remove(c.id)} className="text-slate-400 hover:text-red-500"><FiTrash2 className="h-4 w-4" /></button>
            </li>
          ))}
        </ul>
      ) : <p className="text-sm text-slate-400">No certificates added yet.</p>}

      <Modal open={open} onClose={() => setOpen(false)} title="Add certificate"
        footer={<><button className="btn-secondary" onClick={() => setOpen(false)}>Cancel</button>
          <button className="btn-primary" onClick={handleSubmit(add)} disabled={isSubmitting}>{isSubmitting && <Spinner className="h-4 w-4 text-white" />} Add</button></>}>
        <div className="space-y-3">
          <Field label="Name"><input className="input" {...register('name', { required: true })} /></Field>
          <Field label="Issuing organization"><input className="input" {...register('issuingOrganization')} /></Field>
          <div className="grid grid-cols-2 gap-3">
            <Field label="Issue date"><input type="date" className="input" {...register('issueDate')} /></Field>
            <Field label="Expiry date"><input type="date" className="input" {...register('expiryDate')} /></Field>
          </div>
          <Field label="Credential ID"><input className="input" {...register('credentialId')} /></Field>
          <Field label="Credential URL"><input className="input" {...register('credentialUrl')} /></Field>
        </div>
      </Modal>
    </SectionCard>
  );
}

const fmtRange = (start, end, isCurrent) => {
  const s = start ? new Date(start).toLocaleDateString(undefined, { year: 'numeric', month: 'short' }) : '';
  const e = isCurrent ? 'Present' : end ? new Date(end).toLocaleDateString(undefined, { year: 'numeric', month: 'short' }) : '';
  return [s, e].filter(Boolean).join(' – ');
};
