<script setup lang="ts">
import { useQuery } from '@tanstack/vue-query'

interface LanguageRanking {
    id: number
    userId: number
    language: string
    score: number
    rank: number
    updatedAt: string
}

const apiBase = `${import.meta.env.BASE_URL}api`

const { data: rankings, isLoading, isError } = useQuery<LanguageRanking[]>({
    queryKey: ['language-rankings'],
    queryFn: async () => {
        const res = await fetch(`${apiBase}/language-rankings`)
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

        <div class="lw-card" style="margin-top: calc(var(--lw-space) * 1.5)">
            <h2 class="lw-card__title">Language Rankings</h2>
            <p class="lw-card__hint">Scores and positions across all languages</p>

            <p v-if="isLoading" class="lw-table__empty">Loading rankings…</p>
            <p v-else-if="isError" class="lw-table__error">Failed to load rankings.</p>
            <p v-else-if="!rankings?.length" class="lw-table__empty">No rankings available yet.</p>

            <div v-else class="leaderboard-grid">
                <div class="leaderboard-grid__header">Rank</div>
                <div class="leaderboard-grid__header">User</div>
                <div class="leaderboard-grid__header">Language</div>
                <div class="leaderboard-grid__header">Score</div>

                <template v-for="r in rankings" :key="r.id">
                    <div class="leaderboard-grid__cell leaderboard-grid__rank">
                        <span v-if="r.rank <= 3" class="lw-badge">#{{ r.rank }}</span>
                        <span v-else>#{{ r.rank }}</span>
                    </div>
                    <div class="leaderboard-grid__cell">{{ r.userId }}</div>
                    <div class="leaderboard-grid__cell">{{ r.language }}</div>
                    <div class="leaderboard-grid__cell leaderboard-grid__score">{{ r.score }}</div>
                </template>
            </div>
        </div>
    </div>
</template>

<style scoped>
.leaderboard-grid {
    display: grid;
    grid-template-columns: minmax(60px, 0.5fr) 1fr 1fr minmax(80px, 0.75fr);
    gap: 0;
}

.leaderboard-grid__header {
    font-size: 0.78rem;
    text-transform: uppercase;
    letter-spacing: 0.05em;
    color: var(--lw-colour-ink-muted);
    padding: 0.6rem 0.75rem;
    border-bottom: 1px solid var(--lw-colour-border);
    font-weight: 600;
}

.leaderboard-grid__cell {
    padding: 0.6rem 0.75rem;
    border-bottom: 1px solid var(--lw-colour-border);
    display: flex;
    align-items: center;
}

/* Remove border on the last row */
.leaderboard-grid__cell:nth-last-child(-n+4) {
    border-bottom: none;
}

.leaderboard-grid__rank {
    font-weight: 700;
}

.leaderboard-grid__score {
    font-family: var(--lw-font-mono);
    font-weight: 600;
}
</style>
