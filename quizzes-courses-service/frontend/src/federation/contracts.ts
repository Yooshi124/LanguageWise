import type { Component } from 'vue'

export interface AuthenticatedUser {
  id: number
  name: string
}

export interface FeatureHostContext {
  user: AuthenticatedUser | null
  navigate: (path: string) => Promise<void>
  signIn: (returnUrl?: string) => void
  signOut: () => Promise<void>
}

export interface FeatureRouteDefinition {
  path: string
  name: string
  component: Component
  props?: Record<string, unknown>
  meta?: Record<string, unknown>
}

export interface FederatedFeatureModule {
  QuizzesCoursesComponent: Component
  metadata: {
    key: string
    displayName: string
    icon: string
    basePath: string
    requiresAuth: boolean
  }
  routes: readonly FeatureRouteDefinition[]
}