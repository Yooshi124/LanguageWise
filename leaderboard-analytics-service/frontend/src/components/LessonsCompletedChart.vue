<script setup lang="ts">
import { computed, onBeforeUnmount, ref, shallowRef, watch } from 'vue'
import { useQuery } from '@tanstack/vue-query'
import Highcharts from 'highcharts'
import Accessibility from 'highcharts/modules/accessibility'

Accessibility(Highcharts)

interface LessonsCompletedPoint {
    date: string
    lessonsCompleted: number
}

interface LessonsCompletedSeries {
    courseCode: string
    courseTitle: string
    points: LessonsCompletedPoint[]
}

interface LessonsCompletedResponse {
    userId: number
    from: string
    to: string
    series: LessonsCompletedSeries[]
}

const apiBase = '/analytics/api'

const { data, isLoading, isError } = useQuery<LessonsCompletedResponse>({
    queryKey: ['lessons-completed-over-time'],
    queryFn: async () => {
        const res = await fetch(`${apiBase}/lessons-completed-over-time`, {
            credentials: 'same-origin',
            headers: { Accept: 'application/json' },
        })
        if (!res.ok) throw new Error(`Request failed (${res.status})`)
        return res.json()
    },
})

const chartContainer = ref<HTMLDivElement | null>(null)
const chart = shallowRef<Highcharts.Chart | null>(null)

const hasSeries = computed(() => (data.value?.series?.length ?? 0) > 0)

function buildSeries(response: LessonsCompletedResponse): Highcharts.SeriesOptionsType[] {
    return response.series.map((s) => ({
        type: 'line',
        name: s.courseTitle,
        data: s.points.map((p) => {
            const [year, month, day] = p.date.split('-').map(Number)
            return [Date.UTC(year, month - 1, day), p.lessonsCompleted] as [number, number]
        }),
    }))
}

function buildOptions(response: LessonsCompletedResponse): Highcharts.Options {
    return {
        chart: { type: 'line', backgroundColor: 'transparent' },
        title: { text: undefined },
        accessibility: {
            description: 'Cumulative lessons completed during the last 30 days, shown as one line per course.',
        },
        credits: { enabled: false },
        legend: { align: 'center', verticalAlign: 'bottom' },
        xAxis: {
            type: 'datetime',
            title: { text: undefined },
            dateTimeLabelFormats: { day: '%e %b', week: '%e %b', month: "%b '%y" },
        },
        yAxis: {
            title: { text: 'Lessons completed' },
            allowDecimals: false,
            min: 0,
        },
        tooltip: {
            shared: true,
            xDateFormat: '%A, %e %b %Y',
        },
        plotOptions: {
            line: { marker: { enabled: false, states: { hover: { enabled: true } } } },
        },
        series: buildSeries(response),
    }
}

watch(
    [data, chartContainer],
    ([response, container]) => {
        if (!response || !container) return

        if (!chart.value) {
            chart.value = Highcharts.chart(container, buildOptions(response))
            return
        }

        chart.value.update(
            { series: buildSeries(response) } as Highcharts.Options,
            true,
            true,
        )
    },
    { immediate: true },
)

onBeforeUnmount(() => {
    chart.value?.destroy()
    chart.value = null
})
</script>

<template>
    <div class="lw-card analytics-card">
        <h2 class="lw-card__title">Lessons Completed Over Time</h2>
        <p class="lw-card__hint">Your last 30 days of lesson progress, per course</p>

        <p v-if="isLoading" class="lw-table__empty">Loading chart…</p>
        <p v-else-if="isError" class="lw-table__error">Failed to load chart data.</p>
        <p v-else-if="!hasSeries" class="lw-table__empty">No lesson activity yet.</p>

        <div v-show="!isLoading && !isError && hasSeries" ref="chartContainer" class="lessons-completed-chart" />
    </div>
</template>
