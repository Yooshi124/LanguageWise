import { shallowReactive } from 'vue'
import type { RouteLocationNormalizedLoaded } from 'vue-router'

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
  arguments: (route: RouteLocationNormalizedLoaded) => Record<string, unknown> | null
  /** Why the tool cannot run on this page, or null when it can. */
  unavailable?: (route: RouteLocationNormalizedLoaded) => string | null
}

export interface FeatureAssistant {
  /** Prefix of the feature's `/assistant/*` endpoints, e.g. `/quizzes-and-courses/api`. */
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

export const featureAssistants = shallowReactive(new Map<string, FeatureAssistant>())
