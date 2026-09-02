<script setup lang="ts">
import type { HostContext } from '../models'

const props = defineProps<{ hostContext?: HostContext }>()
</script>

<template>
  <section class="feature-quests-achievements">
    <RouterView v-if="props.hostContext?.user" v-slot="{ Component }">
      <component :is="Component" @unauthorized="props.hostContext.signIn()" />
    </RouterView>
    <section v-else class="lw-card lw-state">
      <h2 class="lw-card__title">Sign in required</h2>
      <p>Your achievements and preferences are linked to your LanguageWise account.</p>
      <button class="lw-command" type="button" @click="props.hostContext?.signIn()">Sign in</button>
    </section>
  </section>
</template>