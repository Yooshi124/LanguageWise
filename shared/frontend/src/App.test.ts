import { flushPromises, shallowMount } from '@vue/test-utils'
import { createMemoryHistory, createRouter } from 'vue-router'
import { afterEach, describe, expect, it, vi } from 'vitest'
import App from './App.vue'

afterEach(() => {
  vi.unstubAllGlobals()
})

describe('App authentication boundary', () => {
  it('checks the session once when the host mounts', async () => {
    const fetchMock = vi.fn().mockResolvedValue(new Response(null, { status: 401 }))
    vi.stubGlobal('fetch', fetchMock)
    const router = createRouter({
      history: createMemoryHistory(),
      routes: [{ path: '/', name: 'home', component: { template: '<div />' } }],
    })
    await router.push('/')

    shallowMount(App, {
      global: {
        plugins: [router],
        stubs: {
          AppSidebar: true,
          HostErrorView: true,
          HostLoadingView: true,
          RouterView: true,
          VApp: true,
          VMain: true,
        },
      },
    })
    await flushPromises()

    expect(fetchMock).toHaveBeenCalledTimes(1)
    expect(fetchMock).toHaveBeenCalledWith('/api/check-login', expect.any(Object))
  })
})