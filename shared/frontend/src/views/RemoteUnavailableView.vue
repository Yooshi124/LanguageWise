<script setup lang="ts">
import { ref } from 'vue'
import AppIcon from '../components/AppIcon.vue'

const props = defineProps<{
  featureName: string
  retry: () => Promise<void>
}>()

const retrying = ref(false)
const retryFailed = ref(false)

async function retry() {
  retrying.value = true
  retryFailed.value = false

  try {
    await props.retry()
  } catch {
    retryFailed.value = true
  } finally {
    retrying.value = false
  }
}
</script>

<template>
  <section class="remote-unavailable" role="alert" aria-labelledby="remote-unavailable-title">
    <div class="remote-unavailable__icon"><AppIcon name="courses" :size="34" /></div>
    <p class="remote-unavailable__eyebrow">Feature unavailable</p>
    <h1 id="remote-unavailable-title">{{ featureName }} could not be loaded.</h1>
    <p>The rest of LanguageWise is still available. Retry when the feature service is back.</p>
    <p v-if="retryFailed" role="status">The feature is still unavailable.</p>
    <v-btn color="primary" variant="flat" :loading="retrying" @click="retry">Retry</v-btn>
  </section>
</template>