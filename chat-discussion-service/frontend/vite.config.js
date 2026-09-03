import { defineConfig } from 'vite';
import vue from '@vitejs/plugin-vue';
import { federation } from '@module-federation/vite';

export default defineConfig({
    base: '/chat-discussion/',
    plugins: [
        vue(),
        federation({
            name: 'chat_discussion',
            filename: 'remoteEntry.js',
            publicPath: '/remotes/chat-discussion/',
            exposes: {
                './feature': './src/federation/feature.js'
            },
            shared: {
                vue: { singleton: true, strictVersion: true, requiredVersion: '3.5.42' },
                'vue-router': { singleton: true, strictVersion: true, requiredVersion: '4.6.4' },
                '@mdi/js': { singleton: true, strictVersion: true, requiredVersion: '7.4.47' }
            },
            runtimePlugins: [],
            bundleAllCSS: false,
            dts: false
        })
    ],
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
        emptyOutDir: true,
        target: 'esnext'
    }
});
