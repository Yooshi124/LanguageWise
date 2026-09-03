import { flushPromises, mount } from '@vue/test-utils'
import { QueryClient, VueQueryPlugin } from '@tanstack/vue-query'
import { afterEach, describe, expect, it, vi } from 'vitest'
import AiSummaryCard from './AiSummaryCard.vue'

function mountCard() {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } })
  return mount(AiSummaryCard, {
    global: { plugins: [[VueQueryPlugin, { queryClient }]] },
  })
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

    const wrapper = mountCard()
    await flushPromises()

    expect(fetchMock).toHaveBeenCalledWith('/analytics/api/lessons-completed-summary', expect.objectContaining({ method: 'POST' }))
    expect(wrapper.text()).toContain('Your recent lessons show steady momentum.')
    expect(wrapper.text()).toContain('Trending up')
    expect(wrapper.text()).toContain('Best course: Spanish')
  })

  it('renders an error when summary generation fails', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(new Response(null, { status: 503 })))

    const wrapper = mountCard()
    await flushPromises()

    expect(wrapper.text()).toContain('Failed to generate summary.')
  })
})