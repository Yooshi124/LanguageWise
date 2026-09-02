<script setup>
import { computed } from 'vue';
import { useRoute } from 'vue-router';
import { useAssistant } from '../composables/useAssistant.js';

const route = useRoute();
const { open: assistantOpen, toggle: toggleAssistant } = useAssistant();

const browsingForums = computed(() => ['forums', 'forum', 'post', 'post-edit'].includes(route.name));
const viewingMyPosts = computed(() => route.name === 'my-posts');
</script>

<template>
    <header class="local-topbar">
        <nav aria-label="Forum sections" class="local-topbar-nav">
            <v-btn
                :to="{ name: 'forums' }"
                variant="text"
                :active="browsingForums"
            >Forums</v-btn>
            <v-btn
                :to="{ name: 'my-posts' }"
                variant="text"
                :active="viewingMyPosts"
            >My Posts</v-btn>
            <v-btn
                variant="text"
                :active="assistantOpen"
                :aria-pressed="assistantOpen"
                @click="toggleAssistant"
            >AI mode</v-btn>
            <v-btn
                :to="{ name: 'post-create' }"
                color="primary"
                variant="flat"
                class="topbar-cta"
            >New post</v-btn>
        </nav>
    </header>
</template>
