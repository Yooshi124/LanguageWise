<script setup>
import { computed, onBeforeUnmount, onMounted, ref, watch } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import PostCard from '../components/PostCard.vue';
import StateBlock from '../components/StateBlock.vue';
import { usePostList } from '../composables/usePostList.js';
import { useFeatureUser } from '../federation/featureHost.js';

const route = useRoute();
const router = useRouter();
const me = useFeatureUser();
const { posts, loading, loadingMore, error, hasMore, load, loadMore, replace } = usePostList();

const term = ref(typeof route.query.q === 'string' ? route.query.q : '');
let debounce = null;

const activeTerm = computed(() => (typeof route.query.q === 'string' ? route.query.q : ''));

const filter = computed(() => ({ userId: me.value?.id, q: route.query.q || undefined }));

watch(term, (value) => {
    window.clearTimeout(debounce);
    debounce = window.setTimeout(() => {
        const next = value.trim();

        if (next === activeTerm.value) {
            return;
        }

        router.replace({ query: next ? { q: next } : {} });
    }, 300);
});

watch(activeTerm, (value) => {
    if (value !== term.value.trim()) {
        term.value = value;
    }
});

watch(filter, () => load(filter.value), { deep: true });

onMounted(() => load(filter.value));
onBeforeUnmount(() => window.clearTimeout(debounce));
</script>

<template>
    <header class="cd-heading">
        <div class="cd-heading__intro">
            <v-chip color="primary" variant="tonal">Your activity</v-chip>
        </div>
        <h1>My posts.<br /><span>Everything you have shared.</span></h1>
        <p>Revisit your discussions, track the replies they gathered and pick up where you left off.</p>
    </header>

    <label class="cd-search">
        <span class="cd-search__label">Search your posts</span>
        <input v-model="term" class="cd-search__input" type="search" placeholder="Search everything you posted…">
    </label>

    <StateBlock v-if="loading" title="Loading your posts…" />

    <StateBlock
        v-else-if="error && error.isUnavailable"
        title="Your posts are unavailable"
        message="The discussion service could not reach its database. Nothing has been lost."
        tone="error"
        retry-label="Try again"
        @retry="load(filter)"
    />

    <StateBlock
        v-else-if="error"
        title="Something went wrong loading your posts"
        :message="error.firstValidationMessage || 'Please try again.'"
        tone="error"
        retry-label="Try again"
        @retry="load(filter)"
    />

    <StateBlock
        v-else-if="posts.length === 0 && activeTerm"
        title="No matches"
        :message="`None of your posts matched '${activeTerm}'.`"
    />

    <StateBlock
        v-else-if="posts.length === 0"
        title="You have not posted yet"
        message="Anything you write will show up here."
    >
        <p><RouterLink :to="{ name: 'post-create' }">Write your first post</RouterLink></p>
    </StateBlock>

    <template v-else>
        <PostCard v-for="post in posts" :key="post.id" :post="post" @update="replace" />

        <p v-if="hasMore" class="cd-more">
            <button type="button" class="lw-command" :disabled="loadingMore" @click="loadMore(filter)">
                {{ loadingMore ? 'Loading…' : 'Load more' }}
            </button>
        </p>
    </template>
</template>
