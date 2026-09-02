import { afterEach, describe, expect, it, vi } from 'vitest'

function jsonResponse(body: unknown, status = 200) {
  return new Response(JSON.stringify(body), {
    status,
    headers: { 'Content-Type': 'application/json' },
  })
}

async function loadAuth() {
  vi.resetModules()
  return import('./useAuth')
}

afterEach(() => {
  vi.unstubAllGlobals()
})

describe('useAuth', () => {
  it('retains the authenticated user and performs one session check', async () => {
    const fetchMock = vi.fn().mockResolvedValue(jsonResponse({ id: 7, name: 'justin' }))
    vi.stubGlobal('fetch', fetchMock)
    const { ensureAuthenticated, useAuth } = await loadAuth()

    await Promise.all([ensureAuthenticated(), ensureAuthenticated()])
    await ensureAuthenticated()

    expect(fetchMock).toHaveBeenCalledTimes(1)
    expect(useAuth().user.value).toEqual({ id: 7, name: 'justin' })
    expect(useAuth().username.value).toBe('justin')
  })

  it('caches a signed-out result without repeating the session check', async () => {
    const fetchMock = vi.fn().mockResolvedValue(new Response(null, { status: 401 }))
    vi.stubGlobal('fetch', fetchMock)
    const { ensureAuthenticated, useAuth } = await loadAuth()

    expect(await ensureAuthenticated()).toBe(false)
    expect(await ensureAuthenticated()).toBe(false)

    expect(fetchMock).toHaveBeenCalledTimes(1)
    expect(useAuth().status.value).toBe('signed-out')
  })

  it('refreshes the complete identity after login', async () => {
    const fetchMock = vi
      .fn()
      .mockResolvedValueOnce(new Response(null, { status: 200 }))
      .mockResolvedValueOnce(jsonResponse({ id: 1, name: 'amber' }))
    vi.stubGlobal('fetch', fetchMock)
    const { login, useAuth } = await loadAuth()

    await login('amber', 'test')

    expect(fetchMock).toHaveBeenCalledTimes(2)
    expect(useAuth().user.value).toEqual({ id: 1, name: 'amber' })
  })

  it('uses the canonical login route and preserves the return URL', async () => {
    window.history.replaceState({}, '', '/analytics/?range=30#summary')
    const { loginUrl } = await loadAuth()

    expect(loginUrl()).toBe(
      'http://localhost:3000/login?returnUrl=%2Fanalytics%2F%3Frange%3D30%23summary',
    )
  })
})