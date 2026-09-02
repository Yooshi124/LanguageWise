import { createRouter, createWebHistory } from 'vue-router'
import HomeView from './views/HomeView.vue'
import LoginView from './views/LoginView.vue'
import {
  isReferencePath,
  referenceRoutesReady,
  registerReferenceRoutes,
} from './federation/referenceRemote'

const router = createRouter({
  history: createWebHistory(),
  routes: [
    { path: '/', name: 'home', component: HomeView },
    { path: '/index.html', redirect: '/' },
    { path: '/login.html', name: 'login', component: LoginView },
  ],
  scrollBehavior: () => ({ top: 0 }),
})

router.beforeEach(async (to) => {
  if (!isReferencePath(to.path) || referenceRoutesReady()) {
    return true
  }

  try {
    await registerReferenceRoutes(router)
    return { path: to.fullPath, replace: true }
  } catch {
    if (to.name === 'federation-reference-unavailable') {
      return true
    }
    return { path: to.fullPath, replace: true }
  }
})

export default router
