<script setup lang="ts">
import {
  mdiAccountCircleOutline,
  mdiBookOpenPageVariantOutline,
  mdiChartBoxOutline,
  mdiClose,
  mdiControllerClassicOutline,
  mdiForumOutline,
  mdiHomeOutline,
  mdiLogoutVariant,
  mdiMenu,
  mdiTrophyOutline,
} from '@mdi/js'
import type { AppIconName } from '../config/navigation'

defineProps<{
  name: AppIconName
  size?: number
}>()

const icons: Record<AppIconName, string> = {
  analytics: mdiChartBoxOutline,
  close: mdiClose,
  courses: mdiBookOpenPageVariantOutline,
  discussion: mdiForumOutline,
  games: mdiControllerClassicOutline,
  home: mdiHomeOutline,
  logout: mdiLogoutVariant,
  menu: mdiMenu,
  profile: mdiAccountCircleOutline,
  quests: mdiTrophyOutline,
}
</script>

<template>
  <v-icon class="app-icon" :icon="icons[name]" :size="size ?? 24" aria-hidden="true" />
</template>
