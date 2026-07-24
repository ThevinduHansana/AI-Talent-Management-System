import { ROLES } from '../constants';

// Resolves the landing route for a user based on their highest-privilege role.
export function dashboardPath(user) {
  const roles = user?.roles || [];
  if (roles.includes(ROLES.ADMINISTRATOR)) return '/admin/dashboard';
  if (roles.includes(ROLES.RECRUITER)) return '/recruiter/dashboard';
  if (roles.includes(ROLES.HIRING_MANAGER)) return '/hiring-manager/dashboard';
  if (roles.includes(ROLES.CANDIDATE)) return '/candidate/dashboard';
  return '/';
}
