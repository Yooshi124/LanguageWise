import { fileURLToPath, URL } from 'node:url';
import { defineConfig } from 'vite';
import vue from '@vitejs/plugin-vue';
import { federation } from '@module-federation/vite';

// Base path under which the shared-frontend gateway serves this app. The
// Dockerfile sets VITE_BASE=/mini-games/ for the gateway build; plain
// `npm run dev` keeps '/' so local development is unaffected.
const base = process.env.VITE_BASE ?? '/';

export default defineConfig({
    base,
    plugins: [
        vue(),
        federation({
            name: 'mini_games',
            filename: 'remoteEntry.js',
            publicPath: '/remotes/mini-games/',
            exposes: {
                './feature': './src/federation/feature.js'
            },
            shared: {
                vue: { singleton: true, requiredVersion: '3.5.42', strictVersion: true },
                'vue-router': { singleton: true, requiredVersion: '4.6.4', strictVersion: true },
                '@mdi/js': { singleton: true, requiredVersion: '7.4.47', strictVersion: true }
            },
            bundleAllCSS: false,
            dts: false
        })
    ],
    resolve: {
        alias: {
            '@': fileURLToPath(new URL('./src', import.meta.url))
        }
    },
    server: {
        proxy: {
            '/mini-games/api': {
                target: 'http://localhost:5001',
                rewrite: (path) => path.replace(/^\/mini-games/, '')
            }
        }
    },
    build: {
        outDir: 'dist',
        emptyOutDir: true
    }
});