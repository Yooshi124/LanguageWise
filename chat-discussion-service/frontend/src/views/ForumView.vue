<script setup>
import { computed, onBeforeUnmount, onMounted, ref, watch } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import AppIcon from '../components/AppIcon.vue';
import PostCard from '../components/PostCard.vue';
import StateBlock from '../components/StateBlock.vue';
import { usePostList } from '../composables/usePostList.js';
import { useForums } from '../composables/useForums.js';
import { forumColour, forumFlag } from '../config/languages.js';

const props = defineProps({ code: { type: String, required: true } });

const route = useRoute();
const router = useRouter();
const { ensureLoaded, forumName, exists, forums } = useForums();
const { posts, loading, loadingMore, error, hasMore, load, loadMore, replace } = usePostList();

const term = ref(typeof route.query.q === 'string' ? route.query.q : '');
const forumsReady = ref(false);
let debounce = null;

const filter = computed(() => ({ forumCode: props.code, q: route.query.q || undefined }));
const activeTerm = computed(() => (typeof route.query.q === 'string' ? route.query.q : ''));
const forumKnown = computed(() => !forumsReady.value || exists(props.code));

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

onMounted(async () => {
    try {
        await ensureLoaded();
    } catch {
    }

    forumsReady.value = forums.value.length > 0;

    if (forumKnown.value) {
        await load(filter.value);
    }
});

onBeforeUnmount(() => window.clearTimeout(debounce));
</script>

<template>
    <StateBlock
        v-if="!forumKnown"
        title="No such forum"
        :message="`There is no forum called '${code}'.`"
        tone="error"
    >
        <p><RouterLink :to="{ name: 'forums' }">Back to all forums</RouterLink></p>
    </StateBlock>

    <template v-else>
        <v-btn :to="{ name: 'forums' }" variant="text" class="cd-back">
            <template #prepend><AppIcon name="arrow-left" /></template>
            All forums
        </v-btn>

        <header class="cd-heading cd-heading--compact">
            <div class="cd-heading__intro">
                <img
                    v-if="forumFlag(code)"
                    class="cd-heading__flag"
                    :src="forumFlag(code)"
                    alt=""
                    aria-hidden="true"
                />
                <v-chip :color="forumColour(code)" variant="tonal">Forum</v-chip>
            </div>
            <h1>{{ forumName(code) }}</h1>
            <p>Search covers post titles, post content and comments in this forum.</p>
        </header>

        <label class="cd-search">
            <span class="cd-search__label">Search this forum</span>
            <input
                v-model="term"
                class="cd-search__input"
                type="search"
                :placeholder="`Search ${forumName(code)}…`"
            >
        </label>

        <StateBlock v-if="loading" title="Loading posts…" />

        <StateBlock
            v-else-if="error && error.isUnavailable"
            title="The forum is unavailable"
            message="The discussion service could not reach its database. Nothing has been lost."
            tone="error"
            retry-label="Try again"
            @retry="load(filter)"
        />

        <StateBlock
            v-else-if="error"
            title="Something went wrong loading this forum"
            :message="error.firstValidationMessage || 'Please try again.'"
            tone="error"
            retry-label="Try again"
            @retry="load(filter)"
        />

        <StateBlock
            v-else-if="posts.length === 0 && activeTerm"
            title="No matches"
            :message="`Nothing in ${forumName(code)} matched '${activeTerm}'.`"
        />

        <StateBlock
            v-else-if="posts.length === 0"
            title="No posts here yet"
            :message="`Be the first to post in ${forumName(code)}.`"
        >
            <p><RouterLink :to="{ name: 'post-create' }">Write a post</RouterLink></p>
        </StateBlock>

        <template v-else>
            <PostCard
                v-for="post in posts"
                :key="post.id"
                :post="post"
                :show-forum="false"
                @update="replace"
            />

            <p v-if="hasMore" class="cd-more">
                <button type="button" class="lw-command" :disabled="loadingMore" @click="loadMore(filter)">
                    {{ loadingMore ? 'Loading…' : 'Load more' }}
                </button>
            </p>
        </template>
    </template>
</template>
