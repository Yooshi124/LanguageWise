<script setup lang="ts">
import { onMounted, ref } from 'vue'
import AppIcon from './components/AppIcon.vue'
import AppSidebar from './components/AppSidebar.vue'
import HomeView from './views/HomeView.vue'
import { useAuth } from './composables/useAuth'

const auth = useAuth()
const sidebarExpanded = ref(false)
const mobileSidebarOpen = ref(false)

onMounted(() => {
  auth.ensureAuthenticated().catch(() => {})
})
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
      <v-main>
        <HomeView />
      </v-main>
    </div>
  </v-app>
</template>
