import { motion } from 'framer-motion';
import { Link } from 'react-router-dom';
import Logo from './Logo';

/**
 * Shared shell for the Login and Register screens: a single centred card split
 * into a form half and a branded half, separated by an organic torn-paper edge.
 */

/** Organic "torn paper" edge separating the form from the branded panel. */
const TORN_EDGE = 'M0,0 H55 C20,70 75,130 45,200 C15,270 80,330 50,400 C20,470 78,530 48,600 C18,670 72,730 52,800 H0 Z';

/** Pill-shaped field styling shared by every auth input. */
export const authInput =
  'w-full rounded-full border border-slate-200 bg-white py-3.5 text-sm text-slate-900 ' +
  'shadow-[0_2px_12px_rgba(15,23,42,0.07)] transition-colors placeholder:text-slate-400 ' +
  'focus:border-brand-500 focus:outline-none focus:ring-4 focus:ring-brand-500/15 ' +
  'dark:border-slate-700 dark:bg-slate-800/70 dark:text-slate-100';

/** Gradient icon chip inset on the left of a pill field. */
export function FieldIcon({ icon: Icon }) {
  return (
    <span className="pointer-events-none absolute left-1.5 top-1/2 flex h-10 w-10 -translate-y-1/2 items-center justify-center rounded-full bg-gradient-to-br from-brand-600 to-brand-500 text-white shadow-[0_4px_12px_-2px_rgba(79,70,229,0.5)]">
      <Icon className="h-4 w-4" />
    </span>
  );
}

/** Inline validation message, aligned with the pill's inner padding. */
export function FieldError({ error }) {
  if (!error) return null;
  return <p className="mt-1.5 pl-5 text-xs text-red-600">{error.message}</p>;
}

export default function AuthCard({ heading, subheading, panelHeading, panelText, children, wide = false }) {
  return (
    <div className="relative flex min-h-screen items-center justify-center overflow-hidden px-4 py-10">
      {/* decorative background */}
      <div className="pointer-events-none absolute inset-0 overflow-hidden">
        <div className="absolute -left-24 -top-24 h-80 w-80 rounded-full bg-brand-300/40 blur-3xl animate-blob dark:bg-brand-700/20" />
        <div className="absolute -bottom-24 -right-24 h-80 w-80 rounded-full bg-accent-300/40 blur-3xl animate-blob dark:bg-accent-700/20" style={{ animationDelay: '3s' }} />
      </div>

      <motion.div
        initial={{ opacity: 0, y: 18 }}
        animate={{ opacity: 1, y: 0 }}
        transition={{ duration: 0.45 }}
        className={`relative w-full overflow-hidden rounded-[2rem] bg-white shadow-[var(--shadow-lift)] dark:bg-slate-900 lg:grid lg:grid-cols-2 ${wide ? 'max-w-6xl' : 'max-w-5xl'}`}
      >
        {/* ---------- form panel ---------- */}
        <div className="relative z-10 px-6 py-12 sm:px-12 lg:py-16 lg:pr-4">
          <div className={`mx-auto w-full ${wide ? 'max-w-md' : 'max-w-sm'}`}>
            <div className="mb-8 flex justify-center lg:hidden">
              <Link to="/"><Logo size="lg" /></Link>
            </div>

            <div className="text-center">
              <h1 className="text-4xl font-bold text-slate-900 dark:text-white">{heading}</h1>
              <p className="mt-1.5 text-sm text-slate-500 dark:text-slate-400">{subheading}</p>
            </div>

            {children}
          </div>
        </div>

        {/* ---------- branded panel ---------- */}
        <div className="relative hidden overflow-hidden bg-gradient-to-br from-brand-700 via-brand-600 to-accent-600 lg:block">
          <div className="pointer-events-none absolute -right-16 -top-16 h-64 w-64 rounded-full bg-white/10 blur-2xl animate-blob" />
          <div className="pointer-events-none absolute -bottom-20 right-10 h-72 w-72 rounded-full bg-white/10 blur-2xl animate-blob" style={{ animationDelay: '4s' }} />

          {/* torn-paper edge: back layer for depth, then the solid card-coloured edge */}
          <svg
            className="pointer-events-none absolute inset-y-0 left-0 h-full w-28 translate-x-3 text-white/25"
            viewBox="0 0 100 800"
            preserveAspectRatio="none"
            fill="currentColor"
            aria-hidden="true"
          >
            <path d={TORN_EDGE} />
          </svg>
          <svg
            className="pointer-events-none absolute inset-y-0 left-0 h-full w-28 -translate-x-px text-white dark:text-slate-900"
            viewBox="0 0 100 800"
            preserveAspectRatio="none"
            fill="currentColor"
            aria-hidden="true"
          >
            <path d={TORN_EDGE} />
          </svg>

          <div className="relative flex h-full flex-col justify-between py-16 pl-28 pr-12">
            <span className="[&_span]:!text-white"><Logo size="md" /></span>

            <motion.div
              initial={{ opacity: 0, y: 20 }}
              animate={{ opacity: 1, y: 0 }}
              transition={{ duration: 0.6, delay: 0.1 }}
              className="text-center"
            >
              <h2 className="text-3xl font-bold text-white">{panelHeading}</h2>
              <p className="mx-auto mt-4 max-w-xs text-sm leading-relaxed text-white/80">{panelText}</p>
            </motion.div>

            <p className="text-center text-xs text-white/60">
              © {new Date().getFullYear()} GetCareers — AI-Powered Recruitment.
            </p>
          </div>
        </div>
      </motion.div>
    </div>
  );
}
