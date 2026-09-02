<script setup lang="ts">
import { computed, ref } from 'vue'
import { useRoute } from 'vue-router'
import AppIcon from './components/AppIcon.vue'
import AppSidebar from './components/AppSidebar.vue'

const route = useRoute()
const sidebarExpanded = ref(false)
const mobileSidebarOpen = ref(false)
const showShell = computed(() => route.name !== 'login')
</script>

<template>
  <v-app v-if="showShell" :class="{ 'sidebar-expanded': sidebarExpanded }">
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
      <v-main>
        <router-view />
      </v-main>
    </div>
  </v-app>
  <v-app v-else>
    <router-view />
  </v-app>
</template>
