/** @type {import('tailwindcss').Config} */
module.exports = {
  content: ["./index.html", "./src-ui/**/*.{ts,tsx}"],
  theme: {
    extend: {
      colors: {
        brand: {
          50: "#eff6ff",
          100: "#dbeafe",
          200: "#bfdbfe",
          300: "#93c5fd",
          400: "#60a5fa",
          500: "#2563eb",
          600: "#1d4ed8",
          700: "#1e40af",
          800: "#1e3a8a",
          900: "#172554"
        },
        slate: {
          25: "#fcfdff"
        }
      },
      boxShadow: {
        shell: "0 20px 48px rgba(15, 23, 42, 0.14)",
        card: "0 18px 42px rgba(15, 23, 42, 0.10)"
      },
      fontFamily: {
        sans: ["Inter", "Segoe UI", "ui-sans-serif", "system-ui", "sans-serif"],
        mono: ["JetBrains Mono", "ui-monospace", "monospace"]
      },
      backgroundImage: {
        "frost-grid":
          "linear-gradient(rgba(255,255,255,0.24) 1px, transparent 1px), linear-gradient(90deg, rgba(255,255,255,0.24) 1px, transparent 1px)"
      }
    }
  },
  plugins: []
};
