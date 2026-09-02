import { afterEach, describe, expect, it, vi } from 'vitest'

vi.mock('./federation/quizzesCoursesRemote', () => ({
  isQuizzesCoursesPath: () => false,
  quizzesCoursesRoutesReady: () => false,
  registerQuizzesCoursesRoutes: vi.fn(),
}))

afterEach(() => {
  vi.unstubAllGlobals()
})

describe('router authentication', () => {
  it('redirects the compatibility login URL to the canonical route', async () => {
    vi.resetModules()
    const { default: router } = await import('./router')

    await router.push('/login.html?returnUrl=/analytics#account')

    expect(router.currentRoute.value.fullPath).toBe('/login?returnUrl=/analytics#account')
  })

  it('redirects a signed-out protected route and preserves its URL', async () => {
    vi.resetModules()
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(new Response(null, { status: 401 })))
    const { default: router } = await import('./router')
    router.addRoute({
      path: '/protected',
      name: 'protected-test',
      component: { template: '<div />' },
      meta: { requiresAuth: true },
    })

    await router.push('/protected?tab=progress')

    expect(router.currentRoute.value.name).toBe('signed-out')
    expect(router.currentRoute.value.query.returnUrl).toBe('/protected?tab=progress')
  })

  it('does not enter a protected route when session verification fails', async () => {
    vi.resetModules()
    vi.stubGlobal('fetch', vi.fn().mockRejectedValue(new Error('Network unavailable')))
    const { default: router } = await import('./router')
    router.addRoute({
      path: '/protected-error',
      name: 'protected-error-test',
      component: { template: '<div />' },
      meta: { requiresAuth: true },
    })

    await router.push('/protected-error')

    expect(router.currentRoute.value.name).toBe('home')
  })
})