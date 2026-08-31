<script setup lang="ts">
import { computed, ref } from 'vue'
import { useRoute } from 'vue-router'
import AppIcon from './components/AppIcon.vue'
import AppSidebar from './components/AppSidebar.vue'
import AppTopBar from './components/AppTopBar.vue'
import LoginRequiredView from './views/LoginRequiredView.vue'
import { useAuth } from './composables/useAuth'

const route = useRoute()
const auth = useAuth()
const sidebarExpanded = ref(false)
const mobileSidebarOpen = ref(false)
const showTopBar = computed(() => route.name !== 'course' && route.name !== 'lesson')
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
      <AppTopBar v-if="showTopBar" />
      <v-main>
        <LoginRequiredView v-if="auth.status.value === 'signed-out'" />
        <router-view v-else />
      </v-main>
    </div>
  </v-app>
</template>
