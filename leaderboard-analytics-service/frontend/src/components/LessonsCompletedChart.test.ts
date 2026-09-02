import { flushPromises, mount } from '@vue/test-utils'
import { QueryClient, VueQueryPlugin } from '@tanstack/vue-query'
import { afterEach, describe, expect, it, vi } from 'vitest'
import LessonsCompletedChart from './LessonsCompletedChart.vue'

const { chartFactory, chartUpdate, chartDestroy } = vi.hoisted(() => ({
  chartFactory: vi.fn(),
  chartUpdate: vi.fn(),
  chartDestroy: vi.fn(),
}))

vi.mock('highcharts', () => ({
  default: {
    chart: chartFactory,
  },
}))
vi.mock('highcharts/modules/accessibility', () => ({ default: vi.fn() }))

const initialData = {
  userId: 7,
  from: '2026-01-01',
  to: '2026-01-02',
  series: [{
    courseCode: 'de',
    courseTitle: 'German',
    points: [{ date: '2026-01-01', lessonsCompleted: 2 }],
  }],
}

afterEach(() => {
  vi.unstubAllGlobals()
  vi.clearAllMocks()
})

describe('LessonsCompletedChart', () => {
  it('creates, updates, and destroys an accessible UTC chart', async () => {
    chartFactory.mockReturnValue({ update: chartUpdate, destroy: chartDestroy })
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(
      new Response(JSON.stringify(initialData), { status: 200 }),
    ))
    const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } })
    const wrapper = mount(LessonsCompletedChart, {
      global: { plugins: [[VueQueryPlugin, { queryClient }]] },
    })
    await flushPromises()

    expect(chartFactory).toHaveBeenCalledOnce()
    const options = chartFactory.mock.calls[0][1]
    expect(options.accessibility.description).toContain('last 30 days')
    expect(options.series[0].data[0]).toEqual([Date.UTC(2026, 0, 1), 2])
    expect(options.yAxis.allowDecimals).toBe(false)
    expect(options.tooltip.shared).toBe(true)

    queryClient.setQueryData(['lessons-completed-over-time'], {
      ...initialData,
      series: [{ ...initialData.series[0], points: [{ date: '2026-01-02', lessonsCompleted: 4 }] }],
    })
    await flushPromises()
    expect(chartUpdate).toHaveBeenCalledOnce()

    wrapper.unmount()
    expect(chartDestroy).toHaveBeenCalledOnce()
  })
})