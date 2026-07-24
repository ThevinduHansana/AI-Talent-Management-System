// Central place for role names, enum option lists and storage keys used across the app.

export const ROLES = {
  CANDIDATE: 'Candidate',
  RECRUITER: 'Recruiter',
  HIRING_MANAGER: 'HiringManager',
  ADMINISTRATOR: 'Administrator',
};

export const STORAGE_KEYS = {
  ACCESS_TOKEN: 'rp_access_token',
  REFRESH_TOKEN: 'rp_refresh_token',
  USER: 'rp_user',
  THEME: 'rp_theme',
};

export const EMPLOYMENT_TYPES = ['FullTime', 'PartTime', 'Contract', 'Internship', 'Temporary', 'Freelance'];
export const EXPERIENCE_LEVELS = ['Entry', 'Junior', 'Mid', 'Senior', 'Lead', 'Executive'];
export const PROFICIENCY_LEVELS = ['Beginner', 'Intermediate', 'Advanced', 'Expert'];
export const AVAILABILITY_STATUSES = ['Available', 'OpenToOffers', 'Employed', 'NotAvailable'];
export const GENDERS = ['NotSpecified', 'Male', 'Female', 'Other'];

// Colors for application status badges.
export const APPLICATION_STATUS_STYLES = {
  Applied: 'bg-blue-100 text-blue-700 dark:bg-blue-900/40 dark:text-blue-300',
  UnderReview: 'bg-amber-100 text-amber-700 dark:bg-amber-900/40 dark:text-amber-300',
  Shortlisted: 'bg-violet-100 text-violet-700 dark:bg-violet-900/40 dark:text-violet-300',
  InterviewScheduled: 'bg-indigo-100 text-indigo-700 dark:bg-indigo-900/40 dark:text-indigo-300',
  Interviewed: 'bg-cyan-100 text-cyan-700 dark:bg-cyan-900/40 dark:text-cyan-300',
  Offered: 'bg-emerald-100 text-emerald-700 dark:bg-emerald-900/40 dark:text-emerald-300',
  Hired: 'bg-green-100 text-green-700 dark:bg-green-900/40 dark:text-green-300',
  Rejected: 'bg-red-100 text-red-700 dark:bg-red-900/40 dark:text-red-300',
  Withdrawn: 'bg-slate-200 text-slate-600 dark:bg-slate-700 dark:text-slate-300',
};

export const humanize = (value) =>
  typeof value === 'string' ? value.replace(/([a-z])([A-Z])/g, '$1 $2') : value;
