<script setup>
import { onMounted, ref } from 'vue';
import AppIcon from '../components/AppIcon.vue';
import StateBlock from '../components/StateBlock.vue';
import { useForums } from '../composables/useForums.js';
import { forumColour, forumFlag } from '../config/languages.js';

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
    <div class="hero-copy">
        <v-chip color="primary" variant="tonal" class="mb-5">Learn together!</v-chip>
        <h1>Pick a forum.<br /><span>Join the conversation.</span></h1>
        <p>Ask questions, share your progress and swap tips with other learners.</p>
    </div>

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
            <span class="cd-forum__accent" :style="{ background: forumColour(forum.code) }" />

            <!-- The same flag files the quizzes home page uses, so a language reads the
                 same in both services. A forum with no course behind it gets an icon. -->
            <img
                v-if="forumFlag(forum.code)"
                class="cd-forum__flag"
                :src="forumFlag(forum.code)"
                alt=""
                aria-hidden="true"
            />
            <span
                v-else
                class="cd-forum__flag cd-forum__flag--icon"
                :style="{ color: forumColour(forum.code) }"
            >
                <AppIcon name="global" :size="30" />
            </span>

            <h2 class="cd-forum__name">{{ forum.name }}</h2>
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
    margin-top: 40px;
}

.cd-forum {
    position: relative;
    display: block;
    overflow: hidden;
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

/* The stripe down the left edge, as on the quizzes course cards. */
.cd-forum__accent {
    position: absolute;
    inset: 0 auto 0 0;
    width: 5px;
}

.cd-forum__flag {
    display: block;
    width: 58px;
    height: 40px;
    margin-bottom: 20px;
    border-radius: 6px;
    object-fit: cover;
    box-shadow: 0 4px 10px rgba(0, 0, 0, .16);
}

/* Sized like a flag, so the cards line up whether or not one is shown. */
.cd-forum__flag--icon {
    display: grid;
    place-items: center;
    border: 1px solid var(--lw-colour-border);
    background: #f5f6ff;
    box-shadow: none;
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
