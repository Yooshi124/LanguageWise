<script setup lang="ts">
import { computed } from 'vue'
import { useRoute } from 'vue-router'

const route = useRoute()
const loginHref = computed(() => {
  const returnUrl = typeof route.query.returnUrl === 'string' ? route.query.returnUrl : '/'
  return `/login?returnUrl=${encodeURIComponent(returnUrl)}`
})
</script>

<template>
  <section class="host-state" aria-labelledby="signed-out-title">
    <div>
      <p class="host-state__eyebrow">Sign in required</p>
      <h1 id="signed-out-title">Continue with your LanguageWise account.</h1>
      <p>This area uses your account to keep learning progress private.</p>
      <v-btn color="primary" variant="flat" :href="loginHref">Sign in</v-btn>
    </div>
  </section>
</template>