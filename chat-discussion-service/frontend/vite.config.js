import { defineConfig } from 'vite';
import vue from '@vitejs/plugin-vue';

export default defineConfig({
    base: '/chat-discussion/',
    plugins: [vue()],
    server: {
        port: 3002,
        proxy: {
            '/chat-discussion/api': {
                target: 'http://localhost:5002',
                changeOrigin: false,
                rewrite: (path) => path.replace(/^\/chat-discussion/, '')
            },
            '/chat-discussion/shared-api': {
                target: 'http://localhost:5000',
                changeOrigin: false,
                rewrite: (path) => path.replace(/^\/chat-discussion\/shared-api/, '/api')
            }
        }
    },
    build: {
        outDir: 'dist',
        emptyOutDir: true
    }
});
