<script setup>
import { ref } from 'vue';
import AppIcon from './components/AppIcon.vue';
import AppSidebar from './components/AppSidebar.vue';
import AppNav from './components/AppNav.vue';
import GarryAssistant from './components/GarryAssistant.vue';
import { useForums } from './composables/useForums.js';

const sidebarExpanded = ref(false);
const mobileSidebarOpen = ref(false);

useForums().ensureLoaded().catch(() => {});
</script>

<template>
    <v-app :class="{ 'sidebar-expanded': sidebarExpanded }">
        <AppSidebar
            v-model:expanded="sidebarExpanded"
            v-model:mobile-open="mobileSidebarOpen"
        />
        <button
            type="button"
            class="mobile-nav-trigger"
            aria-label="Open service navigation"
            @click="mobileSidebarOpen = true"
        >
            <AppIcon name="menu" />
        </button>
        <button
            v-if="mobileSidebarOpen"
            type="button"
            class="sidebar-scrim"
            aria-label="Close service navigation"
            @click="mobileSidebarOpen = false"
        />
        <div class="app-shell-content">
            <AppNav />
            <v-main>
                <div class="forum-page">
                    <RouterView />
                </div>
            </v-main>
        </div>

        <!-- Outside the shell: Garry floats over whichever page you are on, so the
             instructions stay visible while you follow them. -->
        <GarryAssistant />
    </v-app>
</template>
