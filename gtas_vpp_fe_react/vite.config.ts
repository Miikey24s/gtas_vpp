import { defineConfig, loadEnv } from 'vite'
import react from '@vitejs/plugin-react'
import tailwindcss from '@tailwindcss/vite'
import { fileURLToPath, URL } from 'node:url'

// https://vite.dev/config/
export default defineConfig(({ mode }) => {
  const env = loadEnv(mode, process.cwd(), '')
  const backendTarget =
    process.env.BACKEND_HTTPS ??
    process.env.BACKEND_HTTP ??
    env.GTAS_API_PROXY_TARGET

  return {
    plugins: [react(), tailwindcss()],
    resolve: {
      alias: {
        '@': fileURLToPath(new URL('./src', import.meta.url)),
      },
    },
    server: {
      host: '0.0.0.0',
      proxy: backendTarget
        ? {
            '/api': {
              target: backendTarget,
              changeOrigin: true,
              secure: false,
            },
            '/hubs': {
              target: backendTarget,
              changeOrigin: true,
              secure: false,
              ws: true,
            },
          }
        : undefined,
    },
  }
})
