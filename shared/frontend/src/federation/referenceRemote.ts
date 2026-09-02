import { loadRemote, registerRemotes } from '@module-federation/runtime'
import type { Component } from 'vue'
import type { Router, RouteRecordRaw } from 'vue-router'
import { useAuth } from '../composables/useAuth'
import RemoteUnavailableView from '../views/RemoteUnavailableView.vue'

const basePath = '/federation-spike'
const remoteEntryPath = '/remotes/quizzes-courses/remoteEntry.js'

function referenceRemote(force: boolean) {
  return {
    name: 'quizzes_courses',
    alias: 'quizzesCourses',
    entry: force ? `${remoteEntryPath}?retry=${Date.now()}` : remoteEntryPath,
    type: 'module' as const,
  }
}
let routesRegistered = false
let fallbackRegistered = false
let removeFallback: (() => void) | undefined
let registration: Promise<void> | undefined
let remoteRegistered = false

interface ReferenceRemoteModule {
  metadata: {
    key: string
    displayName: string
    icon: string
    basePath: string
    requiresAuth: boolean
  }
  routes: readonly {
    path: string
    name: string
    component: Component
  }[]
}

function hostContext(router: Router) {
  const auth = useAuth()

  return {
    user: null,
    navigate: async (path: string) => {
      await router.push(path)
    },
    signIn: (returnUrl = router.currentRoute.value.fullPath) => {
      const url = new URL('/login.html', window.location.origin)
      url.searchParams.set('returnUrl', returnUrl)
      window.location.assign(url)
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
    name: 'federation-reference-unavailable',
    component: RemoteUnavailableView,
    props: {
      featureName: 'Federation reference remote',
      retry: async () => {
        await registerReferenceRoutes(router, true)
        await router.replace({
          path: router.currentRoute.value.fullPath,
          force: true,
        })
      },
    },
  })
  fallbackRegistered = true
}

export function isReferencePath(path: string) {
  return path === basePath || path.startsWith(`${basePath}/`)
}

export function referenceRoutesReady() {
  return routesRegistered
}

export async function registerReferenceRoutes(router: Router, forceRemote = false) {
  if (routesRegistered) return
  if (registration) return registration

  registration = (async () => {
    try {
      if (!remoteRegistered) {
        registerRemotes([referenceRemote(false)])
        remoteRegistered = true
      } else if (forceRemote) {
        registerRemotes([referenceRemote(true)], { force: true })
      }

      const remote = await loadRemote<ReferenceRemoteModule>('quizzesCourses/reference')
      if (!remote) {
        throw new Error('The federation reference remote returned no module.')
      }
      const context = hostContext(router)

      removeFallback?.()
      removeFallback = undefined
      fallbackRegistered = false

      for (const route of remote.routes) {
        const path = route.path ? `${remote.metadata.basePath}/${route.path}` : remote.metadata.basePath
        router.addRoute({
          path,
          name: route.name,
          component: route.component,
          props: { hostContext: context },
          meta: {
            federatedFeature: remote.metadata.key,
            requiresAuth: remote.metadata.requiresAuth,
          },
        } as RouteRecordRaw)
      }

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