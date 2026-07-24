import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import tailwindcss from '@tailwindcss/vite'

// The API base URL for the dev proxy; overridable via VITE_API_PROXY.
// Defaults to 5094 (the backend's launchSettings port) so `npm run dev` works with no env var.
const apiTarget = process.env.VITE_API_PROXY || 'http://localhost:5094'

// https://vite.dev/config/
export default defineConfig({
  plugins: [react(), tailwindcss()],
  server: {
    port: 5173,
    proxy: {
      // Proxy API calls to the ASP.NET backend to avoid CORS in development.
      '/api': {
        target: apiTarget,
        changeOrigin: true,
      },
    },
  },
})
