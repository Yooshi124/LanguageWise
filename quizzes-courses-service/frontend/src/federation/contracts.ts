import type { Component } from 'vue'
import type { RouteLocationNormalizedLoaded } from 'vue-router'

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

export interface AssistantToolResult {
  tool: string
  arguments?: Record<string, unknown>
  isError: boolean
  result: unknown
}

export interface AssistantToolView {
  summary?: string
  rows: { primary: string; secondary?: string }[]
}

export interface FeatureAssistantTool {
  tool: string
  label: string
  arguments: (route: RouteLocationNormalizedLoaded) => Record<string, unknown>
  unavailable?: (route: RouteLocationNormalizedLoaded) => string | null
}

export interface FeatureAssistant {
  apiBase: string
  welcome: string
  placeholder: string
  suggestions: (route: RouteLocationNormalizedLoaded) => readonly string[]
  context: (route: RouteLocationNormalizedLoaded) => Record<string, unknown>
  tools?: {
    chips: readonly FeatureAssistantTool[]
    view: (result: AssistantToolResult) => AssistantToolView
  }
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
  assistant?: FeatureAssistant
}