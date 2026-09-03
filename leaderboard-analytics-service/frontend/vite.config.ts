import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'
import { federation } from '@module-federation/vite'

export default defineConfig({
  plugins: [
    vue(),
    federation({
      name: 'leaderboard_analytics',
      filename: 'remoteEntry.js',
      publicPath: '/remotes/leaderboard-analytics/',
      exposes: { './feature': './src/federation/feature.ts' },
      shared: {
        vue: { singleton: true, strictVersion: true, requiredVersion: '3.5.42' },
        'vue-router': { singleton: true, strictVersion: true, requiredVersion: '4.6.4' },
        '@tanstack/vue-query': { singleton: true, strictVersion: true, requiredVersion: '5.102.8' },
      },
      bundleAllCSS: false,
      dts: false,
    }),
  ],
  base: '/',
  build: { target: 'esnext' },
})
