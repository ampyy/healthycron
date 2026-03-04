/** @type {import('tailwindcss').Config} */
module.exports = {
  content: [
    "./Views/**/*.cshtml",
    "./Views/**/*.html",
    "./wwwroot/**/*.js"
  ],
  theme: {
    extend: {
      colors: {
        // Core Palette
        'hc-black': '#0A0A0B',
        'hc-ink': '#111114',
        'hc-surface': '#18181C',
        'hc-border': '#27272D',
        'hc-border-mid': '#38383F',
        
        // Text
        'hc-white': '#FAFAFA',
        'hc-silver': '#D4D4D8',
        'hc-dim': '#A1A1AA',
        'hc-muted': '#52525B',

        // Signal System
        'hc-red': '#EF3340',
        'hc-red-deep': '#C41E27',
        'hc-red-subtle': '#2D1215',
        'hc-green': '#22C55E',
        'hc-green-subtle': '#0D2318',
        'hc-amber': '#F59E0B',
        'hc-amber-subtle': '#261D09',
        'hc-blue': '#3B82F6',
        'hc-blue-subtle': '#0D1B38',

        // Legacy mapping (to be phased out safely)
        navy:         '#0A0A0B',
        'navy-light': '#111114',
        'navy-lighter':'#18181C',
        blue:          '#3B82F6',
        purple:        '#a3a3a3',
        'text-primary':'#FAFAFA',
        'text-secondary':'#737373',
        'border-color': '#27272D',
        cyan:          '#06b6d4',
        emerald:       '#22C55E',
      },
      fontFamily: {
        sans: ['Sora', 'system-ui', 'sans-serif'],
        mono: ['DM Mono', 'monospace']
      }
    },
  },
  plugins: [],
}

