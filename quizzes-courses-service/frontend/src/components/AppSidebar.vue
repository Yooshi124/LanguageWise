<script setup lang="ts">
import { onBeforeUnmount } from 'vue'
import { serviceNavigation, sharedHomeHref, utilityNavigation } from '../config/navigation'
import AppIcon from './AppIcon.vue'
import SidebarNavItem from './SidebarNavItem.vue'

const brandIconUrl = `${import.meta.env.BASE_URL}languagewise-icon.png`

const props = defineProps<{
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
        :href="sharedHomeHref"
        class="sidebar-brand"
        :aria-label="expanded || mobileOpen ? undefined : 'LanguageWise home'"
      >
        <img class="brand-mark" :src="brandIconUrl" alt="" />
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
        :active="item.current"
        :disabled="item.disabled"
        :show-label="expanded || mobileOpen"
      />
    </nav>

    <nav class="sidebar-utilities" aria-label="Account">
      <SidebarNavItem
        v-for="item in utilityNavigation"
        :key="item.label"
        :label="item.label"
        :icon="item.icon"
        :disabled="item.disabled"
        :show-label="expanded || mobileOpen"
      />
    </nav>
  </aside>
</template>
