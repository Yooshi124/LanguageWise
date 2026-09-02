<script setup>
import { onMounted, ref } from 'vue';
import StateBlock from '../components/StateBlock.vue';
import { useForums } from '../composables/useForums.js';

const { forums, ensureLoaded } = useForums();

const loading = ref(true);
const failed = ref(false);

async function load() {
    loading.value = true;
    failed.value = false;

    try {
        await ensureLoaded();
    } catch {
        failed.value = true;
    } finally {
        loading.value = false;
    }
}

onMounted(load);
</script>

<template>
    <h2 class="lw-section-heading">Forums</h2>

    <StateBlock v-if="loading" title="Loading forums…" />

    <StateBlock
        v-else-if="failed"
        title="The forum list is unavailable"
        message="The discussion service could not be reached. It may still be starting up."
        tone="error"
        retry-label="Try again"
        @retry="load"
    />

    <div v-else class="cd-forums">
        <RouterLink
            v-for="forum in forums"
            :key="forum.code"
            class="lw-card cd-forum"
            :to="{ name: 'forum', params: { code: forum.code } }"
        >
            <h3 class="cd-forum__name">{{ forum.name }}</h3>
            <p class="cd-forum__hint">
                {{ forum.code === 'global'
                    ? 'Anything that is not tied to one language.'
                    : `Posts from people learning ${forum.name}.` }}
            </p>
        </RouterLink>
    </div>
</template>

<style scoped>
.cd-forums {
    display: grid;
    grid-template-columns: repeat(auto-fill, minmax(260px, 1fr));
    gap: 20px;
    margin-top: 30px;
}

.cd-forum {
    display: block;
    padding: 26px;
    text-decoration: none;
    color: inherit;
    transition: transform .2s ease, box-shadow .2s ease, border-color .2s ease;
}

.cd-forum:hover {
    transform: translateY(-6px);
    border-color: #d5d9e6;
    box-shadow: 0 18px 40px rgba(31, 41, 55, .1);
}

.cd-forum:focus-visible {
    outline: 3px solid rgba(79, 70, 229, .35);
    outline-offset: 3px;
}

.cd-forum__name {
    margin: 0 0 0.35rem;
    font-size: 1.55rem;
    letter-spacing: -.02em;
    color: var(--lw-colour-ink);
}

.cd-forum__hint {
    margin: 0;
    color: var(--lw-colour-ink-muted);
    font-size: 0.9rem;
}
</style>
