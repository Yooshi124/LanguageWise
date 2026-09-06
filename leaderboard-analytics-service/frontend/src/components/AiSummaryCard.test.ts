import { flushPromises, mount } from '@vue/test-utils'
import { QueryClient, VueQueryPlugin } from '@tanstack/vue-query'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { computed, ref } from 'vue'
import AiSummaryCard from './AiSummaryCard.vue'
import { AnalyticsUserIdKey } from '../composables/useAnalyticsUser'

function mountCard(userId: number | null = 7, queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } })) {
  const userIdRef = ref<number | null>(userId)
  const wrapper = mount(AiSummaryCard, {
    global: {
      plugins: [[VueQueryPlugin, { queryClient }]],
      provide: { [AnalyticsUserIdKey as symbol]: computed(() => userIdRef.value) },
    },
  })
  return { wrapper, queryClient, userIdRef }
}

afterEach(() => vi.unstubAllGlobals())

describe('AiSummaryCard', () => {
  it('renders the trend and best course returned by the Analytics API', async () => {
    const fetchMock = vi.fn().mockResolvedValue(new Response(JSON.stringify({
      summary: 'Your recent lessons show steady momentum.',
      trend: 'up',
      bestCourse: 'Spanish',
    }), { status: 200 }))
    vi.stubGlobal('fetch', fetchMock)

    const { wrapper } = mountCard()
    await flushPromises()

    expect(fetchMock).toHaveBeenCalledWith('/analytics/api/lessons-completed-summary', expect.objectContaining({ method: 'POST' }))
    expect(wrapper.text()).toContain('Your recent lessons show steady momentum.')
    expect(wrapper.text()).toContain('Trending up')
    expect(wrapper.text()).toContain('Best course: Spanish')
  })

  it('renders an error when summary generation fails', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(new Response(null, { status: 503 })))

    const { wrapper } = mountCard()
    await flushPromises()

    expect(wrapper.text()).toContain('Failed to generate summary.')
  })

  it('does not reuse the previous user\'s cached summary when the signed-in user changes', async () => {
    const responses = [
      new Response(JSON.stringify({ summary: 'User A summary', trend: 'up', bestCourse: 'French' }), { status: 200 }),
      new Response(JSON.stringify({ summary: 'User B summary', trend: 'down', bestCourse: 'German' }), { status: 200 }),
    ]
    const fetchMock = vi.fn(() => Promise.resolve(responses.shift()!))
    vi.stubGlobal('fetch', fetchMock)

    const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } })
    const { wrapper: firstUserWrapper } = mountCard(1, queryClient)
    await flushPromises()
    expect(firstUserWrapper.text()).toContain('User A summary')
    firstUserWrapper.unmount()

    const { wrapper: secondUserWrapper } = mountCard(2, queryClient)
    await flushPromises()

    expect(fetchMock).toHaveBeenCalledTimes(2)
    expect(secondUserWrapper.text()).toContain('User B summary')
    expect(secondUserWrapper.text()).not.toContain('User A summary')
  })
})