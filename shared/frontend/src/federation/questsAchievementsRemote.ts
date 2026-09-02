import { loadRemote, registerRemotes } from '@module-federation/runtime'
import type { Component } from 'vue'
import type { Router, RouteRecordRaw } from 'vue-router'
import { useAuth } from '../composables/useAuth'
import RemoteUnavailableView from '../views/RemoteUnavailableView.vue'

const basePath = '/quests-and-achievements'
const remoteEntryPath = '/remotes/quests-achievements/remoteEntry.js'

interface FeatureRouteDefinition {
  path: string
  name: string
  component: Component
  props?: Record<string, unknown> | boolean
  meta?: Record<string, unknown>
}

interface RemoteModule {
  QuestsAchievementsNotificationsComponent: Component
  metadata: { key: string; displayName: string; icon: string; basePath: string; requiresAuth: boolean }
  routes: readonly FeatureRouteDefinition[]
}

let routesRegistered = false
let fallbackRegistered = false
let removeFallback: (() => void) | undefined
let registration: Promise<void> | undefined
let remoteRegistered = false

function definition(force: boolean) {
  return {
    name: 'quests_achievements',
    alias: 'questsAchievements',
    entry: force ? `${remoteEntryPath}?retry=${Date.now()}` : remoteEntryPath,
    type: 'module' as const,
  }
}

function hostContext(router: Router) {
  const auth = useAuth()
  return {
    get user() { return auth.user.value },
    navigate: async (path: string) => router.push(path),
    signIn: (returnUrl = router.currentRoute.value.fullPath) => {
      void router.push({ path: '/login', query: { returnUrl } })
    },
    signOut: async () => {
      await auth.logout()
      await router.push('/')
    },
  }
}

function registerFallback(router: Router) {
  if (fallbackRegistered) return
  removeFallback = router.addRoute({
    path: `${basePath}/:pathMatch(.*)*`,
    name: 'quests-achievements-unavailable',
    component: RemoteUnavailableView,
    props: {
      featureName: 'Quests & Achievements',
      retry: async () => {
        await registerQuestsAchievementsRoutes(router, true)
        await router.replace({ path: router.currentRoute.value.fullPath, force: true })
      },
    },
  })
  fallbackRegistered = true
}

export function isQuestsAchievementsPath(path: string) {
  return path === basePath || path.startsWith(`${basePath}/`)
}

export function questsAchievementsRoutesReady() {
  return routesRegistered
}

export async function registerQuestsAchievementsRoutes(router: Router, forceRemote = false) {
  if (routesRegistered) return
  if (registration) return registration

  registration = (async () => {
    try {
      if (!remoteRegistered) {
        registerRemotes([definition(false)])
        remoteRegistered = true
      } else if (forceRemote) {
        registerRemotes([definition(true)], { force: true })
      }
      const remote = await loadRemote<RemoteModule>('questsAchievements/feature')
      if (!remote) throw new Error('The Quests & Achievements remote returned no module.')

      removeFallback?.()
      removeFallback = undefined
      fallbackRegistered = false
      router.addRoute({
        path: remote.metadata.basePath,
        name: 'quests-achievements',
        component: remote.QuestsAchievementsNotificationsComponent,
        props: { hostContext: hostContext(router) },
        meta: { federatedFeature: remote.metadata.key, requiresAuth: remote.metadata.requiresAuth },
        children: remote.routes.map((route) => ({
          path: route.path,
          name: route.name,
          component: route.component,
          props: route.props,
          meta: {
            ...route.meta,
            federatedFeature: remote.metadata.key,
            requiresAuth: remote.metadata.requiresAuth,
          },
        })),
      } as RouteRecordRaw)
      routesRegistered = true
    } catch (error) {
      registerFallback(router)
      throw error
    } finally {
      registration = undefined
    }
  })()
  return registration
}