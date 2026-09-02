<script setup lang="ts">
import { computed, ref } from 'vue'
import { useAuth } from '../composables/useAuth'

const auth = useAuth()
const username = ref('')
const password = ref('')
const submitting = ref(false)
const errorMessage = ref('')
const canSubmit = computed(() => username.value.trim() !== '' && password.value !== '')

function safeReturnUrl() {
  const requested = new URLSearchParams(window.location.search).get('returnUrl')
  const fallback =
    document.referrer && new URL(document.referrer).origin === window.location.origin
      ? new URL(document.referrer).pathname
      : '/'
  const resolved = new URL(requested || fallback, window.location.origin)

  return resolved.origin === window.location.origin
    ? `${resolved.pathname}${resolved.search}${resolved.hash}`
    : '/'
}

async function submit() {
  if (!canSubmit.value || submitting.value) {
    return
  }

  submitting.value = true
  errorMessage.value = ''

  try {
    await auth.login(username.value.trim(), password.value)
    window.location.assign(safeReturnUrl())
  } catch (error) {
    errorMessage.value =
      error instanceof Error ? error.message : 'Unable to reach the server. Please try again later.'
  } finally {
    submitting.value = false
  }
}
</script>

<template>
  <main class="login-page">
    <section class="login-shell" aria-labelledby="login-title">
      <div class="login-brand-panel">
        <a class="login-brand" href="/" aria-label="LanguageWise home">
          <img src="/languagewise-icon.png" alt="" />
          <span>LanguageWise</span>
        </a>
        <div class="login-brand-copy">
          <h2>Learn a language.<br /><span>Open your world.</span></h2>
          <p>Courses, practice, community, and progress tracking in one place.</p>
        </div>
        <p class="login-promise">Learn &middot; Practice &middot; Grow</p>
      </div>

      <div class="login-form-panel">
        <p class="login-eyebrow">Welcome back</p>
        <h1 id="login-title">Continue your journey</h1>
        <p class="login-subtitle">Sign in to pick up where you left off.</p>

        <v-alert
          v-if="errorMessage"
          type="error"
          variant="tonal"
          density="compact"
          class="mb-5"
          role="alert"
        >
          {{ errorMessage }}
        </v-alert>

        <v-form @submit.prevent="submit">
          <v-text-field
            v-model="username"
            label="Username"
            autocomplete="username"
            variant="outlined"
            autofocus
            :disabled="submitting"
          />
          <v-text-field
            v-model="password"
            label="Password"
            type="password"
            autocomplete="current-password"
            variant="outlined"
            :disabled="submitting"
          />
          <v-btn
            type="submit"
            color="primary"
            size="large"
            block
            :loading="submitting"
            :disabled="!canSubmit"
          >
            Sign in
          </v-btn>
        </v-form>
        <p class="login-account-note">One account for every LanguageWise experience.</p>
      </div>
    </section>
  </main>
</template>
