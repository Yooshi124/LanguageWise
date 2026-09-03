import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'
import { federation } from '@module-federation/vite'

export default defineConfig({
  base: '/quizzes-and-courses/',
  plugins: [
    vue(),
    federation({
      name: 'quizzes_courses',
      filename: 'remoteEntry.js',
      publicPath: '/remotes/quizzes-courses/',
      exposes: {
        './feature': './src/federation/feature.ts',
      },
      shared: {
        vue: { singleton: true, requiredVersion: '3.5.42', strictVersion: true },
        'vue-router': { singleton: true, requiredVersion: '4.6.4', strictVersion: true },
        vuetify: { singleton: true, requiredVersion: '3.13.2', strictVersion: true },
        '@mdi/js': { singleton: true, requiredVersion: '7.4.47', strictVersion: true },
      },
      bundleAllCSS: false,
      dts: false,
    }),
  ],
  server: {
    port: 3003,
    proxy: {
      '/quizzes-and-courses/api': {
        target: 'http://localhost:8080',
        rewrite: (path) => path.replace(/^\/quizzes-and-courses/, ''),
      },
    },
  },
})
