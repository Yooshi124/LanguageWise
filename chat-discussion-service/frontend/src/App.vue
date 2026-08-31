<script setup>
import AppHeader from './components/AppHeader.vue';
import AppNav from './components/AppNav.vue';
import AssistantPanel from './components/AssistantPanel.vue';
import { useForums } from './composables/useForums.js';

useForums().ensureLoaded().catch(() => {});
</script>

<template>
    <AppHeader />
    <main class="lw-shell">
        <AppNav />
        <RouterView />
    </main>
    <!-- Outside the shell: AI mode docks over whichever page you are on, so the
         instructions stay visible while you follow them. -->
    <AssistantPanel />
</template>
