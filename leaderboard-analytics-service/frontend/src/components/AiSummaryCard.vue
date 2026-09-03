<script setup lang="ts">
import { computed } from 'vue'
import { useQuery } from '@tanstack/vue-query'

interface LessonsCompletedSummary {
    summary: string
    trend: 'up' | 'down' | 'flat'
    bestCourse: string
}

const apiBase = '/analytics/api'

const { data, isLoading, isError } = useQuery<LessonsCompletedSummary>({
    queryKey: ['lessons-completed-summary'],
    queryFn: async () => {
        const res = await fetch(`${apiBase}/lessons-completed-summary`, {
            method: 'POST',
            credentials: 'same-origin',
            headers: { Accept: 'application/json' },
        })
        if (!res.ok) throw new Error(`Request failed (${res.status})`)
        return res.json()
    },
    // The LLM call is deterministic per user for the same 30-day window; no need to refetch on focus.
    staleTime: 5 * 60 * 1000,
    refetchOnWindowFocus: false,
})

const trendLabel = computed(() => {
    switch (data.value?.trend) {
        case 'up': return 'Trending up'
        case 'down': return 'Trending down'
        case 'flat': return 'Holding steady'
        default: return ''
    }
})

const trendGlyph = computed(() => {
    switch (data.value?.trend) {
        case 'up': return '▲'
        case 'down': return '▼'
        case 'flat': return '▬'
        default: return ''
    }
})
</script>

<template>
    <div class="lw-card analytics-card">
        <h2 class="lw-card__title">AI Summary</h2>
        <p class="lw-card__hint">Personalised insights on your last 30 days</p>

        <p v-if="isLoading" class="lw-table__empty">Generating summary…</p>
        <p v-else-if="isError" class="lw-table__error">Failed to generate summary.</p>
        <p v-else-if="!data" class="lw-table__empty">No summary available.</p>

        <template v-else>
            <p class="ai-summary__text">{{ data.summary }}</p>
            <div class="ai-summary__meta">
                <span :class="['ai-summary__trend', `ai-summary__trend--${data.trend}`]">
                    <span class="ai-summary__glyph" aria-hidden="true">{{ trendGlyph }}</span>
                    {{ trendLabel }}
                </span>
                <span class="ai-summary__best">Best course: <strong>{{ data.bestCourse }}</strong></span>
            </div>
        </template>
    </div>
</template>
