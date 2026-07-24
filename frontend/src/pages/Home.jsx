import { useEffect, useRef, useState } from 'react';
import { Link } from 'react-router-dom';
import { motion, useInView, animate, AnimatePresence } from 'framer-motion';
import {
  FiSearch, FiCpu, FiTrendingUp, FiUsers, FiZap, FiArrowRight, FiUploadCloud, FiTarget,
  FiCalendar, FiStar, FiChevronDown, FiCheckCircle, FiBarChart2, FiShield, FiMessageSquare,
} from 'react-icons/fi';
import {
  AreaChart, Area, ResponsiveContainer, XAxis, Tooltip, BarChart, Bar, Cell,
} from 'recharts';
import { jobsApi } from '../api';
import JobCard from '../components/JobCard';
import HeroIllustration from '../components/HeroIllustration';
import { useAuth } from '../contexts/AuthContext';
import { dashboardPath } from '../utils/roles';

/* ---------- Animated count-up number ---------- */
function Counter({ to, suffix = '', duration = 1.6 }) {
  const ref = useRef(null);
  const inView = useInView(ref, { once: true, margin: '-60px' });
  const [val, setVal] = useState(0);
  useEffect(() => {
    if (!inView) return;
    const controls = animate(0, to, {
      duration,
      ease: 'easeOut',
      onUpdate: (v) => setVal(v),
    });
    return () => controls.stop();
  }, [inView, to, duration]);
  const display = Number.isInteger(to) ? Math.round(val).toLocaleString() : val.toFixed(1);
  return <span ref={ref}>{display}{suffix}</span>;
}

const features = [
  { icon: FiCpu, title: 'AI Candidate Screening', desc: 'Resumes parsed and scored automatically to surface the best-fit candidates in seconds.' },
  { icon: FiTrendingUp, title: 'Smart Job Matching', desc: 'Personalised recommendations based on skills, experience and career goals.' },
  { icon: FiUsers, title: 'End-to-End Hiring', desc: 'From application to offer, manage the entire recruitment workflow in one place.' },
  { icon: FiBarChart2, title: 'Real-Time Analytics', desc: 'Track applications, interviews and hiring performance at a glance.' },
  { icon: FiShield, title: 'Enterprise Security', desc: 'Role-based access, audit logs and encrypted data keep your pipeline safe.' },
  { icon: FiMessageSquare, title: 'Built-in Messaging', desc: 'Recruiters and candidates stay connected without leaving the platform.' },
];

const steps = [
  { icon: FiUploadCloud, title: 'Create your profile', desc: 'Sign up and upload your resume — our AI does the parsing for you.' },
  { icon: FiTarget, title: 'Get matched', desc: 'Receive tailored job recommendations ranked by fit and relevance.' },
  { icon: FiCalendar, title: 'Interview & get hired', desc: 'Apply, schedule interviews and track every step to your offer.' },
];

const stats = [
  { to: 10, suffix: 'k+', label: 'Active Candidates' },
  { to: 1200, suffix: '+', label: 'Open Positions' },
  { to: 500, suffix: '+', label: 'Hiring Companies' },
  { to: 94, suffix: '%', label: 'Match Accuracy' },
];

const companies = ['Northwind', 'Acme Corp', 'Globex', 'Initech', 'Umbrella', 'Stark Ind.', 'Wayne Ent.', 'Hooli'];

const testimonials = [
  { name: 'Sarah Chen', role: 'Head of Talent, Globex', quote: 'GetCareers cut our time-to-hire in half. The AI ranking surfaces candidates we would have missed.', initials: 'SC' },
  { name: 'Marcus Reid', role: 'Software Engineer', quote: 'I uploaded my resume and had three relevant interviews within a week. The matching is scarily accurate.', initials: 'MR' },
  { name: 'Elena Duarte', role: 'Recruiter, Initech', quote: 'The pipeline view and built-in messaging keep everything in one place. It just works.', initials: 'ED' },
];

const faqs = [
  { q: 'Is GetCareers free for job seekers?', a: 'Yes. Creating a profile, uploading your resume, getting AI recommendations and applying to jobs is completely free for candidates.' },
  { q: 'How does the AI matching work?', a: 'We parse your resume, extract skills and experience, and score them against every open role to rank the best-fit opportunities for you.' },
  { q: 'Can my company post jobs?', a: 'Absolutely. Recruiters can post roles, manage an applicant pipeline, use AI ranking and schedule interviews from a dedicated dashboard.' },
  { q: 'Is my data secure?', a: 'We use role-based access control, encrypted storage and full audit logging to keep your data protected at every step.' },
];

const analyticsData = [
  { m: 'Jan', v: 42 }, { m: 'Feb', v: 58 }, { m: 'Mar', v: 71 }, { m: 'Apr', v: 65 },
  { m: 'May', v: 88 }, { m: 'Jun', v: 102 }, { m: 'Jul', v: 124 },
];
const funnelData = [
  { s: 'Applied', v: 100, c: '#818cf8' }, { s: 'Screened', v: 68, c: '#6366f1' },
  { s: 'Interview', v: 34, c: '#22d3ee' }, { s: 'Offer', v: 12, c: '#06b6d4' },
];

const fadeUp = {
  hidden: { opacity: 0, y: 24 },
  show: (i = 0) => ({ opacity: 1, y: 0, transition: { delay: i * 0.08, duration: 0.5 } }),
};

function Section({ children, className = '' }) {
  return <section className={`mx-auto max-w-6xl px-4 ${className}`}>{children}</section>;
}

export default function Home() {
  const { user, isAuthenticated } = useAuth();
  const [featured, setFeatured] = useState(null);
  const [openFaq, setOpenFaq] = useState(0);
  const exploreDashboardLink = isAuthenticated ? dashboardPath(user) : '/login';

  useEffect(() => {
    let active = true;
    jobsApi.search({ pageSize: 6, page: 1 })
      .then((res) => { if (active) setFeatured(res.items || []); })
      .catch(() => { if (active) setFeatured([]); });
    return () => { active = false; };
  }, []);

  return (
    <div className="overflow-hidden">
      {/* ============ HERO ============ */}
      <section className="relative isolate">
        {/* animated background */}
        <div className="pointer-events-none absolute inset-0 -z-10 overflow-hidden">
          <div className="absolute -left-24 -top-24 h-96 w-96 rounded-full bg-brand-300/40 blur-3xl animate-blob dark:bg-brand-600/20" />
          <div className="absolute right-0 top-10 h-96 w-96 rounded-full bg-accent-300/40 blur-3xl animate-blob dark:bg-accent-600/20" style={{ animationDelay: '3s' }} />
          <div className="absolute bottom-0 left-1/3 h-80 w-80 rounded-full bg-violet-300/30 blur-3xl animate-blob dark:bg-violet-700/20" style={{ animationDelay: '6s' }} />
        </div>

        <div className="mx-auto grid max-w-6xl items-center gap-12 px-4 py-20 lg:grid-cols-2 lg:py-28">
          <motion.div initial="hidden" animate="show" variants={{ show: { transition: { staggerChildren: 0.1 } } }}>
            <motion.span variants={fadeUp} className="eyebrow">
              <FiZap className="h-3.5 w-3.5" /> AI-Powered Recruitment
            </motion.span>
            <motion.h1 variants={fadeUp} className="mt-5 text-4xl font-extrabold leading-[1.1] tracking-tight text-slate-900 dark:text-white sm:text-5xl lg:text-6xl">
              Find the right talent,{' '}
              <span className="text-gradient">faster</span>.
            </motion.h1>
            <motion.p variants={fadeUp} className="mt-5 max-w-xl text-lg text-slate-600 dark:text-slate-300">
              GetCareers connects candidates and employers with AI-driven screening, ranking and
              recommendations — the smarter way to hire and get hired.
            </motion.p>
            <motion.div variants={fadeUp} className="mt-8 flex flex-wrap items-center gap-3">
              <Link to="/jobs" className="btn-primary btn-lg"><FiSearch /> Browse Jobs</Link>
              <Link to="/register" className="btn-secondary btn-lg">Get started free <FiArrowRight /></Link>
            </motion.div>
            <motion.div variants={fadeUp} className="mt-8 flex flex-wrap items-center gap-x-6 gap-y-2 text-sm text-slate-500 dark:text-slate-400">
              {['No credit card required', 'Free for candidates', 'Cancel anytime'].map((t) => (
                <span key={t} className="flex items-center gap-1.5"><FiCheckCircle className="text-emerald-500" /> {t}</span>
              ))}
            </motion.div>
          </motion.div>

          {/* hero illustration + floating cards */}
          <motion.div
            initial={{ opacity: 0, scale: 0.92 }}
            animate={{ opacity: 1, scale: 1 }}
            transition={{ duration: 0.7, ease: 'easeOut' }}
            className="relative mx-auto w-full max-w-md lg:max-w-none"
          >
            <HeroIllustration className="w-full drop-shadow-sm" />
            <motion.div
              className="absolute -left-4 top-10 flex items-center gap-3 rounded-2xl border border-slate-200/70 bg-white/90 p-3 shadow-xl backdrop-blur dark:border-slate-700 dark:bg-slate-800/90"
              animate={{ y: [0, -10, 0] }} transition={{ duration: 4, repeat: Infinity }}
            >
              <span className="flex h-10 w-10 items-center justify-center rounded-xl bg-emerald-100 text-emerald-600"><FiCheckCircle /></span>
              <div>
                <div className="text-sm font-bold text-slate-900 dark:text-white">Match found</div>
                <div className="text-xs text-slate-500">96% fit score</div>
              </div>
            </motion.div>
            <motion.div
              className="absolute -right-2 bottom-8 flex items-center gap-3 rounded-2xl border border-slate-200/70 bg-white/90 p-3 shadow-xl backdrop-blur dark:border-slate-700 dark:bg-slate-800/90"
              animate={{ y: [0, 10, 0] }} transition={{ duration: 5, repeat: Infinity }}
            >
              <span className="flex h-10 w-10 items-center justify-center rounded-xl bg-brand-100 text-brand-600"><FiCpu /></span>
              <div>
                <div className="text-sm font-bold text-slate-900 dark:text-white">AI Screening</div>
                <div className="text-xs text-slate-500">1,240 resumes ranked</div>
              </div>
            </motion.div>
          </motion.div>
        </div>
      </section>

      {/* ============ TRUSTED COMPANIES ============ */}
      <Section className="pb-8">
        <p className="text-center text-xs font-semibold uppercase tracking-widest text-slate-400">
          Trusted by hiring teams at
        </p>
        <div className="mt-6 flex flex-wrap items-center justify-center gap-x-10 gap-y-4">
          {companies.map((c) => (
            <span key={c} className="text-lg font-bold text-slate-400 grayscale transition hover:text-slate-600 dark:text-slate-600 dark:hover:text-slate-400">
              {c}
            </span>
          ))}
        </div>
      </Section>

      {/* ============ STATS ============ */}
      <Section className="py-14">
        <div className="card grid grid-cols-2 gap-6 p-8 sm:grid-cols-4">
          {stats.map((s, i) => (
            <motion.div key={s.label} variants={fadeUp} custom={i} initial="hidden" whileInView="show" viewport={{ once: true }} className="text-center">
              <div className="text-3xl font-extrabold text-gradient sm:text-4xl">
                <Counter to={s.to} suffix={s.suffix} />
              </div>
              <div className="mt-1 text-sm font-medium text-slate-500 dark:text-slate-400">{s.label}</div>
            </motion.div>
          ))}
        </div>
      </Section>

      {/* ============ FEATURES ============ */}
      <Section className="py-16">
        <div className="mx-auto max-w-2xl text-center">
          <span className="eyebrow">Platform</span>
          <h2 className="mt-4 text-3xl font-bold text-slate-900 dark:text-white sm:text-4xl">
            Everything you need to hire intelligently
          </h2>
          <p className="mt-3 text-slate-600 dark:text-slate-300">
            A complete AI toolkit for recruiters, hiring managers and candidates alike.
          </p>
        </div>
        <div className="mt-12 grid grid-cols-1 gap-6 sm:grid-cols-2 lg:grid-cols-3">
          {features.map((f, i) => (
            <motion.div key={f.title} variants={fadeUp} custom={i} initial="hidden" whileInView="show" viewport={{ once: true }} className="card card-hover p-6">
              <span className="flex h-12 w-12 items-center justify-center rounded-xl bg-gradient-to-br from-brand-500 to-accent-500 text-white shadow-lg">
                <f.icon className="h-6 w-6" />
              </span>
              <h3 className="mt-5 text-lg font-semibold text-slate-900 dark:text-white">{f.title}</h3>
              <p className="mt-2 text-sm text-slate-500 dark:text-slate-400">{f.desc}</p>
            </motion.div>
          ))}
        </div>
      </Section>

      {/* ============ HOW IT WORKS ============ */}
      <Section className="py-16">
        <div className="mx-auto max-w-2xl text-center">
          <span className="eyebrow">How it works</span>
          <h2 className="mt-4 text-3xl font-bold text-slate-900 dark:text-white sm:text-4xl">
            From sign-up to hired in three steps
          </h2>
        </div>
        <div className="relative mt-12 grid gap-8 md:grid-cols-3">
          <div className="absolute left-0 right-0 top-8 hidden h-px bg-gradient-to-r from-transparent via-brand-200 to-transparent md:block dark:via-brand-800" />
          {steps.map((s, i) => (
            <motion.div key={s.title} variants={fadeUp} custom={i} initial="hidden" whileInView="show" viewport={{ once: true }} className="relative text-center">
              <div className="mx-auto flex h-16 w-16 items-center justify-center rounded-2xl border border-brand-100 bg-white text-brand-600 shadow-md dark:border-brand-900 dark:bg-slate-900">
                <s.icon className="h-7 w-7" />
              </div>
              <div className="mx-auto mt-4 flex h-6 w-6 items-center justify-center rounded-full bg-brand-600 text-xs font-bold text-white">{i + 1}</div>
              <h3 className="mt-3 text-lg font-semibold text-slate-900 dark:text-white">{s.title}</h3>
              <p className="mx-auto mt-2 max-w-xs text-sm text-slate-500 dark:text-slate-400">{s.desc}</p>
            </motion.div>
          ))}
        </div>
      </Section>

      {/* ============ FEATURED JOBS ============ */}
      <Section className="py-16">
        <div className="flex flex-col items-start justify-between gap-3 sm:flex-row sm:items-end">
          <div>
            <span className="eyebrow">Opportunities</span>
            <h2 className="mt-4 text-3xl font-bold text-slate-900 dark:text-white sm:text-4xl">Featured jobs</h2>
          </div>
          <Link to="/jobs" className="btn-ghost">View all jobs <FiArrowRight /></Link>
        </div>
        <div className="mt-8 grid grid-cols-1 gap-4 md:grid-cols-2 lg:grid-cols-3">
          {featured === null ? (
            Array.from({ length: 6 }).map((_, i) => (
              <div key={i} className="card space-y-3 p-5">
                <div className="h-5 w-2/3 animate-pulse rounded bg-slate-200 dark:bg-slate-700" />
                <div className="h-4 w-1/3 animate-pulse rounded bg-slate-200 dark:bg-slate-700" />
                <div className="h-4 w-full animate-pulse rounded bg-slate-200 dark:bg-slate-700" />
              </div>
            ))
          ) : featured.length ? (
            featured.map((job, i) => (
              <motion.div key={job.id} variants={fadeUp} custom={i} initial="hidden" whileInView="show" viewport={{ once: true }}>
                <JobCard job={job} />
              </motion.div>
            ))
          ) : (
            <div className="col-span-full rounded-2xl border border-dashed border-slate-300 p-10 text-center text-slate-500 dark:border-slate-700">
              No open positions right now — check back soon.
            </div>
          )}
        </div>
      </Section>

      {/* ============ ANALYTICS PREVIEW ============ */}
      <Section className="py-16">
        <div className="card overflow-hidden">
          <div className="grid gap-8 p-8 lg:grid-cols-2 lg:items-center">
            <div>
              <span className="eyebrow"><FiBarChart2 className="h-3.5 w-3.5" /> Analytics</span>
              <h2 className="mt-4 text-3xl font-bold text-slate-900 dark:text-white">
                Recruitment insights, in real time
              </h2>
              <p className="mt-3 text-slate-600 dark:text-slate-300">
                Track applications, monitor your hiring funnel and measure performance with a live analytics
                dashboard built for decision-makers.
              </p>
              <ul className="mt-6 space-y-2.5">
                {['Applications & interview trends', 'Conversion funnel by stage', 'Time-to-hire and source metrics'].map((t) => (
                  <li key={t} className="flex items-center gap-2 text-sm text-slate-600 dark:text-slate-300">
                    <FiCheckCircle className="text-emerald-500" /> {t}
                  </li>
                ))}
              </ul>
              <Link to={exploreDashboardLink} className="btn-primary mt-8">Explore the dashboard <FiArrowRight /></Link>
            </div>
            <div className="grid gap-4">
              <div className="rounded-2xl border border-slate-100 bg-slate-50/60 p-4 dark:border-slate-800 dark:bg-slate-800/40">
                <div className="mb-2 text-xs font-semibold text-slate-500">Applications over time</div>
                <ResponsiveContainer width="100%" height={140}>
                  <AreaChart data={analyticsData} margin={{ top: 4, right: 4, left: 0, bottom: 0 }}>
                    <defs>
                      <linearGradient id="hg" x1="0" y1="0" x2="0" y2="1">
                        <stop offset="0%" stopColor="#6366f1" stopOpacity={0.5} />
                        <stop offset="100%" stopColor="#6366f1" stopOpacity={0} />
                      </linearGradient>
                    </defs>
                    <XAxis dataKey="m" tick={{ fontSize: 11, fill: '#94a3b8' }} axisLine={false} tickLine={false} />
                    <Tooltip cursor={{ stroke: '#6366f1', strokeWidth: 1 }} />
                    <Area type="monotone" dataKey="v" stroke="#6366f1" strokeWidth={2.5} fill="url(#hg)" />
                  </AreaChart>
                </ResponsiveContainer>
              </div>
              <div className="rounded-2xl border border-slate-100 bg-slate-50/60 p-4 dark:border-slate-800 dark:bg-slate-800/40">
                <div className="mb-2 text-xs font-semibold text-slate-500">Hiring funnel</div>
                <ResponsiveContainer width="100%" height={120}>
                  <BarChart data={funnelData} layout="vertical" margin={{ left: 0, right: 8 }}>
                    <XAxis type="number" hide />
                    <Bar dataKey="v" radius={[0, 6, 6, 0]} barSize={16}>
                      {funnelData.map((d) => <Cell key={d.s} fill={d.c} />)}
                    </Bar>
                    <Tooltip cursor={{ fill: 'rgba(99,102,241,0.06)' }} />
                  </BarChart>
                </ResponsiveContainer>
              </div>
            </div>
          </div>
        </div>
      </Section>

      {/* ============ TESTIMONIALS ============ */}
      <Section className="py-16">
        <div className="mx-auto max-w-2xl text-center">
          <span className="eyebrow">Testimonials</span>
          <h2 className="mt-4 text-3xl font-bold text-slate-900 dark:text-white sm:text-4xl">Loved by teams and talent</h2>
        </div>
        <div className="mt-12 grid gap-6 md:grid-cols-3">
          {testimonials.map((t, i) => (
            <motion.figure key={t.name} variants={fadeUp} custom={i} initial="hidden" whileInView="show" viewport={{ once: true }} className="card p-6">
              <div className="flex gap-1 text-amber-400">
                {Array.from({ length: 5 }).map((_, j) => <FiStar key={j} className="h-4 w-4 fill-current" />)}
              </div>
              <blockquote className="mt-4 text-sm text-slate-600 dark:text-slate-300">“{t.quote}”</blockquote>
              <figcaption className="mt-5 flex items-center gap-3">
                <span className="flex h-10 w-10 items-center justify-center rounded-full bg-gradient-to-br from-brand-600 to-accent-500 text-sm font-semibold text-white">{t.initials}</span>
                <div>
                  <div className="text-sm font-semibold text-slate-900 dark:text-white">{t.name}</div>
                  <div className="text-xs text-slate-500">{t.role}</div>
                </div>
              </figcaption>
            </motion.figure>
          ))}
        </div>
      </Section>

      {/* ============ FAQ ============ */}
      <Section className="py-16">
        <div className="mx-auto max-w-3xl">
          <div className="text-center">
            <span className="eyebrow">FAQ</span>
            <h2 className="mt-4 text-3xl font-bold text-slate-900 dark:text-white sm:text-4xl">Frequently asked questions</h2>
          </div>
          <div className="mt-10 space-y-3">
            {faqs.map((f, i) => {
              const open = openFaq === i;
              return (
                <div key={f.q} className="card overflow-hidden">
                  <button
                    onClick={() => setOpenFaq(open ? -1 : i)}
                    className="flex w-full items-center justify-between gap-4 px-5 py-4 text-left"
                  >
                    <span className="font-semibold text-slate-900 dark:text-white">{f.q}</span>
                    <FiChevronDown className={`h-5 w-5 shrink-0 text-slate-400 transition-transform ${open ? 'rotate-180' : ''}`} />
                  </button>
                  <AnimatePresence initial={false}>
                    {open && (
                      <motion.div
                        initial={{ height: 0, opacity: 0 }}
                        animate={{ height: 'auto', opacity: 1 }}
                        exit={{ height: 0, opacity: 0 }}
                        transition={{ duration: 0.25 }}
                      >
                        <p className="px-5 pb-5 text-sm text-slate-600 dark:text-slate-400">{f.a}</p>
                      </motion.div>
                    )}
                  </AnimatePresence>
                </div>
              );
            })}
          </div>
        </div>
      </Section>

      {/* ============ CTA ============ */}
      <Section className="py-16">
        <div className="relative overflow-hidden rounded-3xl bg-gradient-to-br from-brand-600 via-brand-500 to-accent-500 px-6 py-16 text-center shadow-[var(--shadow-lift)]">
          <div className="pointer-events-none absolute -right-16 -top-16 h-64 w-64 rounded-full bg-white/10 blur-2xl" />
          <div className="pointer-events-none absolute -bottom-16 -left-16 h-64 w-64 rounded-full bg-white/10 blur-2xl" />
          <h2 className="relative text-3xl font-bold text-white sm:text-4xl">Ready to take the next step?</h2>
          <p className="relative mx-auto mt-3 max-w-xl text-white/85">
            Join thousands of candidates and companies hiring smarter with GetCareers.
          </p>
          <div className="relative mt-8 flex flex-wrap items-center justify-center gap-3">
            <Link to="/register" className="btn-lg inline-flex items-center gap-2 rounded-xl bg-white px-6 font-semibold text-brand-700 shadow-lg transition hover:-translate-y-0.5">
              Get started free <FiArrowRight />
            </Link>
            <Link to="/jobs" className="btn-lg inline-flex items-center gap-2 rounded-xl border border-white/40 px-6 font-semibold text-white transition hover:bg-white/10">
              Browse jobs
            </Link>
          </div>
        </div>
      </Section>
    </div>
  );
}
