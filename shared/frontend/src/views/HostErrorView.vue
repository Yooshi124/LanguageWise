<script setup lang="ts">
import { ref } from 'vue'

const props = defineProps<{
  message: string
  retry: () => Promise<void>
}>()

const retrying = ref(false)

async function retry() {
  retrying.value = true
  try {
    await props.retry()
  } finally {
    retrying.value = false
  }
}
</script>

<template>
  <section class="host-state host-state--error" role="alert" aria-labelledby="host-error-title">
    <div>
      <p class="host-state__eyebrow">Something went wrong</p>
      <h1 id="host-error-title">The shared experience could not continue.</h1>
      <p>{{ message }}</p>
      <v-btn color="primary" variant="flat" :loading="retrying" @click="retry">Retry</v-btn>
    </div>
  </section>
</template>