<script setup lang="ts">
import { computed } from 'vue'
import { useQuery } from '@tanstack/vue-query'
import LessonsCompletedChart from '../components/LessonsCompletedChart.vue'
import AiSummaryCard from '../components/AiSummaryCard.vue'
import { useAuth } from '../composables/useAuth'

interface LanguageRanking {
    id: number
    userId: number
    language: string
    score: number
    rank: number
    updatedAt: string
}

const apiBase = `${import.meta.env.BASE_URL}api`

const auth = useAuth()
const isAuthLoading = computed(() => auth.status.value === 'loading')
const isSignedOut = computed(() => auth.status.value === 'signed-out')
const isAuthenticated = computed(() => auth.status.value === 'authenticated')

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
    enabled: isAuthenticated,
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

        <div v-if="isAuthLoading" class="lw-card" style="margin-top: calc(var(--lw-space) * 1.5)">
            <p class="lw-table__empty">Checking login…</p>
        </div>

        <div v-else-if="isSignedOut" class="lw-card" style="margin-top: calc(var(--lw-space) * 1.5)">
            <h2 class="lw-card__title">Analytics is only available to logged in users</h2>
            <p class="lw-card__hint">Please sign in to view your language rankings and lesson progress.</p>
        </div>

        <template v-else>
            <LessonsCompletedChart />
            <AiSummaryCard />

            <div class="lw-card" style="margin-top: calc(var(--lw-space) * 1.5)">
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
        </template>
    </div>
</template>

<style scoped>
.my-rankings-grid {
    display: grid;
    grid-template-columns: 1fr minmax(80px, 0.6fr) minmax(80px, 0.6fr);
    gap: 0;
}

.my-rankings-grid__header {
    font-size: 0.78rem;
    text-transform: uppercase;
    letter-spacing: 0.05em;
    color: var(--lw-colour-ink-muted);
    padding: 0.6rem 0.75rem;
    border-bottom: 1px solid var(--lw-colour-border);
    font-weight: 600;
}

.my-rankings-grid__cell {
    padding: 0.6rem 0.75rem;
    border-bottom: 1px solid var(--lw-colour-border);
    display: flex;
    align-items: center;
}

.my-rankings-grid__cell:nth-last-child(-n+3) {
    border-bottom: none;
}

.my-rankings-grid__rank {
    font-weight: 700;
}

.my-rankings-grid__score {
    font-family: var(--lw-font-mono);
    font-weight: 600;
}
</style>
