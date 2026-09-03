import { loadRemote, registerRemotes } from '@module-federation/runtime'
import type { Component } from 'vue'
import type { Router, RouteRecordRaw } from 'vue-router'
import { useAuth } from '../composables/useAuth'
import RemoteUnavailableView from '../views/RemoteUnavailableView.vue'

interface FeatureRouteDefinition {
  path: string
  name: string
  component: Component
  props?: Record<string, unknown> | boolean
  meta?: Record<string, unknown>
}

interface FederatedFeatureModule {
  metadata: {
    key: string
    basePath: string
    requiresAuth: boolean
  }
  routes: readonly FeatureRouteDefinition[]
  [exportName: string]: unknown
}

interface RemoteRegistrationConfig {
  runtimeName: string
  alias: string
  entryPath: string
  moduleId: string
  componentExport: string
  basePath: string
  routeName: string
  featureName: string
}

export interface FederatedRemoteRegistration {
  fallbackRouteName: string
  matches: (path: string) => boolean
  ready: () => boolean
  register: (router: Router, forceRemote?: boolean) => Promise<void>
}

export function createRemoteRegistration(
  config: RemoteRegistrationConfig,
): FederatedRemoteRegistration {
  const fallbackRouteName = `${config.routeName}-unavailable`
  let routesRegistered = false
  let fallbackRegistered = false
  let removeFallback: (() => void) | undefined
  let registration: Promise<void> | undefined
  let remoteRegistered = false

  function definition(force: boolean) {
    return {
      name: config.runtimeName,
      alias: config.alias,
      entry: force ? `${config.entryPath}?retry=${Date.now()}` : config.entryPath,
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
      path: `${config.basePath}/:pathMatch(.*)*`,
      name: fallbackRouteName,
      component: RemoteUnavailableView,
      props: {
        featureName: config.featureName,
        retry: async () => {
          await register(router, true)
          await router.replace({ path: router.currentRoute.value.fullPath, force: true })
        },
      },
    })
    fallbackRegistered = true
  }

  async function register(router: Router, forceRemote = false) {
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

        const remote = await loadRemote<FederatedFeatureModule>(config.moduleId)
        const component = remote?.[config.componentExport] as Component | undefined
        if (!remote || !component) {
          throw new Error(`${config.featureName} returned an invalid remote module.`)
        }

        removeFallback?.()
        removeFallback = undefined
        fallbackRegistered = false
        router.addRoute({
          path: remote.metadata.basePath,
          name: config.routeName,
          component,
          props: { hostContext: hostContext(router) },
          meta: {
            federatedFeature: remote.metadata.key,
            requiresAuth: remote.metadata.requiresAuth,
          },
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

  return {
    fallbackRouteName,
    matches: (path) => path === config.basePath || path.startsWith(`${config.basePath}/`),
    ready: () => routesRegistered,
    register,
  }
}