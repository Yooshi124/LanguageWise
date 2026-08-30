<script setup lang="ts">
import type { AppIconName } from '../config/navigation'
import AppIcon from './AppIcon.vue'

defineProps<{
  label: string
  icon: AppIconName
  showLabel: boolean
  href?: string
  active?: boolean
  disabled?: boolean
}>()
</script>

<template>
  <v-tooltip :text="label" location="end" :disabled="showLabel">
    <template #activator="{ props }">
      <a
        v-if="href"
        v-bind="props"
        :href="href"
        class="sidebar-nav-item"
        :class="{ active }"
        :aria-current="active ? 'page' : undefined"
        :aria-label="showLabel ? undefined : label"
      >
        <AppIcon :name="icon" />
        <span v-if="showLabel">{{ label }}</span>
      </a>
      <button
        v-else
        v-bind="props"
        type="button"
        class="sidebar-nav-item"
        disabled
        :aria-label="showLabel ? undefined : `${label} (coming soon)`"
      >
        <AppIcon :name="icon" />
        <span v-if="showLabel">{{ label }}</span>
        <span v-if="showLabel" class="sr-only">(coming soon)</span>
      </button>
    </template>
  </v-tooltip>
</template>
