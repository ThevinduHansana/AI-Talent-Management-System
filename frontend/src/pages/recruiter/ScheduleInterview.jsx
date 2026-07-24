import { useEffect, useMemo, useState } from 'react';
import { useForm } from 'react-hook-form';
import { useNavigate, useSearchParams } from 'react-router-dom';
import { FiCalendar, FiExternalLink } from 'react-icons/fi';
import { interviewsApi, recruiterApi } from '../../api';
import { getErrorMessage } from '../../api/client';
import { useToast } from '../../contexts/ToastContext';
import { PageHeader, Spinner } from '../../components/ui';

/**
 * Schedules an interview against an application. The candidate and job are derived from the
 * application on the server, so this form only picks the application plus the timing details.
 */
export default function ScheduleInterview() {
  const { toast } = useToast();
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();

  const [jobs, setJobs] = useState(null);
  const [applications, setApplications] = useState([]);
  const [loadingApplications, setLoadingApplications] = useState(false);
  const [result, setResult] = useState(null);

  const {
    register, handleSubmit, watch, setValue,
    formState: { errors, isSubmitting },
  } = useForm({
    defaultValues: {
      jobId: searchParams.get('jobId') ?? '',
      applicationId: searchParams.get('applicationId') ?? '',
      title: 'Interview',
      date: '',
      time: '',
      durationMinutes: 60,
      mode: 'Video',
      location: '',
      meetingLink: '',
      notes: '',
    },
  });

  const selectedJobId = watch('jobId');
  const mode = watch('mode');

  // The browser sends local date/time; the API contract is UTC. Show the resolved UTC instant so
  // the recruiter can confirm it before submitting rather than discovering the offset afterwards.
  const date = watch('date');
  const time = watch('time');
  const utcPreview = useMemo(() => {
    if (!date || !time) return null;
    const local = new Date(`${date}T${time}`);
    return Number.isNaN(local.getTime()) ? null : local;
  }, [date, time]);

  useEffect(() => {
    recruiterApi.myJobs()
      .then((data) => setJobs(data.items ?? data))
      .catch((e) => { toast(getErrorMessage(e), 'error'); setJobs([]); });
  }, [toast]);

  useEffect(() => {
    if (!selectedJobId) { setApplications([]); return; }
    setLoadingApplications(true);
    recruiterApi.applicationsForJob(selectedJobId)
      .then((data) => setApplications(data.items ?? data))
      .catch((e) => { toast(getErrorMessage(e), 'error'); setApplications([]); })
      .finally(() => setLoadingApplications(false));
  }, [selectedJobId, toast]);

  const onSubmit = async (values) => {
    const localStart = new Date(`${values.date}T${values.time}`);
    if (Number.isNaN(localStart.getTime())) {
      toast('Please provide a valid date and time.', 'error');
      return;
    }

    try {
      const response = await interviewsApi.create({
        applicationId: values.applicationId,
        title: values.title,
        // toISOString() always yields UTC with a trailing Z, which is what the API expects.
        interviewDate: localStart.toISOString(),
        durationMinutes: Number(values.durationMinutes),
        mode: values.mode,
        location: values.mode === 'Onsite' ? values.location || null : null,
        meetingLink: values.mode === 'Video' ? values.meetingLink || null : null,
        interviewerUserId: null,
        notes: values.notes || null,
      });

      setResult(response);
      toast('Interview scheduled and calendar invitation sent', 'success');
    } catch (e) {
      toast(getErrorMessage(e), 'error');
    }
  };

  if (result) {
    return (
      <div>
        <PageHeader icon={FiCalendar} title="Interview scheduled" subtitle={result.message} />

        <div className="card mt-4 flex flex-col gap-3 p-4 sm:flex-row">
          <a href={result.googleCalendarUrl} target="_blank" rel="noopener noreferrer" className="btn-secondary">
            <FiExternalLink /> Add to Google Calendar
          </a>
        </div>

        <div className="mt-6 flex gap-3">
          <button className="btn-primary" onClick={() => navigate('/recruiter/interviews')}>
            View all interviews
          </button>
          <button className="btn-secondary" onClick={() => setResult(null)}>
            Schedule another
          </button>
        </div>
      </div>
    );
  }

  return (
    <div>
      <PageHeader
        icon={FiCalendar}
        title="Schedule Interview"
        subtitle="The candidate is emailed a calendar invitation automatically"
      />

      <form onSubmit={handleSubmit(onSubmit)} className="card max-w-2xl space-y-4 p-5">
        <div>
          <label className="label" htmlFor="jobId">Job</label>
          <select id="jobId" className="input" {...register('jobId', { required: 'Select a job.' })}>
            <option value="">Select a job…</option>
            {(jobs ?? []).map((j) => <option key={j.id} value={j.id}>{j.title}</option>)}
          </select>
          {errors.jobId && <p className="mt-1 text-sm text-red-600">{errors.jobId.message}</p>}
        </div>

        <div>
          <label className="label" htmlFor="applicationId">Candidate</label>
          <select
            id="applicationId"
            className="input"
            disabled={!selectedJobId || loadingApplications}
            {...register('applicationId', { required: 'Select a candidate.' })}
          >
            <option value="">
              {loadingApplications ? 'Loading…' : selectedJobId ? 'Select a candidate…' : 'Select a job first'}
            </option>
            {applications.map((a) => (
              <option key={a.id} value={a.id}>{a.candidateName} — {a.status}</option>
            ))}
          </select>
          {errors.applicationId && <p className="mt-1 text-sm text-red-600">{errors.applicationId.message}</p>}
        </div>

        <div>
          <label className="label" htmlFor="title">Title</label>
          <input id="title" className="input" {...register('title', { required: 'Title is required.', maxLength: 200 })} />
          {errors.title && <p className="mt-1 text-sm text-red-600">{errors.title.message}</p>}
        </div>

        <div className="grid gap-4 sm:grid-cols-3">
          <div>
            <label className="label" htmlFor="date">Date</label>
            <input id="date" type="date" className="input" {...register('date', { required: 'Date is required.' })} />
            {errors.date && <p className="mt-1 text-sm text-red-600">{errors.date.message}</p>}
          </div>
          <div>
            <label className="label" htmlFor="time">Time</label>
            <input id="time" type="time" className="input" {...register('time', { required: 'Time is required.' })} />
            {errors.time && <p className="mt-1 text-sm text-red-600">{errors.time.message}</p>}
          </div>
          <div>
            <label className="label" htmlFor="durationMinutes">Duration (minutes)</label>
            <input
              id="durationMinutes"
              type="number"
              min={15}
              max={240}
              step={15}
              className="input"
              {...register('durationMinutes', {
                required: 'Duration is required.',
                min: { value: 15, message: 'Minimum 15 minutes.' },
                max: { value: 240, message: 'Maximum 240 minutes.' },
              })}
            />
            {errors.durationMinutes && <p className="mt-1 text-sm text-red-600">{errors.durationMinutes.message}</p>}
          </div>
        </div>

        {utcPreview && (
          <p className="text-xs text-slate-500">
            Starts {utcPreview.toLocaleString()} your time — sent to the candidate as{' '}
            <strong>{utcPreview.toISOString().slice(0, 16).replace('T', ' ')} UTC</strong>.
          </p>
        )}

        <div>
          <label className="label" htmlFor="mode">Mode</label>
          <select
            id="mode"
            className="input"
            {...register('mode')}
            onChange={(e) => {
              setValue('mode', e.target.value);
              // Clear the field that no longer applies so a stale value isn't submitted.
              if (e.target.value === 'Video') setValue('location', '');
              if (e.target.value === 'Onsite') setValue('meetingLink', '');
            }}
          >
            <option value="Video">Video</option>
            <option value="Onsite">Onsite</option>
            <option value="Phone">Phone</option>
          </select>
        </div>

        {mode === 'Video' && (
          <div>
            <label className="label" htmlFor="meetingLink">Meeting link</label>
            <input
              id="meetingLink"
              className="input"
              placeholder="https://meet.google.com/abc-defg-hij"
              {...register('meetingLink', {
                pattern: { value: /^https?:\/\/.+/i, message: 'Must be an absolute http(s) URL.' },
              })}
            />
            {errors.meetingLink && <p className="mt-1 text-sm text-red-600">{errors.meetingLink.message}</p>}
          </div>
        )}

        {mode === 'Onsite' && (
          <div>
            <label className="label" htmlFor="location">Location</label>
            <input id="location" className="input" placeholder="Office address or room" {...register('location')} />
          </div>
        )}

        <div>
          <label className="label" htmlFor="notes">Notes</label>
          <textarea id="notes" rows={3} className="input" {...register('notes', { maxLength: 2000 })} />
        </div>

        <button type="submit" className="btn-primary" disabled={isSubmitting}>
          {isSubmitting ? <Spinner className="h-4 w-4" /> : <FiCalendar />}
          {isSubmitting ? 'Scheduling…' : 'Schedule interview'}
        </button>
      </form>
    </div>
  );
}
