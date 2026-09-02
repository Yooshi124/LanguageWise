import { flushPromises, mount } from '@vue/test-utils'
import { QueryClient, VueQueryPlugin } from '@tanstack/vue-query'
import { afterEach, describe, expect, it, vi } from 'vitest'
import HomeView from './HomeView.vue'

function mountView() {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  })

  return mount(HomeView, {
    global: {
      plugins: [[VueQueryPlugin, { queryClient }]],
      stubs: {
        LessonsCompletedChart: true,
        AiSummaryCard: true,
      },
    },
  })
}

afterEach(() => vi.unstubAllGlobals())

describe('HomeView rankings', () => {
  it('renders ranked languages and uses the gateway API path', async () => {
    const rankings = [
      { id: 1, userId: 7, language: 'German', score: 420, rank: 2, updatedAt: '2026-01-01T00:00:00Z' },
      { id: 2, userId: 7, language: 'Spanish', score: 315, rank: 5, updatedAt: '2026-01-01T00:00:00Z' },
    ]
    const fetchMock = vi.fn().mockResolvedValue(new Response(JSON.stringify(rankings), { status: 200 }))
    vi.stubGlobal('fetch', fetchMock)

    const wrapper = mountView()
    await flushPromises()

    expect(fetchMock).toHaveBeenCalledWith('/analytics/api/my-language-rankings', expect.any(Object))
    expect(wrapper.text()).toContain('German')
    expect(wrapper.text()).toContain('#2')
    expect(wrapper.text()).toContain('Spanish')
    expect(wrapper.text()).toContain('#5')
  })

  it('renders the empty state when the user has no rankings', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(new Response('[]', { status: 200 })))

    const wrapper = mountView()
    await flushPromises()

    expect(wrapper.text()).toContain('You are not ranked in any language yet.')
  })

  it('renders an error when rankings cannot be loaded', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(new Response(null, { status: 503 })))

    const wrapper = mountView()
    await flushPromises()

    expect(wrapper.text()).toContain('Failed to load your rankings.')
  })
})