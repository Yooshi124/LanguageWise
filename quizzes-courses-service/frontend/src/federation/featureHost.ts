import type { FeatureHostContext } from './contracts'

let hostContext: FeatureHostContext | undefined

export function setFeatureHostContext(context: FeatureHostContext | undefined) {
  hostContext = context
}

export function handleUnauthorized() {
  if (hostContext) {
    void hostContext.signOut()
  }
}