<script setup lang="ts">
import { computed } from 'vue'
import { useRoute } from 'vue-router'
import AppIcon from '../components/AppIcon.vue'
import { useAuth } from '../composables/useAuth'

const route = useRoute()
const auth = useAuth()
const loginHref = computed(() => {
  void route.fullPath
  return auth.loginUrl()
})
</script>

<template>
  <v-container class="wip-page">
    <v-card
      max-width="680"
      rounded="xl"
      elevation="0"
      class="wip-card text-center pa-12"
    >
      <AppIcon name="profile" :size="64" class="login-required-icon" />
      <v-chip color="primary" variant="tonal" class="mb-6">Login required</v-chip>
      <h1>You are not logged in</h1>
      <p>Sign in to access your LanguageWise courses, lessons, quizzes, and flashcards.</p>
      <v-btn :href="loginHref" color="primary" size="large" rounded="lg">Sign in</v-btn>
    </v-card>
  </v-container>
</template>
