import type { RouteLocationRaw, RouteLocationResolvedGeneric } from 'vue-router'
import { useRoute, useRouter } from 'vue-router'

function internalRoute(
  value: unknown,
  resolve: (location: RouteLocationRaw) => RouteLocationResolvedGeneric,
  allowedRouteNames: readonly string[],
  courseCode: unknown,
) {
  if (typeof value !== 'string' || !value.startsWith('/') || value.startsWith('//')) return null

  const route = resolve(value)
  return route.matched.length &&
    typeof route.name === 'string' &&
    allowedRouteNames.includes(route.name) &&
    route.params.courseCode === courseCode
    ? route
    : null
}

export function useSafeBack(
  fallback: () => RouteLocationRaw,
  allowedRouteNames: readonly string[],
) {
  const route = useRoute()
  const router = useRouter()

  return function goBack() {
    const requestedRoute = internalRoute(
      route.query.returnTo,
      router.resolve,
      allowedRouteNames,
      route.params.courseCode,
    )
    const historyRoute = internalRoute(
      window.history.state?.back,
      router.resolve,
      allowedRouteNames,
      route.params.courseCode,
    )

    if (requestedRoute) {
      if (historyRoute?.fullPath === requestedRoute.fullPath) {
        router.back()
      } else {
        void router.push(requestedRoute)
      }
      return
    }

    if (historyRoute) {
      router.back()
    } else {
      void router.push(fallback())
    }
  }
}
