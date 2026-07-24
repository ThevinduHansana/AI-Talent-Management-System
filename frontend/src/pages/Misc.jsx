import { useEffect, useRef, useState } from 'react';
import { Link } from 'react-router-dom';
import { useForm } from 'react-hook-form';
import { motion, useInView, animate } from 'framer-motion';
import {
  FiMail, FiPhone, FiMapPin, FiClock, FiArrowLeft, FiArrowRight, FiSend, FiCpu, FiTrendingUp,
  FiShield, FiUsers, FiCheckCircle, FiTarget, FiHeart, FiZap, FiLinkedin, FiTwitter, FiGithub,
} from 'react-icons/fi';
import { useToast } from '../contexts/ToastContext';

/* ---------- shared bits ---------- */
function Blobs() {
  return (
    <div className="pointer-events-none absolute inset-0 -z-10 overflow-hidden">
      <div className="absolute -left-24 -top-24 h-96 w-96 rounded-full bg-brand-300/40 blur-3xl animate-blob dark:bg-brand-600/20" />
      <div className="absolute right-0 top-10 h-96 w-96 rounded-full bg-accent-300/40 blur-3xl animate-blob dark:bg-accent-600/20" style={{ animationDelay: '3s' }} />
    </div>
  );
}

function Counter({ to, suffix = '' }) {
  const ref = useRef(null);
  const inView = useInView(ref, { once: true, margin: '-40px' });
  const [val, setVal] = useState(0);
  useEffect(() => {
    if (!inView) return;
    const controls = animate(0, to, { duration: 1.4, ease: 'easeOut', onUpdate: (v) => setVal(v) });
    return () => controls.stop();
  }, [inView, to]);
  return <span ref={ref}>{Math.round(val).toLocaleString()}{suffix}</span>;
}

const fadeUp = {
  hidden: { opacity: 0, y: 22 },
  show: (i = 0) => ({ opacity: 1, y: 0, transition: { delay: i * 0.08, duration: 0.5 } }),
};

/* =========================================================================
   ABOUT
   ========================================================================= */
const values = [
  { icon: FiCpu, title: 'AI-first', desc: 'Intelligent screening and matching at the core of everything we build.' },
  { icon: FiTrendingUp, title: 'Results-driven', desc: 'We measure success by faster hires and better matches — nothing else.' },
  { icon: FiShield, title: 'Trust & security', desc: 'Role-based access, audit trails and encrypted data protect every user.' },
  { icon: FiHeart, title: 'People-centered', desc: 'Technology that frees recruiters to focus on people, not paperwork.' },
];

const aboutStats = [
  { to: 10, suffix: 'k+', label: 'Candidates' },
  { to: 500, suffix: '+', label: 'Companies' },
  { to: 94, suffix: '%', label: 'Match accuracy' },
  { to: 4, suffix: '', label: 'Roles supported' },
];

export function About() {
  return (
    <div className="overflow-hidden">
      {/* hero */}
      <section className="relative isolate">
        <Blobs />
        <div className="mx-auto max-w-4xl px-4 py-24 text-center">
          <motion.span variants={fadeUp} initial="hidden" animate="show" className="eyebrow">
            <FiZap className="h-3.5 w-3.5" /> About GetCareers
          </motion.span>
          <motion.h1 variants={fadeUp} custom={1} initial="hidden" animate="show" className="mt-5 text-4xl font-extrabold leading-tight tracking-tight text-slate-900 dark:text-white sm:text-5xl">
            Smarter hiring for a <span className="text-gradient">global workforce</span>
          </motion.h1>
          <motion.p variants={fadeUp} custom={2} initial="hidden" animate="show" className="mx-auto mt-5 max-w-2xl text-lg text-slate-600 dark:text-slate-300">
            GetCareers is an AI-powered recruitment and talent-management platform that connects the right
            people with the right opportunities — from first application to final offer.
          </motion.p>
          <motion.div variants={fadeUp} custom={3} initial="hidden" animate="show" className="mt-8 flex flex-wrap items-center justify-center gap-3">
            <Link to="/jobs" className="btn-primary btn-lg">Browse jobs <FiArrowRight /></Link>
            <Link to="/register" className="btn-secondary btn-lg">Join GetCareers</Link>
          </motion.div>
        </div>
      </section>

      {/* stats */}
      <section className="mx-auto max-w-5xl px-4 pb-4">
        <div className="card grid grid-cols-2 gap-6 p-8 sm:grid-cols-4">
          {aboutStats.map((s, i) => (
            <motion.div key={s.label} variants={fadeUp} custom={i} initial="hidden" whileInView="show" viewport={{ once: true }} className="text-center">
              <div className="text-3xl font-extrabold text-gradient sm:text-4xl"><Counter to={s.to} suffix={s.suffix} /></div>
              <div className="mt-1 text-sm font-medium text-slate-500 dark:text-slate-400">{s.label}</div>
            </motion.div>
          ))}
        </div>
      </section>

      {/* mission */}
      <section className="mx-auto max-w-5xl px-4 py-16">
        <div className="grid gap-8 lg:grid-cols-2 lg:items-center">
          <motion.div variants={fadeUp} initial="hidden" whileInView="show" viewport={{ once: true }}>
            <span className="eyebrow"><FiTarget className="h-3.5 w-3.5" /> Our mission</span>
            <h2 className="mt-4 text-3xl font-bold text-slate-900 dark:text-white">
              Make great hiring effortless for everyone
            </h2>
            <div className="mt-4 space-y-4 text-slate-600 dark:text-slate-300">
              <p>
                We built GetCareers for multinational HR teams who were drowning in resumes and spreadsheets.
                Our AI automates candidate screening, ranking and job matching so recruiters can spend their
                time on people, not paperwork.
              </p>
              <p>
                Candidates get a single place to manage their profile, resume and applications — with
                AI-driven recommendations that surface the roles they&apos;re most likely to land.
              </p>
            </div>
            <ul className="mt-6 space-y-2.5">
              {['Recruiters, hiring managers & candidates in one workflow', 'Real, explainable AI match scores', 'Enterprise-grade security & audit logging'].map((t) => (
                <li key={t} className="flex items-center gap-2 text-sm text-slate-600 dark:text-slate-300">
                  <FiCheckCircle className="text-emerald-500" /> {t}
                </li>
              ))}
            </ul>
          </motion.div>

          <motion.div variants={fadeUp} custom={1} initial="hidden" whileInView="show" viewport={{ once: true }} className="relative">
            <div className="rounded-3xl bg-gradient-to-br from-brand-600 via-brand-500 to-accent-500 p-8 text-white shadow-[var(--shadow-lift)]">
              <FiUsers className="h-10 w-10 opacity-90" />
              <blockquote className="mt-5 text-xl font-semibold leading-snug">
                “We cut time-to-hire in half — the AI ranking surfaces candidates we would have missed.”
              </blockquote>
              <div className="mt-5 text-sm text-white/80">Head of Talent, Globex</div>
            </div>
          </motion.div>
        </div>
      </section>

      {/* values */}
      <section className="mx-auto max-w-6xl px-4 py-16">
        <div className="mx-auto max-w-2xl text-center">
          <span className="eyebrow">What we value</span>
          <h2 className="mt-4 text-3xl font-bold text-slate-900 dark:text-white sm:text-4xl">The principles behind the product</h2>
        </div>
        <div className="mt-12 grid grid-cols-1 gap-6 sm:grid-cols-2 lg:grid-cols-4">
          {values.map((v, i) => (
            <motion.div key={v.title} variants={fadeUp} custom={i} initial="hidden" whileInView="show" viewport={{ once: true }} className="card card-hover p-6">
              <span className="flex h-12 w-12 items-center justify-center rounded-xl bg-gradient-to-br from-brand-500 to-accent-500 text-white shadow-lg">
                <v.icon className="h-6 w-6" />
              </span>
              <h3 className="mt-5 text-lg font-semibold text-slate-900 dark:text-white">{v.title}</h3>
              <p className="mt-2 text-sm text-slate-500 dark:text-slate-400">{v.desc}</p>
            </motion.div>
          ))}
        </div>
      </section>

      {/* CTA */}
      <section className="mx-auto max-w-6xl px-4 pb-20">
        <div className="relative overflow-hidden rounded-3xl bg-gradient-to-br from-brand-600 via-brand-500 to-accent-500 px-6 py-14 text-center shadow-[var(--shadow-lift)]">
          <div className="pointer-events-none absolute -right-16 -top-16 h-64 w-64 rounded-full bg-white/10 blur-2xl" />
          <h2 className="relative text-3xl font-bold text-white sm:text-4xl">Ready to hire smarter?</h2>
          <p className="relative mx-auto mt-3 max-w-xl text-white/85">Join thousands of teams and candidates on GetCareers.</p>
          <div className="relative mt-8 flex flex-wrap items-center justify-center gap-3">
            <Link to="/register" className="btn-lg inline-flex items-center gap-2 rounded-xl bg-white px-6 font-semibold text-brand-700 shadow-lg transition hover:-translate-y-0.5">Get started free <FiArrowRight /></Link>
            <Link to="/contact" className="btn-lg inline-flex items-center gap-2 rounded-xl border border-white/40 px-6 font-semibold text-white transition hover:bg-white/10">Contact us</Link>
          </div>
        </div>
      </section>
    </div>
  );
}

/* =========================================================================
   CONTACT
   ========================================================================= */
const contactMethods = [
  { icon: FiMail, label: 'Email us', value: 'hello@getcareers.example', hint: 'We reply within 24 hours' },
  { icon: FiPhone, label: 'Call us', value: '+1 (555) 123-4567', hint: 'Mon–Fri, 9am–6pm' },
  { icon: FiMapPin, label: 'Visit us', value: 'New York, USA', hint: '350 Fifth Avenue' },
];

export function Contact() {
  const { toast } = useToast();
  const { register, handleSubmit, reset, formState: { errors, isSubmitting } } = useForm();

  const onSubmit = async () => {
    // Front-end only: no contact endpoint on the backend, so we simulate a successful send.
    await new Promise((r) => setTimeout(r, 600));
    toast("Thanks! Your message has been sent — we'll be in touch soon.", 'success');
    reset();
  };

  return (
    <div className="overflow-hidden">
      <section className="relative isolate">
        <Blobs />
        <div className="mx-auto max-w-3xl px-4 py-20 text-center">
          <motion.span variants={fadeUp} initial="hidden" animate="show" className="eyebrow"><FiMail className="h-3.5 w-3.5" /> Contact</motion.span>
          <motion.h1 variants={fadeUp} custom={1} initial="hidden" animate="show" className="mt-5 text-4xl font-extrabold tracking-tight text-slate-900 dark:text-white sm:text-5xl">
            Let&apos;s <span className="text-gradient">talk</span>
          </motion.h1>
          <motion.p variants={fadeUp} custom={2} initial="hidden" animate="show" className="mx-auto mt-4 max-w-xl text-lg text-slate-600 dark:text-slate-300">
            Questions, partnerships or support — the GetCareers team would love to hear from you.
          </motion.p>
        </div>
      </section>

      <section className="mx-auto max-w-6xl px-4 pb-20">
        <div className="grid gap-6 lg:grid-cols-[1fr_1.3fr]">
          {/* left: contact info */}
          <div className="space-y-4">
            {contactMethods.map((m, i) => (
              <motion.div key={m.label} variants={fadeUp} custom={i} initial="hidden" whileInView="show" viewport={{ once: true }} className="card card-hover flex items-start gap-4 p-5">
                <span className="flex h-12 w-12 shrink-0 items-center justify-center rounded-xl bg-gradient-to-br from-brand-500 to-accent-500 text-white shadow-lg">
                  <m.icon className="h-6 w-6" />
                </span>
                <div>
                  <div className="text-sm font-semibold text-slate-500 dark:text-slate-400">{m.label}</div>
                  <div className="mt-0.5 font-semibold text-slate-900 dark:text-white">{m.value}</div>
                  <div className="mt-0.5 text-xs text-slate-400">{m.hint}</div>
                </div>
              </motion.div>
            ))}

            <motion.div variants={fadeUp} custom={3} initial="hidden" whileInView="show" viewport={{ once: true }} className="card flex items-center gap-4 p-5">
              <span className="flex h-12 w-12 shrink-0 items-center justify-center rounded-xl bg-slate-100 text-slate-500 dark:bg-slate-800 dark:text-slate-300">
                <FiClock className="h-6 w-6" />
              </span>
              <div>
                <div className="font-semibold text-slate-900 dark:text-white">Business hours</div>
                <div className="mt-0.5 text-sm text-slate-500">Monday–Friday · 9:00am – 6:00pm ET</div>
                <div className="mt-3 flex gap-2">
                  {[FiLinkedin, FiTwitter, FiGithub].map((Icon, k) => (
                    <a key={k} href="#" aria-label="Social link" className="flex h-8 w-8 items-center justify-center rounded-lg border border-slate-200 text-slate-500 transition-colors hover:border-brand-300 hover:bg-brand-50 hover:text-brand-600 dark:border-slate-700 dark:hover:bg-slate-800">
                      <Icon className="h-4 w-4" />
                    </a>
                  ))}
                </div>
              </div>
            </motion.div>
          </div>

          {/* right: form */}
          <motion.div variants={fadeUp} custom={1} initial="hidden" whileInView="show" viewport={{ once: true }} className="card p-6 sm:p-8">
            <h2 className="text-xl font-bold text-slate-900 dark:text-white">Send us a message</h2>
            <p className="mt-1 text-sm text-slate-500">Fill in the form and we&apos;ll get back to you shortly.</p>

            <form onSubmit={handleSubmit(onSubmit)} className="mt-6 space-y-4" noValidate>
              <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
                <div>
                  <label className="label" htmlFor="name">Name</label>
                  <input id="name" className="input" placeholder="Your name" {...register('name', { required: 'Name is required' })} />
                  {errors.name && <p className="mt-1 text-xs text-red-600">{errors.name.message}</p>}
                </div>
                <div>
                  <label className="label" htmlFor="email">Email</label>
                  <input id="email" type="email" className="input" placeholder="you@example.com"
                    {...register('email', { required: 'Email is required', pattern: { value: /^\S+@\S+$/, message: 'Enter a valid email' } })} />
                  {errors.email && <p className="mt-1 text-xs text-red-600">{errors.email.message}</p>}
                </div>
              </div>
              <div>
                <label className="label" htmlFor="subject">Subject</label>
                <input id="subject" className="input" placeholder="How can we help?" {...register('subject', { required: 'Subject is required' })} />
                {errors.subject && <p className="mt-1 text-xs text-red-600">{errors.subject.message}</p>}
              </div>
              <div>
                <label className="label" htmlFor="message">Message</label>
                <textarea id="message" className="input min-h-[140px]" placeholder="Tell us a bit more…" {...register('message', { required: 'Message is required' })} />
                {errors.message && <p className="mt-1 text-xs text-red-600">{errors.message.message}</p>}
              </div>
              <button type="submit" className="btn-primary w-full btn-lg" disabled={isSubmitting}>
                {isSubmitting ? 'Sending…' : <>Send message <FiSend /></>}
              </button>
            </form>
          </motion.div>
        </div>
      </section>
    </div>
  );
}

/* =========================================================================
   404
   ========================================================================= */
export function NotFound() {
  return (
    <div className="relative flex min-h-[70vh] flex-col items-center justify-center overflow-hidden px-4 text-center">
      <Blobs />
      <div className="text-8xl font-extrabold text-gradient">404</div>
      <h1 className="mt-4 text-2xl font-bold text-slate-900 dark:text-white">Page not found</h1>
      <p className="mt-2 text-slate-500">The page you&apos;re looking for doesn&apos;t exist or has moved.</p>
      <Link to="/" className="btn-primary mt-6"><FiArrowLeft /> Back home</Link>
    </div>
  );
}
