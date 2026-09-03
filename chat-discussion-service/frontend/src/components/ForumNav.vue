<script setup>
import { computed } from 'vue';
import { useRoute } from 'vue-router';

const route = useRoute();

const browsingForums = computed(() => ['forums', 'forum', 'post', 'post-edit'].includes(route.name));
const viewingMyPosts = computed(() => route.name === 'my-posts');
</script>

<template>
    <header class="cd-topbar">
        <nav aria-label="Forum sections" class="cd-nav">
            <v-btn :to="{ name: 'forums' }" :active="browsingForums" variant="text">Forums</v-btn>
            <v-btn :to="{ name: 'my-posts' }" :active="viewingMyPosts" variant="text">My Posts</v-btn>
            <v-btn :to="{ name: 'post-create' }" class="cd-nav__cta" color="primary" variant="flat">New post</v-btn>
        </nav>
    </header>
</template>