import { useEffect } from 'react';
import { useForm, useFieldArray } from 'react-hook-form';
import { useNavigate, useParams } from 'react-router-dom';
import { FiPlus, FiTrash2, FiArrowLeft } from 'react-icons/fi';
import { recruiterApi } from '../../api';
import { getErrorMessage } from '../../api/client';
import { useToast } from '../../contexts/ToastContext';
import { EMPLOYMENT_TYPES, EXPERIENCE_LEVELS, PROFICIENCY_LEVELS, humanize } from '../../constants';
import { PageHeader, Spinner, LoadingScreen } from '../../components/ui';

const emptyDefaults = {
  title: '', description: '', responsibilities: '', requirements: '',
  employmentType: 'FullTime', experienceLevel: 'Mid', location: '', isRemote: false,
  salaryMin: '', salaryMax: '', currency: 'USD', vacancies: 1, closingDate: '', status: 'Open',
  skills: [{ skillName: '', category: '', isRequired: true, minimumProficiency: 'Intermediate', weight: 5 }],
};

export default function JobForm() {
  const { id } = useParams();
  const isEdit = !!id;
  const navigate = useNavigate();
  const { toast } = useToast();

  const { register, control, handleSubmit, reset, formState: { errors, isSubmitting, isLoading } } = useForm({
    defaultValues: emptyDefaults,
  });
  const { fields, append, remove } = useFieldArray({ control, name: 'skills' });

  useEffect(() => {
    if (!isEdit) return;
    let active = true;
    recruiterApi.getJob(id)
      .then((job) => {
        if (!active) return;
        reset({
          title: job.title, description: job.description, responsibilities: job.responsibilities || '',
          requirements: job.requirements || '', employmentType: job.employmentType, experienceLevel: job.experienceLevel,
          location: job.location || '', isRemote: job.isRemote, salaryMin: job.salaryMin ?? '', salaryMax: job.salaryMax ?? '',
          currency: job.currency, vacancies: job.vacancies, closingDate: job.closingDate ? job.closingDate.substring(0, 10) : '',
          status: job.status,
          skills: job.skills?.length
            ? job.skills.map((s) => ({ skillName: s.skillName, category: '', isRequired: s.isRequired, minimumProficiency: s.minimumProficiency, weight: s.weight }))
            : emptyDefaults.skills,
        });
      })
      .catch((e) => { toast(getErrorMessage(e), 'error'); navigate('/recruiter/jobs'); });
    return () => { active = false; };
  }, [id, isEdit, reset, navigate, toast]);

  const onSubmit = async (values) => {
    const payload = {
      ...values,
      salaryMin: values.salaryMin === '' ? null : Number(values.salaryMin),
      salaryMax: values.salaryMax === '' ? null : Number(values.salaryMax),
      vacancies: Number(values.vacancies) || 1,
      departmentId: null,
      closingDate: values.closingDate ? new Date(values.closingDate).toISOString() : null,
      skills: values.skills
        .filter((s) => s.skillName.trim())
        .map((s) => ({ ...s, weight: Number(s.weight) || 5 })),
    };
    try {
      const saved = isEdit ? await recruiterApi.updateJob(id, payload) : await recruiterApi.createJob(payload);
      toast(isEdit ? 'Job updated.' : 'Job posted.', 'success');
      navigate(`/recruiter/jobs/${saved.id}/pipeline`);
    } catch (e) {
      toast(getErrorMessage(e), 'error');
    }
  };

  if (isEdit && isLoading) return <LoadingScreen />;

  return (
    <div className="mx-auto max-w-3xl">
      <button onClick={() => navigate(-1)} className="mb-4 inline-flex items-center gap-1 text-sm text-slate-500 hover:text-brand-600">
        <FiArrowLeft /> Back
      </button>
      <PageHeader title={isEdit ? 'Edit Job' : 'Post a Job'} subtitle="Describe the role and required skills" />

      <form onSubmit={handleSubmit(onSubmit)} className="space-y-6">
        <div className="card space-y-4 p-5">
          <div>
            <label className="label">Job title</label>
            <input className="input" {...register('title', { required: 'Title is required' })} />
            {errors.title && <p className="mt-1 text-xs text-red-600">{errors.title.message}</p>}
          </div>
          <div>
            <label className="label">Description</label>
            <textarea className="input min-h-[120px]" {...register('description', { required: 'Description is required' })} />
            {errors.description && <p className="mt-1 text-xs text-red-600">{errors.description.message}</p>}
          </div>
          <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
            <div><label className="label">Responsibilities</label><textarea className="input min-h-[80px]" {...register('responsibilities')} /></div>
            <div><label className="label">Requirements</label><textarea className="input min-h-[80px]" {...register('requirements')} /></div>
          </div>
        </div>

        <div className="card grid grid-cols-1 gap-4 p-5 sm:grid-cols-2">
          <div>
            <label className="label">Employment type</label>
            <select className="input" {...register('employmentType')}>{EMPLOYMENT_TYPES.map((t) => <option key={t} value={t}>{humanize(t)}</option>)}</select>
          </div>
          <div>
            <label className="label">Experience level</label>
            <select className="input" {...register('experienceLevel')}>{EXPERIENCE_LEVELS.map((t) => <option key={t} value={t}>{humanize(t)}</option>)}</select>
          </div>
          <div><label className="label">Location</label><input className="input" {...register('location')} /></div>
          <label className="flex items-center gap-2 pt-7 text-sm text-slate-600 dark:text-slate-300">
            <input type="checkbox" {...register('isRemote')} /> Remote position
          </label>
          <div><label className="label">Salary min</label><input type="number" min="0" className="input" {...register('salaryMin')} /></div>
          <div><label className="label">Salary max</label><input type="number" min="0" className="input" {...register('salaryMax')} /></div>
          <div><label className="label">Currency</label><input className="input" {...register('currency')} /></div>
          <div><label className="label">Vacancies</label><input type="number" min="1" className="input" {...register('vacancies')} /></div>
          <div><label className="label">Closing date</label><input type="date" className="input" {...register('closingDate')} /></div>
          <div>
            <label className="label">Status</label>
            <select className="input" {...register('status')}>
              {['Draft', 'Open', 'OnHold', 'Closed'].map((s) => <option key={s} value={s}>{humanize(s)}</option>)}
            </select>
          </div>
        </div>

        <div className="card p-5">
          <div className="mb-4 flex items-center justify-between">
            <h2 className="font-semibold text-slate-800 dark:text-slate-100">Required skills</h2>
            <button type="button" className="btn-secondary"
              onClick={() => append({ skillName: '', category: '', isRequired: true, minimumProficiency: 'Intermediate', weight: 5 })}>
              <FiPlus /> Add skill
            </button>
          </div>
          <div className="space-y-3">
            {fields.map((field, index) => (
              <div key={field.id} className="grid grid-cols-1 items-end gap-2 rounded-lg border border-slate-200 p-3 sm:grid-cols-12 dark:border-slate-700">
                <div className="sm:col-span-4">
                  <label className="label">Skill</label>
                  <input className="input" placeholder="e.g. C#" {...register(`skills.${index}.skillName`)} />
                </div>
                <div className="sm:col-span-3">
                  <label className="label">Min. level</label>
                  <select className="input" {...register(`skills.${index}.minimumProficiency`)}>
                    {PROFICIENCY_LEVELS.map((p) => <option key={p} value={p}>{p}</option>)}
                  </select>
                </div>
                <div className="sm:col-span-2">
                  <label className="label">Weight</label>
                  <input type="number" min="1" max="10" className="input" {...register(`skills.${index}.weight`)} />
                </div>
                <label className="flex items-center gap-2 pb-2 text-sm text-slate-600 sm:col-span-2 dark:text-slate-300">
                  <input type="checkbox" {...register(`skills.${index}.isRequired`)} /> Required
                </label>
                <button type="button" onClick={() => remove(index)} className="btn-secondary text-red-600 sm:col-span-1" aria-label="Remove skill">
                  <FiTrash2 />
                </button>
              </div>
            ))}
          </div>
        </div>

        <div className="flex justify-end gap-2">
          <button type="button" className="btn-secondary" onClick={() => navigate('/recruiter/jobs')}>Cancel</button>
          <button type="submit" className="btn-primary" disabled={isSubmitting}>
            {isSubmitting && <Spinner className="h-4 w-4 text-white" />} {isEdit ? 'Save changes' : 'Post job'}
          </button>
        </div>
      </form>
    </div>
  );
}
