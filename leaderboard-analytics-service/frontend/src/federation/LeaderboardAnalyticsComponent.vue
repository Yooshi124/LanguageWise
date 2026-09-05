<script setup lang="ts">
import GarryAssistant from '../components/GarryAssistant.vue'
import type { HostContext } from '../models'

const props = defineProps<{ hostContext?: HostContext }>()
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