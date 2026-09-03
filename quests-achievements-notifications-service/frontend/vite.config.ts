import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'
import { federation } from '@module-federation/vite'

export default defineConfig({
  base: '/',
  publicDir: 'images',
  plugins: [
    vue(),
    federation({
      name: 'quests_achievements',
      filename: 'remoteEntry.js',
      publicPath: '/remotes/quests-achievements/',
      exposes: { './feature': './src/federation/feature.ts' },
      shared: {
        vue: { singleton: true, strictVersion: true, requiredVersion: '3.5.42' },
        'vue-router': { singleton: true, strictVersion: true, requiredVersion: '4.6.4' },
        '@mdi/js': { singleton: true, strictVersion: true, requiredVersion: '7.4.47' },
      },
      bundleAllCSS: false,
      dts: false,
    }),
  ],
  server: {
    port: 3004,
    proxy: {
      '/quests-and-achievements/api': {
        target: 'http://localhost:5004',
        rewrite: (path) => path.replace(/^\/quests-and-achievements/, ''),
      },
    },
  },
  build: { target: 'esnext' },
})