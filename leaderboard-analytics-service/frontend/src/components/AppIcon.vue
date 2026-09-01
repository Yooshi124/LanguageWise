<script setup lang="ts">
import {
  mdiAccountCircleOutline,
  mdiArrowLeft,
  mdiArrowRight,
  mdiBookOpenPageVariantOutline,
  mdiCardsOutline,
  mdiChartBoxOutline,
  mdiClipboardCheckOutline,
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
  'arrow-left': mdiArrowLeft,
  'arrow-right': mdiArrowRight,
  close: mdiClose,
  courses: mdiBookOpenPageVariantOutline,
  discussion: mdiForumOutline,
  flashcards: mdiCardsOutline,
  games: mdiControllerClassicOutline,
  home: mdiHomeOutline,
  logout: mdiLogoutVariant,
  menu: mdiMenu,
  profile: mdiAccountCircleOutline,
  quizzes: mdiClipboardCheckOutline,
  quests: mdiTrophyOutline,
}
</script>

<template>
  <v-icon class="app-icon" :icon="icons[name]" :size="size ?? 24" aria-hidden="true" />
</template>
