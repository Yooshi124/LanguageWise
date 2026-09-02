<script setup lang="ts">
import { computed, onBeforeUnmount, ref } from 'vue'
import { useRoute } from 'vue-router'
import { useAuth } from '../composables/useAuth'
import { serviceNavigation } from '../config/navigation'
import AppIcon from './AppIcon.vue'
import SidebarNavItem from './SidebarNavItem.vue'

const auth = useAuth()
const route = useRoute()
const loggingOut = ref(false)
const logoutError = ref('')

const accountLabel = computed(() => {
  if (auth.status.value === 'authenticated') {
    return auth.username.value ?? 'Logged in'
  }
  if (auth.status.value === 'signed-out') {
    return 'Not logged in'
  }
  if (auth.status.value === 'error') {
    return 'Unable to verify login'
  }
  return 'Checking login'
})

const accountHref = computed(() =>
  auth.status.value === 'signed-out' ? auth.loginUrl() : undefined,
)

function isActive(href: string) {
  return href === '/' ? route.path === '/' : route.path.startsWith(href.replace(/\/$/, ''))
}

defineProps<{
  expanded: boolean
  mobileOpen: boolean
}>()

const emit = defineEmits<{
  'update:expanded': [value: boolean]
  'update:mobileOpen': [value: boolean]
}>()

let hoverTimer: ReturnType<typeof setTimeout> | undefined

function setExpanded(value: boolean) {
  clearTimeout(hoverTimer)
  emit('update:expanded', value)
}

function scheduleExpanded(value: boolean) {
  clearTimeout(hoverTimer)
  hoverTimer = setTimeout(() => setExpanded(value), value ? 140 : 220)
}

function handleFocusOut(event: FocusEvent) {
  const sidebar = event.currentTarget as HTMLElement
  if (!sidebar.contains(event.relatedTarget as Node | null)) {
    setExpanded(false)
  }
}

async function handleLogout() {
  loggingOut.value = true
  logoutError.value = ''

  try {
    await auth.logout()
  } catch (error) {
    logoutError.value = error instanceof Error ? error.message : 'Unable to log out'
  } finally {
    loggingOut.value = false
  }
}

onBeforeUnmount(() => clearTimeout(hoverTimer))
</script>

<template>
  <aside
    class="app-sidebar"
    :class="{ expanded, 'mobile-open': mobileOpen }"
    aria-label="LanguageWise services"
    @mouseenter="scheduleExpanded(true)"
    @mouseleave="scheduleExpanded(false)"
    @focusin="setExpanded(true)"
    @focusout="handleFocusOut"
  >
    <div class="sidebar-header">
      <a
        href="/"
        class="sidebar-brand"
        :aria-label="expanded || mobileOpen ? undefined : 'LanguageWise home'"
      >
        <img class="brand-mark" src="/languagewise-icon.png" alt="" />
        <span v-if="expanded || mobileOpen" class="brand-name">LanguageWise</span>
      </a>
      <button
        type="button"
        class="sidebar-toggle mobile-sidebar-close"
        aria-label="Close service navigation"
        @click="emit('update:mobileOpen', false)"
      >
        <AppIcon name="close" />
      </button>
    </div>

    <nav class="sidebar-services" aria-label="Services">
      <SidebarNavItem
        v-for="item in serviceNavigation"
        :key="item.label"
        :label="item.label"
        :icon="item.icon"
        :href="item.href"
        :active="isActive(item.href)"
        :show-label="expanded || mobileOpen"
      />
    </nav>

    <nav class="sidebar-utilities" aria-label="Account">
      <SidebarNavItem
        :label="accountLabel"
        icon="profile"
        :href="accountHref"
        :static="!accountHref"
        :show-label="expanded || mobileOpen"
      />
      <SidebarNavItem
        v-if="auth.isAuthenticated.value"
        :label="loggingOut ? 'Logging out' : 'Logout'"
        icon="logout"
        :disabled="loggingOut"
        :show-label="expanded || mobileOpen"
        @click="handleLogout"
      />
      <p v-if="logoutError" class="sidebar-account-error" role="alert">{{ logoutError }}</p>
    </nav>
  </aside>
</template>
