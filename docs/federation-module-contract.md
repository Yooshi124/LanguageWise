# Federation Module Contract

This contract is the template for every LanguageWise feature remote. The Shared
frontend is the only application host. A remote contributes feature routes and
metadata; it does not create another application shell.

## Pinned runtime

All hosts and remotes use exact compatible versions:

| Package | Version |
| --- | --- |
| `@module-federation/vite` | `1.21.2` |
| `@module-federation/runtime` (host) | `2.9.0` |
| `vue` | `3.5.42` |
| `vue-router` | `4.6.4` |
| `vuetify` | `3.13.2` |
| `@mdi/js` | `7.4.47` |

Vue, Vue Router, Vuetify, and `@mdi/js` must be configured with
`singleton: true`, `strictVersion: true`, and the exact `requiredVersion` in
both host and remote federation configurations. TanStack Vue Query `5.102.8`
will follow the same singleton rule when Analytics is migrated. Highcharts
remains feature-local.

## Exposed module

Each remote exposes one module whose named exports satisfy these interfaces:

```ts
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
}

export interface FederatedFeatureModule {
  metadata: {
    key: string
    displayName: string
    icon: string
    basePath: string
    requiresAuth: boolean
  }
  routes: readonly FeatureRouteDefinition[]
}
```

The module exports `metadata` and `routes`. Route paths are relative to
`metadata.basePath`: use `''` for the feature root and a segment such as
`'details'` for a child. Route names and metadata keys must be globally unique.
The host converts these definitions into Vue Router records and supplies
`FeatureHostContext` as the root component prop.

The Phase 1 reference implementation is
`quizzes-courses-service/frontend/src/federation/reference.ts`; its contract
types are in the adjacent `contracts.ts`.

## Ownership rules

The Shared host owns:

- `createApp`, the only Vue Router and Vuetify installation, and `v-app`;
- shell navigation, authentication state, route guards, and host actions;
- top-level loading, timeout, retry, and error views;
- all authored CSS and the visual system.

A federated feature must not create an app, install a router or Vuetify, render
a second shell, perform its own session check, or emit authored CSS. It may use
host-provided singleton components and composables. A standalone entry may
remain for local development only when it adapts the same feature components.

## Loading and serving

The host registers each remote with `@module-federation/runtime` only when a
route under that feature's base path is entered, then calls `loadRemote`.
Do not add feature remotes to the host plugin's static `remotes` option:
`@module-federation/vite` 1.21.2 preloads those entries during host bootstrap,
which defeats route-level laziness.

Each remote must set its production public path to
`/remotes/<feature>/`. Shared nginx proxies that namespace to the owning
frontend container. Serve:

- `remoteEntry.js` with no-store/no-cache revalidation headers;
- hashed chunks and assets with a one-year immutable cache policy.

Remote failure must install a host-owned, feature-specific fallback without
breaking the shell or other routes. Retry force-registers a cache-busted remote
entry URL because browsers retain failed module fetches for a document's
lifetime. Successful retry rematches the current route without reloading the
document.

## Migration checklist

For every new remote:

1. Pin the common versions and strict singleton configuration.
2. Expose `metadata` and relative `routes` through `remoteEntry.js`.
3. Set `/remotes/<feature>/` as the production public path and add nginx proxy/cache rules.
4. Register and load the remote only from its host route boundary.
5. Keep all authored feature CSS in the Shared host namespace.
6. Verify Home makes no request to the remote namespace.
7. Verify direct child-route refresh and client-side child/back/forward navigation.
8. Stop the remote, verify isolated fallback, restore it, and retry without a document reload.
9. Verify every remote chunk/asset returns `200` from its namespace and no remote stylesheet loads.
10. Build and test the production Docker images, not only Vite development servers.
