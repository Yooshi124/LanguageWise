import { loadRemote, registerRemotes } from '@module-federation/runtime'
import type { Component } from 'vue'
import type { Router, RouteRecordRaw } from 'vue-router'
import { useAuth } from '../composables/useAuth'
import RemoteUnavailableView from '../views/RemoteUnavailableView.vue'

const basePath = '/chat-discussion'
const remoteEntryPath = '/remotes/chat-discussion/remoteEntry.js'

interface FeatureRouteDefinition {
  path: string
  name: string
  component: Component
  props?: Record<string, unknown> | boolean
  meta?: Record<string, unknown>
}

interface ChatDiscussionRemoteModule {
  ChatDiscussionComponent: Component
  metadata: {
    key: string
    displayName: string
    icon: string
    basePath: string
    requiresAuth: boolean
  }
  routes: readonly FeatureRouteDefinition[]
}

let routesRegistered = false
let fallbackRegistered = false
let removeFallback: (() => void) | undefined
let registration: Promise<void> | undefined
let remoteRegistered = false

function remoteDefinition(force: boolean) {
  return {
    name: 'chat_discussion',
    alias: 'chatDiscussion',
    entry: force ? `${remoteEntryPath}?retry=${Date.now()}` : remoteEntryPath,
    type: 'module' as const,
  }
}

function hostContext(router: Router) {
  const auth = useAuth()

  return {
    get user() {
      return auth.user.value
    },
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
    name: 'chat-discussion-unavailable',
    component: RemoteUnavailableView,
    props: {
      featureName: 'Discussion Forum',
      retry: async () => {
        await registerChatDiscussionRoutes(router, true)
        await router.replace({ path: router.currentRoute.value.fullPath, force: true })
      },
    },
  })
  fallbackRegistered = true
}

export function isChatDiscussionPath(path: string) {
  return path === basePath || path.startsWith(`${basePath}/`)
}

export function chatDiscussionRoutesReady() {
  return routesRegistered
}

export async function registerChatDiscussionRoutes(router: Router, forceRemote = false) {
  if (routesRegistered) return
  if (registration) return registration

  registration = (async () => {
    try {
      if (!remoteRegistered) {
        registerRemotes([remoteDefinition(false)])
        remoteRegistered = true
      } else if (forceRemote) {
        registerRemotes([remoteDefinition(true)], { force: true })
      }

      const remote = await loadRemote<ChatDiscussionRemoteModule>('chatDiscussion/feature')
      if (!remote) throw new Error('The Discussion Forum remote returned no module.')

      removeFallback?.()
      removeFallback = undefined
      fallbackRegistered = false
      router.addRoute({
        path: remote.metadata.basePath,
        name: 'chat-discussion',
        component: remote.ChatDiscussionComponent,
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