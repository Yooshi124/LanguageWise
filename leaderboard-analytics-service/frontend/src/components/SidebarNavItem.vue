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
  static?: boolean
}>()

const emit = defineEmits<{
  click: []
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
        v-else-if="!static"
        v-bind="props"
        type="button"
        class="sidebar-nav-item"
        :disabled="disabled"
        :aria-label="showLabel ? undefined : label"
        @click="emit('click')"
      >
        <AppIcon :name="icon" />
        <span v-if="showLabel">{{ label }}</span>
      </button>
      <div
        v-else
        v-bind="props"
        class="sidebar-nav-item sidebar-nav-item-static"
        :aria-label="showLabel ? undefined : label"
      >
        <AppIcon :name="icon" />
        <span v-if="showLabel">{{ label }}</span>
      </div>
    </template>
  </v-tooltip>
</template>
