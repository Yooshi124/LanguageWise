import { flushPromises, mount } from '@vue/test-utils'
import { QueryClient, VueQueryPlugin } from '@tanstack/vue-query'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { computed, ref } from 'vue'
import HomeView from './HomeView.vue'
import { AnalyticsUserIdKey } from '../composables/useAnalyticsUser'

function mountView(userId: number | null = 7, queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } })) {
  const userIdRef = ref<number | null>(userId)
  const wrapper = mount(HomeView, {
    global: {
      plugins: [[VueQueryPlugin, { queryClient }]],
      provide: { [AnalyticsUserIdKey as symbol]: computed(() => userIdRef.value) },
      stubs: {
        LessonsCompletedChart: true,
        AiSummaryCard: true,
      },
    },
  })
  return { wrapper, queryClient, userIdRef }
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

    const { wrapper } = mountView()
    await flushPromises()

    expect(fetchMock).toHaveBeenCalledWith('/analytics/api/my-language-rankings', expect.any(Object))
    expect(wrapper.text()).toContain('German')
    expect(wrapper.text()).toContain('#2')
    expect(wrapper.text()).toContain('Spanish')
    expect(wrapper.text()).toContain('#5')
  })

  it('renders the empty state when the user has no rankings', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(new Response('[]', { status: 200 })))

    const { wrapper } = mountView()
    await flushPromises()

    expect(wrapper.text()).toContain('You are not ranked in any language yet.')
  })

  it('renders an error when rankings cannot be loaded', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(new Response(null, { status: 503 })))

    const { wrapper } = mountView()
    await flushPromises()

    expect(wrapper.text()).toContain('Failed to load your rankings.')
  })

  it('does not reuse the previous user\'s cached rankings when the signed-in user changes', async () => {
    const responses = [
      new Response(JSON.stringify([
        { id: 1, userId: 1, language: 'German', score: 420, rank: 2, updatedAt: '2026-01-01T00:00:00Z' },
      ]), { status: 200 }),
      new Response(JSON.stringify([
        { id: 2, userId: 2, language: 'Spanish', score: 315, rank: 5, updatedAt: '2026-01-01T00:00:00Z' },
      ]), { status: 200 }),
    ]
    const fetchMock = vi.fn(() => Promise.resolve(responses.shift()!))
    vi.stubGlobal('fetch', fetchMock)

    const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } })
    const { wrapper: firstUserWrapper } = mountView(1, queryClient)
    await flushPromises()
    expect(firstUserWrapper.text()).toContain('German')
    firstUserWrapper.unmount()

    const { wrapper: secondUserWrapper } = mountView(2, queryClient)
    await flushPromises()

    expect(fetchMock).toHaveBeenCalledTimes(2)
    expect(secondUserWrapper.text()).toContain('Spanish')
    expect(secondUserWrapper.text()).not.toContain('German')
  })
})