<script setup lang="ts">
import { computed, onErrorCaptured, onMounted, ref, watch } from 'vue'
import { useRoute } from 'vue-router'
import AppIcon from './components/AppIcon.vue'
import AppSidebar from './components/AppSidebar.vue'
import { useAuth } from './composables/useAuth'
import HostErrorView from './views/HostErrorView.vue'
import HostLoadingView from './views/HostLoadingView.vue'

const route = useRoute()
const auth = useAuth()
const sidebarExpanded = ref(false)
const mobileSidebarOpen = ref(false)
const hostError = ref('')
const renderKey = ref(0)
const showShell = computed(() => route.name !== 'login')

async function bootstrapAuthentication() {
  hostError.value = ''

  try {
    await auth.ensureAuthenticated()
  } catch {
    hostError.value = 'LanguageWise could not verify your session.'
  }
}

async function retryHost() {
  renderKey.value += 1
  await bootstrapAuthentication()
}

onMounted(bootstrapAuthentication)
onErrorCaptured((error) => {
  hostError.value = error instanceof Error ? error.message : 'An unexpected error occurred.'
  return false
})
watch(() => route.fullPath, () => {
  hostError.value = ''
})
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
        <HostErrorView v-if="hostError" :message="hostError" :retry="retryHost" />
        <HostLoadingView v-else-if="auth.status.value === 'loading'" />
        <router-view v-else :key="`${route.fullPath}:${renderKey}`" />
      </v-main>
    </div>
  </v-app>
  <v-app v-else>
    <router-view />
  </v-app>
</template>
