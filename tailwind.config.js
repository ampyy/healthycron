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
        navy:         '#080B10',
        'navy-light': '#0E1420',
        'navy-lighter':'#141B27',
        blue:          '#5B8EFF',
        purple:        '#A78BFA',
        'text-primary':'#FFFFFF',
        'text-secondary':'#94A3B8',
        'border-color': '#1E293B',
        cyan:          '#22D3EE',
        emerald:       '#34D399',
      },
      fontFamily: {
        sans: ['Inter', 'system-ui', 'sans-serif']
      }
    },
  },
  plugins: [],
}

