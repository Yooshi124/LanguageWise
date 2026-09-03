<script setup>
import { onMounted, ref } from 'vue';
import AppIcon from '../components/AppIcon.vue';
import StateBlock from '../components/StateBlock.vue';
import { useForums } from '../composables/useForums.js';
import { forumColour, forumFlag } from '../config/languages.js';

const { forums, ensureLoaded, displayName } = useForums();

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

            <h2 class="cd-forum__name">{{ displayName(forum) }}</h2>
            <p class="cd-forum__hint">
                {{ forum.code === 'global'
                    ? 'Anything that is not tied to one language.'
                    : `Posts from people learning ${displayName(forum)}.` }}
            </p>
        </RouterLink>
    </div>
</template>
