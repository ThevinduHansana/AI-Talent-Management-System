/**
 * Hero artwork for the landing page: a stylised "AI candidate ranking" dashboard.
 *
 * Drawn inline as vector rather than shipped as a raster so it stays crisp on any
 * display, weighs almost nothing, carries no stock-photo licensing, and can follow
 * the light/dark theme through Tailwind `fill-*` utilities.
 */

/** Match scores shown on the mocked candidate rows, highest first. */
const ROWS = [
  { score: 96, accent: true },
  { score: 92, accent: false },
  { score: 88, accent: false },
  { score: 84, accent: false },
];

const TRACK_X = 250;
const TRACK_W = 150;

export default function HeroIllustration({ className = '' }) {
  return (
    <svg
      viewBox="0 0 560 440"
      className={className}
      role="img"
      aria-label="Candidate shortlist ranked by AI match score"
      xmlns="http://www.w3.org/2000/svg"
    >
      <defs>
        <linearGradient id="hero-brand" x1="0" y1="0" x2="1" y2="1">
          <stop offset="0%" stopColor="#4f46e5" />
          <stop offset="100%" stopColor="#6366f1" />
        </linearGradient>
        <linearGradient id="hero-accent" x1="0" y1="0" x2="1" y2="1">
          <stop offset="0%" stopColor="#06b6d4" />
          <stop offset="100%" stopColor="#22d3ee" />
        </linearGradient>
        <linearGradient id="hero-glow" x1="0" y1="0" x2="1" y2="1">
          <stop offset="0%" stopColor="#6366f1" stopOpacity="0.18" />
          <stop offset="100%" stopColor="#06b6d4" stopOpacity="0.18" />
        </linearGradient>
        <filter id="hero-shadow" x="-20%" y="-20%" width="140%" height="140%">
          <feDropShadow dx="0" dy="12" stdDeviation="18" floodColor="#0f172a" floodOpacity="0.13" />
        </filter>
      </defs>

      {/* soft brand wash behind the window */}
      <rect x="16" y="20" width="528" height="400" rx="32" fill="url(#hero-glow)" />

      {/* ---- app window ---- */}
      <g filter="url(#hero-shadow)">
        <rect
          x="40" y="30" width="480" height="380" rx="24"
          className="fill-white dark:fill-slate-900"
        />
        <rect
          x="40" y="30" width="480" height="380" rx="24"
          fill="none" strokeWidth="1.5"
          className="stroke-slate-200 dark:stroke-slate-700"
        />
      </g>

      {/* ---- title bar ---- */}
      <circle cx="70" cy="60" r="5" className="fill-slate-300 dark:fill-slate-600" />
      <circle cx="88" cy="60" r="5" className="fill-slate-300 dark:fill-slate-600" />
      <circle cx="106" cy="60" r="5" className="fill-slate-300 dark:fill-slate-600" />
      <rect x="132" y="52" width="104" height="16" rx="8" className="fill-slate-200 dark:fill-slate-700" />
      <rect x="380" y="46" width="110" height="28" rx="14" className="fill-slate-100 dark:fill-slate-800" />
      <circle cx="398" cy="60" r="6" fill="none" strokeWidth="2" className="stroke-slate-400 dark:stroke-slate-500" />
      <line x1="403" y1="65" x2="408" y2="70" strokeWidth="2" strokeLinecap="round" className="stroke-slate-400 dark:stroke-slate-500" />
      <line x1="40" y1="90" x2="520" y2="90" strokeWidth="1.5" className="stroke-slate-200 dark:stroke-slate-700" />

      {/* ---- section heading ---- */}
      <rect x="68" y="108" width="96" height="12" rx="6" className="fill-slate-300 dark:fill-slate-600" />
      <rect x="424" y="106" width="66" height="16" rx="8" fill="url(#hero-brand)" opacity="0.9" />

      {/* ---- ranked candidate rows ---- */}
      {ROWS.map((row, i) => {
        const y = 142 + i * 62;
        const fill = row.accent ? 'url(#hero-accent)' : 'url(#hero-brand)';
        return (
          <g key={row.score}>
            {/* row highlight for the top match */}
            {i === 0 && (
              <rect x="56" y={y - 14} width="448" height="52" rx="14" className="fill-brand-50 dark:fill-brand-950/40" />
            )}

            {/* avatar */}
            <circle cx="92" cy={y + 12} r="18" fill={fill} opacity={row.accent ? 1 : 0.85 - i * 0.12} />

            {/* name + role placeholders */}
            <rect x="124" y={y + 1} width={112 - i * 8} height="11" rx="5.5" className="fill-slate-300 dark:fill-slate-600" />
            <rect x="124" y={y + 19} width={78 - i * 6} height="9" rx="4.5" className="fill-slate-200 dark:fill-slate-700" />

            {/* match score bar */}
            <rect x={TRACK_X} y={y + 8} width={TRACK_W} height="8" rx="4" className="fill-slate-200 dark:fill-slate-700" />
            <rect x={TRACK_X} y={y + 8} width={(TRACK_W * row.score) / 100} height="8" rx="4" fill={fill} />

            <text
              x="424" y={y + 17}
              className="fill-slate-500 dark:fill-slate-400"
              fontSize="13" fontWeight="700" fontFamily="inherit"
            >
              {row.score}%
            </text>
          </g>
        );
      })}

      {/* ---- footer action ---- */}
      <rect x="68" y="376" width="88" height="18" rx="9" className="fill-slate-200 dark:fill-slate-700" />
    </svg>
  );
}
