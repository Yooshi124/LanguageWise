<script setup lang="ts">
import { toRef } from 'vue'
import GarryAssistant from '../components/GarryAssistant.vue'
import { provideAnalyticsUserId } from '../composables/useAnalyticsUser'
import type { HostContext } from '../models'

const props = defineProps<{ hostContext?: HostContext }>()

provideAnalyticsUserId(toRef(() => props.hostContext?.user?.id ?? null))
</script>

<template>
  <section class="feature-leaderboard-analytics">
    <RouterView />
    <GarryAssistant
      v-if="props.hostContext?.user"
      :key="props.hostContext.user.id"
      :user-id="props.hostContext.user.id"
      @unauthorized="props.hostContext.signIn()"
    />
  </section>
</template>