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
        navy: '#0D1117',
        'navy-light': '#161B22',
        'navy-lighter': '#1C2128',
        blue: '#4F94FF',
        purple: '#9B5DE5',
        'text-primary': '#FFFFFF',
        'text-secondary': '#8B949E',
        'border-color': '#30363D'
      },
      fontFamily: {
        sans: ['Inter', 'system-ui', 'sans-serif']
      }
    },
  },
  plugins: [],
}

