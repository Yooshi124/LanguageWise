import { afterEach, describe, expect, it, vi } from 'vitest'

vi.mock('./federation/remotes', () => ({
  federatedRemotes: [{
    fallbackRouteName: 'chat-discussion-unavailable',
    matches: (path: string) => path.startsWith('/chat-discussion'),
    ready: () => false,
    register: vi.fn((router) => {
      router.addRoute({
        path: '/chat-discussion',
        name: 'chat-discussion-test',
        component: { template: '<div />' },
        meta: { requiresAuth: true },
        children: [{
          path: 'posts/:id',
          name: 'chat-discussion-post-test',
          component: { template: '<div />' },
          meta: { requiresAuth: true },
        }],
      })
    }),
  }],
}))

afterEach(() => {
  vi.unstubAllGlobals()
})

describe('router authentication', () => {
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

  it('redirects a signed-out Discussion deep link after registering its protected routes', async () => {
    vi.resetModules()
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(new Response(null, { status: 401 })))
    const { default: router } = await import('./router')

    await router.push('/chat-discussion/posts/42?from=search')

    expect(router.currentRoute.value.name).toBe('signed-out')
    expect(router.currentRoute.value.query.returnUrl).toBe('/chat-discussion/posts/42?from=search')
  })
})