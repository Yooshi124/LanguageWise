import { createRouter, createWebHistory } from 'vue-router'
import HomeView from './views/HomeView.vue'
import LoginView from './views/LoginView.vue'
import SignedOutView from './views/SignedOutView.vue'
import { useAuth } from './composables/useAuth'
import { federatedRemotes } from './federation/remotes'

const router = createRouter({
  history: createWebHistory(),
  routes: [
    { path: '/', name: 'home', component: HomeView },
    { path: '/login', name: 'login', component: LoginView },
    { path: '/signed-out', name: 'signed-out', component: SignedOutView },
  ],
  scrollBehavior: () => ({ top: 0 }),
})

router.beforeEach(async (to) => {
  if (to.meta.requiresAuth) {
    try {
      if (!(await useAuth().ensureAuthenticated())) {
        return { name: 'signed-out', query: { returnUrl: to.fullPath } }
      }
    } catch {
      return { name: 'home' }
    }
  }

  for (const remote of federatedRemotes) {
    if (!remote.matches(to.path) || remote.ready()) continue
    if (to.name === remote.fallbackRouteName) return true

    try {
      await remote.register(router)
    } catch {
      // The registrar installs a host-owned fallback route.
    }
    return { path: to.path, query: to.query, hash: to.hash, replace: true }
  }

  return true
})

export default router
