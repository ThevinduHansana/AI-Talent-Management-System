import { motion } from 'framer-motion';
import { FiCheckCircle } from 'react-icons/fi';
import Logo from './Logo';

/**
 * Branded left panel shared by the Login and Register screens.
 * Hidden on small screens; the form sits alongside on lg+.
 */
export default function AuthAside({
  heading = 'Hire smarter. Get hired faster.',
  points = [
    'AI-powered candidate screening & ranking',
    'Personalised job recommendations',
    'End-to-end hiring in one platform',
  ],
  logoSize = 'md',
}) {
  return (
    <div className="relative hidden overflow-hidden bg-gradient-to-br from-brand-700 via-brand-600 to-accent-600 lg:flex lg:flex-col lg:justify-between lg:p-12">
      <div className="pointer-events-none absolute -right-20 -top-20 h-72 w-72 rounded-full bg-white/10 blur-2xl animate-blob" />
      <div className="pointer-events-none absolute -bottom-24 -left-16 h-80 w-80 rounded-full bg-white/10 blur-2xl animate-blob" style={{ animationDelay: '4s' }} />

      <div className="relative">
        <span className="[&_span]:!text-white"><Logo size={logoSize} /></span>
      </div>

      <motion.div
        initial={{ opacity: 0, y: 20 }}
        animate={{ opacity: 1, y: 0 }}
        transition={{ duration: 0.6 }}
        className="relative"
      >
        <h2 className="max-w-sm text-3xl font-bold leading-tight text-white">{heading}</h2>
        <ul className="mt-8 space-y-4">
          {points.map((p) => (
            <li key={p} className="flex items-center gap-3 text-white/90">
              <FiCheckCircle className="h-5 w-5 shrink-0 text-accent-200" /> {p}
            </li>
          ))}
        </ul>
      </motion.div>

      <p className="relative text-sm text-white/70">
        © {new Date().getFullYear()} GetCareers — AI-Powered Recruitment.
      </p>
    </div>
  );
}
