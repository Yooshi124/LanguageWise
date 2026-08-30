import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'

export default defineConfig({
  base: '/quizzes-and-courses/',
  plugins: [vue()],
  server: {
    port: 3003,
    proxy: {
      '/api': 'http://localhost:8080',
    },
  },
})
