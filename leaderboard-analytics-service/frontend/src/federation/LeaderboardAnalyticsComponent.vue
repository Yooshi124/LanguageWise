<script setup lang="ts">
import { toRef } from 'vue'
import { provideAnalyticsUserId } from '../composables/useAnalyticsUser'
import type { HostContext } from '../models'

const props = defineProps<{ hostContext?: HostContext }>()

provideAnalyticsUserId(toRef(() => props.hostContext?.user?.id ?? null))
</script>

<template>
  <section class="feature-leaderboard-analytics">
    <RouterView />
  </section>
</template>