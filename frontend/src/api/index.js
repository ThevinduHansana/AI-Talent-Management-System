import client from './client';

// --- Auth ---
export const authApi = {
  register: (payload) => client.post('/auth/register', payload).then((r) => r.data),
  login: (payload) => client.post('/auth/login', payload).then((r) => r.data),
  refresh: (refreshToken) => client.post('/auth/refresh', { refreshToken }).then((r) => r.data),
  logout: (refreshToken) => client.post('/auth/logout', { refreshToken }).then((r) => r.data),
  me: () => client.get('/auth/me').then((r) => r.data),
  forgotPassword: (email) => client.post('/auth/forgot-password', { email }).then((r) => r.data),
  resetPassword: (payload) => client.post('/auth/reset-password', payload).then((r) => r.data),
  // DEV ONLY — backed by an endpoint that is compiled out of Release builds. Never call from
  // production code paths; guard usage with `import.meta.env.DEV`.
  devResetToken: (email) => client.post('/auth/dev/reset-token', { email }).then((r) => r.data),
};

// --- Account (data privacy: any authenticated user) ---
export const accountApi = {
  // Returns the raw JSON blob so the caller can trigger a file download.
  exportData: () => client.get('/account/export', { responseType: 'blob' }).then((r) => r.data),
  deleteAccount: () => client.delete('/account').then((r) => r.data),
};

// --- Jobs (public) ---
export const jobsApi = {
  search: (params) => client.get('/jobs', { params }).then((r) => r.data),
  getById: (id) => client.get(`/jobs/${id}`).then((r) => r.data),
};

// --- Candidate profile ---
export const candidateApi = {
  getProfile: () => client.get('/candidate/profile').then((r) => r.data),
  updateProfile: (payload) => client.put('/candidate/profile', payload).then((r) => r.data),
  addSkill: (payload) => client.post('/candidate/skills', payload).then((r) => r.data),
  removeSkill: (id) => client.delete(`/candidate/skills/${id}`).then((r) => r.data),
  addEducation: (payload) => client.post('/candidate/education', payload).then((r) => r.data),
  updateEducation: (id, payload) => client.put(`/candidate/education/${id}`, payload).then((r) => r.data),
  removeEducation: (id) => client.delete(`/candidate/education/${id}`).then((r) => r.data),
  addExperience: (payload) => client.post('/candidate/experience', payload).then((r) => r.data),
  updateExperience: (id, payload) => client.put(`/candidate/experience/${id}`, payload).then((r) => r.data),
  removeExperience: (id) => client.delete(`/candidate/experience/${id}`).then((r) => r.data),
  addCertificate: (payload) => client.post('/candidate/certificates', payload).then((r) => r.data),
  removeCertificate: (id) => client.delete(`/candidate/certificates/${id}`).then((r) => r.data),
};

// --- Applications & saved jobs ---
export const applicationsApi = {
  apply: (payload) => client.post('/applications', payload).then((r) => r.data),
  mine: (params) => client.get('/applications', { params }).then((r) => r.data),
  getById: (id) => client.get(`/applications/${id}`).then((r) => r.data),
  withdraw: (id) => client.post(`/applications/${id}/withdraw`).then((r) => r.data),
  saved: () => client.get('/applications/saved').then((r) => r.data),
  saveJob: (jobId) => client.post(`/applications/saved/${jobId}`).then((r) => r.data),
  unsaveJob: (jobId) => client.delete(`/applications/saved/${jobId}`).then((r) => r.data),
};

// --- Recruiter ---
export const recruiterApi = {
  // Jobs
  myJobs: (params) => client.get('/recruiter/jobs', { params }).then((r) => r.data),
  getJob: (id) => client.get(`/recruiter/jobs/${id}`).then((r) => r.data),
  createJob: (payload) => client.post('/recruiter/jobs', payload).then((r) => r.data),
  updateJob: (id, payload) => client.put(`/recruiter/jobs/${id}`, payload).then((r) => r.data),
  deleteJob: (id) => client.delete(`/recruiter/jobs/${id}`).then((r) => r.data),
  // Applications
  applicationsForJob: (jobId, params) => client.get(`/recruiter/applications/by-job/${jobId}`, { params }).then((r) => r.data),
  getApplication: (id) => client.get(`/recruiter/applications/${id}`).then((r) => r.data),
  downloadResume: (applicationId, resumeId) =>
    client.get(`/recruiter/applications/${applicationId}/resumes/${resumeId}`, { responseType: 'blob' }).then((r) => r.data),
  updateApplicationStatus: (id, payload) => client.put(`/recruiter/applications/${id}/status`, payload).then((r) => r.data),
  rank: (jobId) => client.post(`/recruiter/applications/by-job/${jobId}/rank`).then((r) => r.data),
  // Interviews
  scheduleInterview: (payload) => client.post('/recruiter/interviews', payload).then((r) => r.data),
  upcomingInterviews: () => client.get('/recruiter/interviews/upcoming').then((r) => r.data),
  interviewsForJob: (jobId) => client.get(`/recruiter/interviews/by-job/${jobId}`).then((r) => r.data),
  cancelInterview: (id) => client.post(`/recruiter/interviews/${id}/cancel`).then((r) => r.data),
};

// --- Interviews (calendar integration) ---
// Shared by recruiters and candidates; the backend scopes results to the caller's role.
// Google/Outlook calendar links are generated server-side and returned on each interview —
// never build them in the browser.
export const interviewsApi = {
  create: (payload) => client.post('/interviews', payload).then((r) => r.data),
  update: (id, payload) => client.put(`/interviews/${id}`, payload).then((r) => r.data),
  cancel: (id) => client.delete(`/interviews/${id}`).then((r) => r.data),
  list: (params) => client.get('/interviews', { params }).then((r) => r.data),
  getById: (id) => client.get(`/interviews/${id}`).then((r) => r.data),
};

// --- Hiring manager ---
export const hiringManagerApi = {
  dashboard: () => client.get('/hiring-manager/dashboard').then((r) => r.data),
  reviewQueue: (params) => client.get('/hiring-manager/review-queue', { params }).then((r) => r.data),
  candidateDetail: (applicationId) => client.get(`/hiring-manager/candidates/${applicationId}`).then((r) => r.data),
  downloadResume: (applicationId, resumeId) =>
    client.get(`/hiring-manager/candidates/${applicationId}/resumes/${resumeId}`, { responseType: 'blob' }).then((r) => r.data),
  submitEvaluation: (payload) => client.post('/hiring-manager/evaluations', payload).then((r) => r.data),
  submitFeedback: (payload) => client.post('/hiring-manager/interview-feedback', payload).then((r) => r.data),
  approve: (applicationId, payload) => client.post(`/hiring-manager/candidates/${applicationId}/approve`, payload).then((r) => r.data),
  reject: (applicationId, payload) => client.post(`/hiring-manager/candidates/${applicationId}/reject`, payload).then((r) => r.data),
};

// --- Administrator ---
export const adminApi = {
  analytics: () => client.get('/admin/analytics/overview').then((r) => r.data),
  // Users
  users: (params) => client.get('/admin/users', { params }).then((r) => r.data),
  getUser: (id) => client.get(`/admin/users/${id}`).then((r) => r.data),
  createUser: (payload) => client.post('/admin/users', payload).then((r) => r.data),
  updateUser: (id, payload) => client.put(`/admin/users/${id}`, payload).then((r) => r.data),
  // Organizations
  organizations: () => client.get('/admin/organizations').then((r) => r.data),
  createOrganization: (payload) => client.post('/admin/organizations', payload).then((r) => r.data),
  updateOrganization: (id, payload) => client.put(`/admin/organizations/${id}`, payload).then((r) => r.data),
  deleteOrganization: (id) => client.delete(`/admin/organizations/${id}`).then((r) => r.data),
  addDepartment: (orgId, payload) => client.post(`/admin/organizations/${orgId}/departments`, payload).then((r) => r.data),
  removeDepartment: (orgId, deptId) => client.delete(`/admin/organizations/${orgId}/departments/${deptId}`).then((r) => r.data),
  // Roles
  roles: () => client.get('/admin/roles').then((r) => r.data),
  permissions: () => client.get('/admin/roles/permissions').then((r) => r.data),
  updateRolePermissions: (roleId, permissionIds) => client.put(`/admin/roles/${roleId}/permissions`, { permissionIds }).then((r) => r.data),
  // Audit
  auditLogs: (params) => client.get('/admin/audit-logs', { params }).then((r) => r.data),
};

// --- AI ---
export const aiApi = {
  analyzeResume: (resumeId, autoAddSkills = true) =>
    client.post(`/ai/resumes/${resumeId}/analyze`, null, { params: { autoAddSkills } }).then((r) => r.data),
  recommendations: (count = 6) => client.get('/ai/recommendations', { params: { count } }).then((r) => r.data),
  applicationFeedback: (applicationId) =>
    client.get(`/ai/applications/${applicationId}/feedback`).then((r) => r.data),
};

// --- Notifications ---
export const notificationsApi = {
  list: (params) => client.get('/notifications', { params }).then((r) => r.data),
  unreadCount: () => client.get('/notifications/unread-count').then((r) => r.data),
  markRead: (id) => client.post(`/notifications/${id}/read`).then((r) => r.data),
  markAllRead: () => client.post('/notifications/read-all').then((r) => r.data),
};

// --- Messaging ---
export const messagesApi = {
  send: (payload) => client.post('/messages', payload).then((r) => r.data),
  conversations: () => client.get('/messages/conversations').then((r) => r.data),
  unreadCount: () => client.get('/messages/unread-count').then((r) => r.data),
  thread: (otherUserId, params) => client.get(`/messages/${otherUserId}`, { params }).then((r) => r.data),
};

// --- Resumes ---
export const resumesApi = {
  upload: (file, makePrimary) => {
    const form = new FormData();
    form.append('file', file);
    form.append('makePrimary', makePrimary ? 'true' : 'false');
    return client.post('/resumes', form, { headers: { 'Content-Type': 'multipart/form-data' } }).then((r) => r.data);
  },
  setPrimary: (id) => client.post(`/resumes/${id}/primary`).then((r) => r.data),
  remove: (id) => client.delete(`/resumes/${id}`).then((r) => r.data),
};
