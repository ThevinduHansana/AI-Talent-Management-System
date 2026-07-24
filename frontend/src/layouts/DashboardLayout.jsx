import { useState } from 'react';
import { Link, NavLink, Outlet, useLocation, useNavigate } from 'react-router-dom';
import { AnimatePresence, motion } from 'framer-motion';
import {
  FiBriefcase, FiGrid, FiUser, FiFileText, FiBookmark, FiSearch, FiLogOut, FiMenu, FiX, FiChevronRight,
  FiLayers, FiCalendar, FiUserCheck, FiUsers, FiHome, FiShield, FiActivity, FiMessageSquare, FiBell, FiZap,
  FiChevronsLeft,
} from 'react-icons/fi';
import { useAuth } from '../contexts/AuthContext';
import { ROLES } from '../constants';
import ThemeToggle from '../components/ThemeToggle';
import NotificationBell from '../components/NotificationBell';
import Logo from '../components/Logo';

const candidateNav = [
  { to: '/candidate/dashboard', label: 'Dashboard', icon: FiGrid },
  { to: '/candidate/profile', label: 'My Profile', icon: FiUser },
  { to: '/candidate/applications', label: 'Applications', icon: FiFileText },
  { to: '/candidate/interviews', label: 'Interviews', icon: FiCalendar },
  { to: '/candidate/recommendations', label: 'Recommended', icon: FiZap },
  { to: '/candidate/saved', label: 'Saved Jobs', icon: FiBookmark },
  { to: '/jobs', label: 'Browse Jobs', icon: FiSearch },
  { to: '/messages', label: 'Messages', icon: FiMessageSquare },
  { to: '/notifications', label: 'Notifications', icon: FiBell },
];

const recruiterNav = [
  { to: '/recruiter/dashboard', label: 'Dashboard', icon: FiGrid },
  { to: '/recruiter/jobs', label: 'My Jobs', icon: FiLayers },
  { to: '/recruiter/jobs/new', label: 'Post a Job', icon: FiBriefcase },
  { to: '/recruiter/interviews', label: 'Interviews', icon: FiCalendar },
  { to: '/recruiter/interviews/schedule', label: 'Schedule Interview', icon: FiCalendar },
  { to: '/messages', label: 'Messages', icon: FiMessageSquare },
];

const hiringManagerNav = [
  { to: '/hiring-manager/dashboard', label: 'Dashboard', icon: FiGrid },
  { to: '/hiring-manager/candidates', label: 'Review Candidates', icon: FiUserCheck },
];

const adminNav = [
  { to: '/admin/dashboard', label: 'Analytics', icon: FiActivity },
  { to: '/admin/users', label: 'Users', icon: FiUsers },
  { to: '/admin/organizations', label: 'Organizations', icon: FiHome },
  { to: '/admin/roles', label: 'Roles & Permissions', icon: FiShield },
  { to: '/admin/audit-logs', label: 'Audit Logs', icon: FiFileText },
];

const COLLAPSE_KEY = 'gc_sidebar_collapsed';

export default function DashboardLayout() {
  const { user, logout } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();
  const [mobileOpen, setMobileOpen] = useState(false);
  const [collapsed, setCollapsed] = useState(() => {
    try { return localStorage.getItem(COLLAPSE_KEY) === '1'; } catch { return false; }
  });

  const toggleCollapsed = () => {
    setCollapsed((c) => {
      const next = !c;
      try { localStorage.setItem(COLLAPSE_KEY, next ? '1' : '0'); } catch { /* ignore */ }
      return next;
    });
  };

  const handleLogout = async () => {
    await logout();
    navigate('/login');
  };

  const crumbs = location.pathname.split('/').filter(Boolean);
  const roles = user?.roles || [];
  const nav = roles.includes(ROLES.ADMINISTRATOR)
    ? adminNav
    : roles.includes(ROLES.RECRUITER)
      ? recruiterNav
      : roles.includes(ROLES.HIRING_MANAGER)
        ? hiringManagerNav
        : candidateNav;

  const roleLabel = roles[0] || 'Member';

  const SidebarContent = ({ mini = false }) => (
    <>
      <div className={`flex items-center px-2 py-4 ${mini ? 'justify-center' : 'justify-between'}`}>
        <Link to="/" title="GetCareers">{mini ? <Logo size="sm" iconOnly /> : <Logo size="sm" />}</Link>
      </div>

      <nav className="mt-2 flex flex-1 flex-col gap-1">
        {nav.map(({ to, label, icon: Icon }) => (
          <NavLink
            key={to}
            to={to}
            end
            onClick={() => setMobileOpen(false)}
            title={mini ? label : undefined}
            className={({ isActive }) =>
              `group relative flex items-center gap-3 rounded-xl px-3 py-2.5 text-sm font-medium transition-all ${
                mini ? 'justify-center' : ''
              } ${
                isActive
                  ? 'bg-gradient-to-r from-brand-50 to-accent-50 text-brand-700 dark:from-brand-900/40 dark:to-accent-900/20 dark:text-brand-300'
                  : 'text-slate-600 hover:bg-slate-100 hover:text-slate-900 dark:text-slate-300 dark:hover:bg-slate-800'
              }`
            }
          >
            {({ isActive }) => (
              <>
                {isActive && (
                  <span className="absolute left-0 top-1/2 h-6 w-1 -translate-y-1/2 rounded-r-full bg-brand-600" />
                )}
                <Icon className="h-5 w-5 shrink-0" />
                {!mini && <span className="truncate">{label}</span>}
              </>
            )}
          </NavLink>
        ))}
      </nav>

      <button
        onClick={handleLogout}
        title={mini ? 'Sign out' : undefined}
        className={`mt-2 flex items-center gap-3 rounded-xl px-3 py-2.5 text-sm font-medium text-slate-600 transition-colors hover:bg-red-50 hover:text-red-600 dark:text-slate-300 dark:hover:bg-red-900/20 dark:hover:text-red-400 ${
          mini ? 'justify-center' : ''
        }`}
      >
        <FiLogOut className="h-5 w-5 shrink-0" />
        {!mini && 'Sign out'}
      </button>
    </>
  );

  return (
    <div className="flex min-h-full">
      {/* Desktop sidebar */}
      <motion.aside
        animate={{ width: collapsed ? 76 : 256 }}
        transition={{ duration: 0.25, ease: 'easeInOut' }}
        className="sticky top-0 hidden h-screen shrink-0 flex-col border-r border-slate-200 bg-white p-3 dark:border-slate-800 dark:bg-slate-900 lg:flex"
      >
        <SidebarContent mini={collapsed} />
        <button
          onClick={toggleCollapsed}
          className="mt-3 flex items-center justify-center gap-2 rounded-xl border border-slate-200 py-2 text-xs font-medium text-slate-500 transition-colors hover:bg-slate-100 dark:border-slate-800 dark:hover:bg-slate-800"
          aria-label={collapsed ? 'Expand sidebar' : 'Collapse sidebar'}
        >
          <FiChevronsLeft className={`h-4 w-4 transition-transform ${collapsed ? 'rotate-180' : ''}`} />
          {!collapsed && 'Collapse'}
        </button>
      </motion.aside>

      {/* Mobile sidebar */}
      <AnimatePresence>
        {mobileOpen && (
          <div className="fixed inset-0 z-50 lg:hidden">
            <motion.div
              initial={{ opacity: 0 }} animate={{ opacity: 1 }} exit={{ opacity: 0 }}
              className="absolute inset-0 bg-slate-900/50 backdrop-blur-sm" onClick={() => setMobileOpen(false)}
            />
            <motion.aside
              initial={{ x: -280 }} animate={{ x: 0 }} exit={{ x: -280 }}
              transition={{ type: 'spring', stiffness: 320, damping: 32 }}
              className="absolute left-0 top-0 flex h-full w-64 flex-col border-r border-slate-200 bg-white p-3 dark:border-slate-800 dark:bg-slate-900"
            >
              <SidebarContent />
            </motion.aside>
          </div>
        )}
      </AnimatePresence>

      <div className="flex min-w-0 flex-1 flex-col">
        {/* Top nav */}
        <header className="sticky top-0 z-30 flex h-16 items-center justify-between border-b border-slate-200/70 bg-white/85 px-4 backdrop-blur-xl dark:border-slate-800/70 dark:bg-slate-900/85">
          <div className="flex items-center gap-3">
            <button className="rounded-lg p-1.5 text-slate-600 hover:bg-slate-100 dark:text-slate-300 dark:hover:bg-slate-800 lg:hidden" onClick={() => setMobileOpen((o) => !o)} aria-label="Toggle menu">
              {mobileOpen ? <FiX className="h-6 w-6" /> : <FiMenu className="h-6 w-6" />}
            </button>
            <nav className="hidden items-center gap-1 text-sm text-slate-400 sm:flex" aria-label="Breadcrumb">
              {crumbs.map((c, i) => (
                <span key={i} className="flex items-center gap-1">
                  {i > 0 && <FiChevronRight className="h-3.5 w-3.5" />}
                  <span className={i === crumbs.length - 1 ? 'font-semibold capitalize text-slate-700 dark:text-slate-200' : 'capitalize'}>
                    {c.replace(/-/g, ' ')}
                  </span>
                </span>
              ))}
            </nav>
          </div>
          <div className="flex items-center gap-2 sm:gap-3">
            <NotificationBell />
            <ThemeToggle />
            <div className="ml-1 flex items-center gap-2.5 rounded-full border border-slate-200 bg-white py-1 pl-1 pr-3 dark:border-slate-800 dark:bg-slate-900">
              <div className="flex h-8 w-8 items-center justify-center rounded-full bg-gradient-to-br from-brand-600 to-accent-500 text-xs font-semibold text-white">
                {user?.firstName?.[0]}{user?.lastName?.[0]}
              </div>
              <div className="hidden text-sm leading-tight sm:block">
                <div className="font-semibold text-slate-700 dark:text-slate-200">{user?.firstName} {user?.lastName}</div>
                <div className="text-[11px] text-slate-400">{roleLabel}</div>
              </div>
            </div>
          </div>
        </header>

        <main className="mx-auto w-full max-w-6xl flex-1 p-4 sm:p-6">
          <Outlet />
        </main>
      </div>
    </div>
  );
}
