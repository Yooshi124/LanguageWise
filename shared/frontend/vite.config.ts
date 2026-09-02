import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'
import { federation } from '@module-federation/vite'

export default defineConfig({
  plugins: [
    vue(),
    federation({
      name: 'languagewise_host',
      shared: {
        vue: { singleton: true, requiredVersion: '3.5.42', strictVersion: true },
        'vue-router': { singleton: true, requiredVersion: '4.6.4', strictVersion: true },
        vuetify: { singleton: true, requiredVersion: '3.13.2', strictVersion: true },
        '@mdi/js': { singleton: true, requiredVersion: '7.4.47', strictVersion: true },
      },
      disableSnapshot: true,
      dts: false,
    }),
  ],
  build: {
    modulePreload: false,
  },
  server: {
    port: 3000,
    proxy: {
      '/api': 'http://localhost:5000',
    },
  },
})
