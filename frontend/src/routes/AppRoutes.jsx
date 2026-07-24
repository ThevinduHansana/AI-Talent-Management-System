import { Routes, Route, Navigate } from 'react-router-dom';
import PublicLayout from '../layouts/PublicLayout';
import DashboardLayout from '../layouts/DashboardLayout';
import ProtectedRoute from '../components/ProtectedRoute';
import { ROLES } from '../constants';

import Home from '../pages/Home';
import Jobs from '../pages/Jobs';
import JobDetails from '../pages/JobDetails';
import Login from '../pages/Login';
import Register from '../pages/Register';
import ForgotPassword from '../pages/ForgotPassword';
import ResetPassword from '../pages/ResetPassword';
import { About, Contact, NotFound } from '../pages/Misc';
import Messages from '../pages/Messages';
import Notifications from '../pages/Notifications';

import Dashboard from '../pages/candidate/Dashboard';
import Profile from '../pages/candidate/Profile';
import Applications from '../pages/candidate/Applications';
import UpcomingInterviews from '../pages/candidate/UpcomingInterviews';
import SavedJobs from '../pages/candidate/SavedJobs';
import Recommendations from '../pages/candidate/Recommendations';

import RecruiterDashboard from '../pages/recruiter/Dashboard';
import RecruiterJobs from '../pages/recruiter/Jobs';
import JobForm from '../pages/recruiter/JobForm';
import JobPipeline from '../pages/recruiter/JobPipeline';
import Interviews from '../pages/recruiter/Interviews';
import ScheduleInterview from '../pages/recruiter/ScheduleInterview';

import HiringManagerDashboard from '../pages/hiringmanager/Dashboard';
import ReviewQueue from '../pages/hiringmanager/ReviewQueue';
import CandidateReview from '../pages/hiringmanager/CandidateReview';

import AnalyticsDashboard from '../pages/admin/AnalyticsDashboard';
import AdminUsers from '../pages/admin/Users';
import AdminOrganizations from '../pages/admin/Organizations';
import AdminRoles from '../pages/admin/Roles';
import AuditLogs from '../pages/admin/AuditLogs';

export default function AppRoutes() {
  return (
    <Routes>
      {/* Public site */}
      <Route element={<PublicLayout />}>
        <Route index element={<Home />} />
        <Route path="jobs" element={<Jobs />} />
        <Route path="jobs/:id" element={<JobDetails />} />
        <Route path="about" element={<About />} />
        <Route path="contact" element={<Contact />} />
      </Route>

      {/* Auth (standalone) */}
      <Route path="/login" element={<Login />} />
      <Route path="/register" element={<Register />} />
      <Route path="/forgot-password" element={<ForgotPassword />} />
      <Route path="/reset-password" element={<ResetPassword />} />

      {/* Shared authenticated area: messaging & notifications (any role) */}
      <Route
        element={
          <ProtectedRoute>
            <DashboardLayout />
          </ProtectedRoute>
        }
      >
        <Route path="/messages" element={<Messages />} />
        <Route path="/messages/:userId" element={<Messages />} />
        <Route path="/notifications" element={<Notifications />} />
      </Route>

      {/* Candidate area */}
      <Route
        path="/candidate"
        element={
          <ProtectedRoute roles={[ROLES.CANDIDATE]}>
            <DashboardLayout />
          </ProtectedRoute>
        }
      >
        <Route index element={<Navigate to="dashboard" replace />} />
        <Route path="dashboard" element={<Dashboard />} />
        <Route path="profile" element={<Profile />} />
        <Route path="applications" element={<Applications />} />
        <Route path="interviews" element={<UpcomingInterviews />} />
        <Route path="recommendations" element={<Recommendations />} />
        <Route path="saved" element={<SavedJobs />} />
      </Route>

      {/* Recruiter area */}
      <Route
        path="/recruiter"
        element={
          <ProtectedRoute roles={[ROLES.RECRUITER]}>
            <DashboardLayout />
          </ProtectedRoute>
        }
      >
        <Route index element={<Navigate to="dashboard" replace />} />
        <Route path="dashboard" element={<RecruiterDashboard />} />
        <Route path="jobs" element={<RecruiterJobs />} />
        <Route path="jobs/new" element={<JobForm />} />
        <Route path="jobs/:id/edit" element={<JobForm />} />
        <Route path="jobs/:id/pipeline" element={<JobPipeline />} />
        <Route path="interviews" element={<Interviews />} />
        <Route path="interviews/schedule" element={<ScheduleInterview />} />
      </Route>

      {/* Hiring-manager area */}
      <Route
        path="/hiring-manager"
        element={
          <ProtectedRoute roles={[ROLES.HIRING_MANAGER]}>
            <DashboardLayout />
          </ProtectedRoute>
        }
      >
        <Route index element={<Navigate to="dashboard" replace />} />
        <Route path="dashboard" element={<HiringManagerDashboard />} />
        <Route path="candidates" element={<ReviewQueue />} />
        <Route path="candidates/:applicationId" element={<CandidateReview />} />
      </Route>

      {/* Administrator area */}
      <Route
        path="/admin"
        element={
          <ProtectedRoute roles={[ROLES.ADMINISTRATOR]}>
            <DashboardLayout />
          </ProtectedRoute>
        }
      >
        <Route index element={<Navigate to="dashboard" replace />} />
        <Route path="dashboard" element={<AnalyticsDashboard />} />
        <Route path="users" element={<AdminUsers />} />
        <Route path="organizations" element={<AdminOrganizations />} />
        <Route path="roles" element={<AdminRoles />} />
        <Route path="audit-logs" element={<AuditLogs />} />
      </Route>

      <Route path="*" element={<NotFound />} />
    </Routes>
  );
}
