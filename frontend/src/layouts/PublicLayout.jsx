import { Link, NavLink, Outlet } from 'react-router-dom';
import { FiArrowRight, FiLinkedin, FiTwitter, FiGithub, FiMail } from 'react-icons/fi';
import { useAuth } from '../contexts/AuthContext';
import { dashboardPath } from '../utils/roles';
import ThemeToggle from '../components/ThemeToggle';
import Logo from '../components/Logo';

const navLinks = [
  { to: '/', label: 'Home', end: true },
  { to: '/jobs', label: 'Jobs' },
  { to: '/about', label: 'About' },
  { to: '/contact', label: 'Contact' },
];

const footerCols = [
  { title: 'Platform', links: [['Browse Jobs', '/jobs'], ['Create account', '/register'], ['Sign in', '/login']] },
  { title: 'Company', links: [['About us', '/about'], ['Contact', '/contact'], ['Careers', '/jobs']] },
  { title: 'Resources', links: [['Help Center', '/contact'], ['Privacy', '/about'], ['Terms', '/about']] },
];

export default function PublicLayout() {
  const { isAuthenticated, user } = useAuth();

  return (
    <div className="flex min-h-full flex-col">
      <header className="sticky top-0 z-40 border-b border-slate-200/70 bg-white/80 backdrop-blur-xl dark:border-slate-800/70 dark:bg-slate-950/80">
        <div className="mx-auto flex h-16 max-w-6xl items-center justify-between px-4">
          <Link to="/"><Logo size="md" /></Link>

          <nav className="hidden items-center gap-1 md:flex">
            {navLinks.map((l) => (
              <NavLink
                key={l.to}
                to={l.to}
                end={l.end}
                className={({ isActive }) =>
                  `rounded-lg px-3.5 py-2 text-sm font-medium transition-colors ${
                    isActive
                      ? 'text-brand-600 dark:text-brand-400'
                      : 'text-slate-600 hover:text-slate-900 dark:text-slate-300 dark:hover:text-white'
                  }`
                }
              >
                {l.label}
              </NavLink>
            ))}
          </nav>

          <div className="flex items-center gap-2">
            <ThemeToggle />
            {isAuthenticated ? (
              <Link to={dashboardPath(user)} className="btn-primary btn-sm sm:!px-4 sm:!py-2.5 sm:!text-sm">
                {user?.firstName ? `Hi, ${user.firstName}` : 'Dashboard'} <FiArrowRight />
              </Link>
            ) : (
              <>
                <Link to="/login" className="btn-ghost hidden sm:inline-flex">Sign in</Link>
                <Link to="/register" className="btn-primary">Get started <FiArrowRight /></Link>
              </>
            )}
          </div>
        </div>
      </header>

      <main className="flex-1">
        <Outlet />
      </main>

      <footer className="border-t border-slate-200 bg-white dark:border-slate-800 dark:bg-slate-950">
        <div className="mx-auto max-w-6xl px-4 py-14">
          <div className="grid gap-10 md:grid-cols-[1.4fr_1fr_1fr_1fr]">
            <div>
              <Logo size="md" />
              <p className="mt-4 max-w-xs text-sm text-slate-500 dark:text-slate-400">
                AI-powered recruitment and talent management. Hire smarter, get hired faster.
              </p>
              <div className="mt-5 flex gap-2">
                {[FiLinkedin, FiTwitter, FiGithub, FiMail].map((Icon, i) => (
                  <a
                    key={i}
                    href="#"
                    className="flex h-9 w-9 items-center justify-center rounded-lg border border-slate-200 text-slate-500 transition-colors hover:border-brand-300 hover:bg-brand-50 hover:text-brand-600 dark:border-slate-800 dark:hover:bg-slate-800"
                    aria-label="Social link"
                  >
                    <Icon className="h-4 w-4" />
                  </a>
                ))}
              </div>
            </div>
            {footerCols.map((col) => (
              <div key={col.title}>
                <h4 className="text-sm font-semibold text-slate-900 dark:text-white">{col.title}</h4>
                <ul className="mt-4 space-y-2.5">
                  {col.links.map(([label, to]) => (
                    <li key={label}>
                      <Link to={to} className="text-sm text-slate-500 transition-colors hover:text-brand-600 dark:text-slate-400 dark:hover:text-brand-400">
                        {label}
                      </Link>
                    </li>
                  ))}
                </ul>
              </div>
            ))}
          </div>
          <div className="mt-12 flex flex-col items-center justify-between gap-3 border-t border-slate-200 pt-6 text-sm text-slate-500 dark:border-slate-800 dark:text-slate-400 sm:flex-row">
            <span>© {new Date().getFullYear()} GetCareers. All rights reserved.</span>
            <span>AI-Powered Recruitment &amp; Talent Management.</span>
          </div>
        </div>
      </footer>
    </div>
  );
}
