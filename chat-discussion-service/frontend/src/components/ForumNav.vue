<script setup>
import { computed } from 'vue';
import { useRoute } from 'vue-router';

const route = useRoute();

const browsingForums = computed(() => ['forums', 'forum', 'post', 'post-edit'].includes(route.name));
const viewingMyPosts = computed(() => route.name === 'my-posts');
</script>

<template>
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
        <RouterLink class="cd-nav__link cd-nav__link--cta" :to="{ name: 'post-create' }">New post</RouterLink>
    </nav>
</template>