import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

// https://vite.dev/config/
export default defineConfig({
  plugins: [react()],
  server: {
    proxy: {
      '/diagnostic/health': {
        target: 'http://localhost:5000',
        changeOrigin: true,
      },
      '/diagnostic/config': {
        target: 'http://localhost:5000',
        changeOrigin: true,
      },
      '/diagnostic/uptime': {
        target: 'http://localhost:5000',
        changeOrigin: true,
      },
      '/diagnostic/analytics': {
        target: 'http://localhost:5000',
        changeOrigin: true,
      },
      '/diagnostic/sentiment-status': {
        target: 'http://localhost:5000',
        changeOrigin: true,
      },
      '/hubs': {
        target: 'http://localhost:5000',
        changeOrigin: true,
        ws: true,
      },
      '/api': {
        target: 'http://localhost:5000',
        changeOrigin: true,
      },
    },
  },
})
