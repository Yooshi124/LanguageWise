import { createRouter, createWebHistory } from 'vue-router'
import { routes as featureRoutes } from './federation/feature'

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: featureRoutes.map((route) => ({
    ...route,
    path: route.path ? `/${route.path}` : '/',
  })),
  scrollBehavior: () => ({ top: 0 }),
})

export default router
