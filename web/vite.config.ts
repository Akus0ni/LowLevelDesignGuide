import tailwindcss from '@tailwindcss/vite';
import react from '@vitejs/plugin-react';
import type { Plugin } from 'vite';
import { defineConfig } from 'vitest/config';

/**
 * `vite build --mode maui` produces the bundle packaged into Lld.App.
 * Only that build loads the HybridWebView bridge script, which exists only inside the app.
 */
function hybridWebViewBridge(mode: string): Plugin {
  return {
    name: 'lld-hybridwebview-bridge',
    transformIndexHtml: (html) =>
      mode === 'maui'
        ? html.replace('<head>', '<head>\n    <script src="_framework/hybridwebview.js"></script>')
        : html,
  };
}

// https://vite.dev/config/
export default defineConfig(({ mode }) => ({
  plugins: [react(), tailwindcss(), hybridWebViewBridge(mode)],
  // Relative asset URLs so the app bundle works from the WebView's virtual origin.
  base: mode === 'maui' ? './' : '/',
  server: {
    proxy: {
      // Aspire injects the API endpoint; the proxy avoids CORS in browser dev mode.
      '/api': {
        target: process.env.API_HTTPS || process.env.API_HTTP,
        changeOrigin: true,
      },
    },
  },
  test: {
    environment: 'jsdom',
    setupFiles: ['./src/test/setup.ts'],
    include: ['src/**/*.test.{ts,tsx}', 'scripts/**/*.test.mjs'],
  },
}));
