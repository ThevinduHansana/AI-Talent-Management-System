import { FiZap } from 'react-icons/fi';

/**
 * GetCareers wordmark + icon. Sizes: sm | md | lg. `iconOnly` renders just the mark.
 */
export default function Logo({ size = 'md', iconOnly = false, className = '' }) {
  const dims = {
    sm: { box: 'h-8 w-8 text-base rounded-lg', text: 'text-lg' },
    md: { box: 'h-9 w-9 text-lg rounded-xl', text: 'text-xl' },
    lg: { box: 'h-11 w-11 text-xl rounded-2xl', text: 'text-2xl' },
    xl: { box: 'h-16 w-16 text-3xl rounded-2xl', text: 'text-4xl' },
  }[size];

  return (
    <span className={`flex items-center gap-2.5 ${className}`}>
      <span
        className={`relative flex items-center justify-center ${dims.box} bg-gradient-to-br from-brand-600 to-accent-500 text-white shadow-[0_6px_16px_-4px_rgba(79,70,229,0.5)]`}
      >
        <FiZap className="h-1/2 w-1/2" />
      </span>
      {!iconOnly && (
        <span className={`font-display font-extrabold tracking-tight ${dims.text} text-slate-900 dark:text-white`}>
          Get<span className="text-gradient">Careers</span>
        </span>
      )}
    </span>
  );
}
