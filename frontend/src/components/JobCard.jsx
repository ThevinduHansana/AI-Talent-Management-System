import { Link } from 'react-router-dom';
import { FiMapPin, FiBriefcase, FiClock, FiDollarSign, FiArrowRight } from 'react-icons/fi';
import { humanize } from '../constants';

const formatSalary = (min, max, currency) => {
  if (!min && !max) return null;
  const fmt = (n) => `${currency} ${Number(n).toLocaleString()}`;
  if (min && max) return `${fmt(min)} – ${fmt(max)}`;
  return fmt(min || max);
};

export default function JobCard({ job }) {
  const salary = formatSalary(job.salaryMin, job.salaryMax, job.currency);
  return (
    <Link to={`/jobs/${job.id}`} className="card card-hover group block h-full p-5">
      <div className="flex items-start justify-between gap-3">
        <div className="min-w-0">
          <h3 className="truncate font-semibold text-slate-900 transition-colors group-hover:text-brand-600 dark:text-white">{job.title}</h3>
          <p className="truncate text-sm text-slate-500">{job.organizationName}{job.departmentName ? ` · ${job.departmentName}` : ''}</p>
        </div>
        {job.isRemote && (
          <span className="badge-emerald shrink-0">Remote</span>
        )}
      </div>

      <div className="mt-4 flex flex-wrap gap-x-4 gap-y-2 text-sm text-slate-500 dark:text-slate-400">
        {job.location && <span className="flex items-center gap-1"><FiMapPin className="h-4 w-4" /> {job.location}</span>}
        <span className="flex items-center gap-1"><FiBriefcase className="h-4 w-4" /> {humanize(job.employmentType)}</span>
        <span className="flex items-center gap-1"><FiClock className="h-4 w-4" /> {humanize(job.experienceLevel)}</span>
        {salary && <span className="flex items-center gap-1"><FiDollarSign className="h-4 w-4" /> {salary}</span>}
      </div>

      <div className="mt-4 flex items-center justify-between border-t border-slate-100 pt-4 dark:border-slate-800">
        <span className="text-xs text-slate-400">
          {job.applicationCount} applicant{job.applicationCount === 1 ? '' : 's'}
        </span>
        <span className="inline-flex items-center gap-1 text-sm font-medium text-brand-600">
          View details <FiArrowRight className="transition-transform group-hover:translate-x-0.5" />
        </span>
      </div>
    </Link>
  );
}
