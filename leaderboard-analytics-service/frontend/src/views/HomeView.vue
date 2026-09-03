<script setup lang="ts">
import { useQuery } from '@tanstack/vue-query'
import LessonsCompletedChart from '../components/LessonsCompletedChart.vue'
import AiSummaryCard from '../components/AiSummaryCard.vue'

interface LanguageRanking {
    id: number
    userId: number
    language: string
    score: number
    rank: number
    updatedAt: string
}

const apiBase = '/analytics/api'

const {
    data: myRankings,
    isLoading: isMyLoading,
    isError: isMyError,
} = useQuery<LanguageRanking[]>({
    queryKey: ['my-language-rankings'],
    queryFn: async () => {
        const res = await fetch(`${apiBase}/my-language-rankings`, {
            credentials: 'same-origin',
            headers: { Accept: 'application/json' },
        })
        if (!res.ok) throw new Error(`Request failed (${res.status})`)
        return res.json()
    },
})
</script>

<template>
    <div class="lw-shell">
        <header class="lw-header">
            <div class="lw-header__inner">
                <h1 class="lw-header__title">Leaderboard &amp; Analytics</h1>
                <p class="lw-header__subtitle">Language ranking statistics</p>
            </div>
        </header>

        <LessonsCompletedChart />
        <AiSummaryCard />

        <div class="lw-card analytics-card">
                <h2 class="lw-card__title">Your Rankings</h2>
                <p class="lw-card__hint">Your position across every language you are studying</p>

                <p v-if="isMyLoading" class="lw-table__empty">Loading your rankings…</p>
                <p v-else-if="isMyError" class="lw-table__error">Failed to load your rankings.</p>
                <p v-else-if="!myRankings?.length" class="lw-table__empty">
                    You are not ranked in any language yet.
                </p>

                <div v-else class="my-rankings-grid">
                    <div class="my-rankings-grid__header">Language</div>
                    <div class="my-rankings-grid__header">Rank</div>
                    <div class="my-rankings-grid__header">Score</div>

                    <template v-for="r in myRankings" :key="r.id">
                        <div class="my-rankings-grid__cell">{{ r.language }}</div>
                        <div class="my-rankings-grid__cell my-rankings-grid__rank">
                            <span v-if="r.rank <= 3" class="lw-badge">#{{ r.rank }}</span>
                            <span v-else>#{{ r.rank }}</span>
                        </div>
                        <div class="my-rankings-grid__cell my-rankings-grid__score">{{ r.score }}</div>
                    </template>
                </div>
        </div>
    </div>
</template>
