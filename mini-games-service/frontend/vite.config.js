import { fileURLToPath, URL } from 'node:url';
import { defineConfig } from 'vite';
import vue from '@vitejs/plugin-vue';

// Base path under which the shared-frontend gateway serves this app. The
// Dockerfile sets VITE_BASE=/mini-games/ for the gateway build; plain
// `npm run dev` keeps '/' so local development is unaffected.
const base = process.env.VITE_BASE ?? '/';

export default defineConfig({
    base,
    plugins: [vue()],
    resolve: {
        alias: {
            '@': fileURLToPath(new URL('./src', import.meta.url))
        }
    },
    server: {
        proxy: {
            '/api': 'http://localhost:5001'
        }
    },
    build: {
        outDir: 'dist',
        emptyOutDir: true
    }
});