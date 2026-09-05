<script setup lang="ts">
import GarryAssistant from '../components/GarryAssistant.vue'
import type { HostContext } from '../models'

const props = defineProps<{ hostContext?: HostContext }>()
</script>

<template>
  <section class="feature-quests-achievements">
    <RouterView v-if="props.hostContext?.user" v-slot="{ Component }">
      <component :is="Component" @unauthorized="props.hostContext.signIn()" />
    </RouterView>
    <GarryAssistant
      v-if="props.hostContext?.user"
      :key="props.hostContext.user.id"
      :user-id="props.hostContext.user.id"
      @unauthorized="props.hostContext.signIn()"
    />
    <section v-else class="lw-card lw-state">
      <h2 class="lw-card__title">Sign in required</h2>
      <p>Your achievements and preferences are linked to your LanguageWise account.</p>
      <button class="lw-command" type="button" @click="props.hostContext?.signIn()">Sign in</button>
    </section>
  </section>
</template>