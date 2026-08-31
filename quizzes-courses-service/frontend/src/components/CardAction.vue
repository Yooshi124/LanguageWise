<script setup lang="ts">
import AppIcon from './AppIcon.vue'

defineProps<{
  label: string
  size?: number
}>()
</script>

<template>
  <span class="card-action-content">
    <span>{{ label }}</span>
    <AppIcon name="arrow-right" :size="size ?? 28" />
  </span>
</template>
