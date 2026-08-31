<script setup>
import { computed } from 'vue';
import { useRoute } from 'vue-router';
import { useAssistant } from '../composables/useAssistant.js';

const services = [
    { name: 'Home', owner: 'Team', href: 'http://localhost:3000/' },
    { name: 'Mini Games', owner: 'Kyan', href: null },
    { name: 'Discussion Forum', owner: 'Lachlan', href: '/chat-discussion/', current: true },
    { name: 'Quizzes & Courses', owner: 'Justin', href: 'http://localhost:3000/quizzes-and-courses/' },
    { name: 'Quests & Achievements', owner: 'Amber', href: 'http://localhost:3000/quests-and-achievements/' },
    { name: 'Leaderboard & Analytics', owner: 'Roan', href: null }
];

const route = useRoute();
const { open: assistantOpen, toggle: toggleAssistant } = useAssistant();

const browsingForums = computed(() => ['forums', 'forum', 'post', 'post-edit'].includes(route.name));
const viewingMyPosts = computed(() => route.name === 'my-posts');
</script>

<template>
    <nav aria-label="Microservices">
        <ul class="lw-tabs">
            <li v-for="service in services" :key="service.name">
                <a
                    v-if="service.href"
                    class="lw-tabs__link"
                    :aria-current="service.current ? 'page' : undefined"
                    :href="service.href"
                >
                    {{ service.name }}
                    <span class="lw-tabs__owner">{{ service.owner }}</span>
                </a>
                <span v-else class="lw-tabs__link lw-tabs__link--disabled" aria-disabled="true">
                    {{ service.name }}
                    <span class="lw-tabs__owner">{{ service.owner }}</span>
                </span>
            </li>
        </ul>
    </nav>

    <nav aria-label="Forum sections" class="cd-nav">
        <RouterLink
            class="cd-nav__link"
            :class="{ 'cd-nav__link--current': browsingForums }"
            :to="{ name: 'forums' }"
        >Forums</RouterLink>
        <RouterLink
            class="cd-nav__link"
            :class="{ 'cd-nav__link--current': viewingMyPosts }"
            :to="{ name: 'my-posts' }"
        >My Posts</RouterLink>
        <button
            type="button"
            class="cd-nav__link cd-nav__link--ai"
            :class="{ 'cd-nav__link--current': assistantOpen }"
            :aria-pressed="assistantOpen"
            @click="toggleAssistant"
        >AI mode</button>
        <RouterLink class="cd-nav__link cd-nav__link--cta" :to="{ name: 'post-create' }">New post</RouterLink>
    </nav>
</template>

<style scoped>
.cd-nav {
    display: flex;
    gap: 0.5rem;
    align-items: center;
    padding: 0.75rem 0 1.25rem;
    border-bottom: 1px solid var(--lw-colour-border);
    margin-bottom: 1.5rem;
}

.cd-nav__link {
    padding: 0.45rem 0.9rem;
    border: 1px solid var(--lw-colour-border);
    border-radius: var(--lw-radius-sm);
    color: var(--lw-colour-ink);
    text-decoration: none;
    font-weight: 600;
    font-size: 0.95rem;
}

.cd-nav__link:hover {
    border-color: var(--lw-colour-primary);
}

.cd-nav__link--current {
    background: var(--lw-colour-primary);
    border-color: var(--lw-colour-primary);
    color: #fff;
}

/* A button rather than a link, so it needs the resets an anchor gets for free. */
.cd-nav__link--ai {
    margin-left: auto;
    background: none;
    font-family: inherit;
    cursor: pointer;
}
</style>
