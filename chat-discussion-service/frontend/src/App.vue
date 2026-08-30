<script setup>
import AppHeader from './components/AppHeader.vue';
import AppNav from './components/AppNav.vue';
import { useForums } from './composables/useForums.js';

useForums().ensureLoaded().catch(() => {});
</script>

<template>
    <AppHeader />
    <main class="lw-shell">
        <AppNav />
        <RouterView />
    </main>
</template>
