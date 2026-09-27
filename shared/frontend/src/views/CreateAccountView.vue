<script setup lang="ts">
import { computed, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import ProfilePicturePicker from '../components/ProfilePicturePicker.vue'
import { uploadProfilePicture, type PendingPicture } from '../composables/profilePicture'
import { useAuth } from '../composables/useAuth'

const auth = useAuth()
const route = useRoute()
const router = useRouter()
const username = ref('')
const password = ref('')
const confirmPassword = ref('')
const picture = ref<PendingPicture | null>(null)
const submitting = ref(false)
const errorMessage = ref('')
const accountCreated = ref(false)

const usernameRules = [
  (value: string) => /^[A-Za-z0-9._-]{3,32}$/.test(value.trim()) ||
    "3-32 characters: letters, numbers, '.', '_' or '-'.",
]
const passwordRules = [(value: string) => value.length >= 8 || 'At least 8 characters.']
const confirmRules = [(value: string) => value === password.value || 'Passwords do not match.']

const canSubmit = computed(
  () =>
    usernameRules.every((rule) => rule(username.value) === true) &&
    passwordRules.every((rule) => rule(password.value) === true) &&
    confirmPassword.value === password.value,
)

function safeReturnUrl() {
  const requested = typeof route.query.returnUrl === 'string' ? route.query.returnUrl : '/'
  const resolved = new URL(requested, window.location.origin)

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
    await auth.createAccount(username.value.trim(), password.value)
  } catch (error) {
    errorMessage.value =
      error instanceof Error ? error.message : 'Unable to reach the server. Please try again later.'
    submitting.value = false
    return
  }

  accountCreated.value = true
  if (picture.value) {
    try {
      await uploadProfilePicture(auth.user.value!.id, picture.value.file)
    } catch (error) {
      const reason = error instanceof Error ? error.message : 'Unable to upload your picture.'
      errorMessage.value = `Your account was created, but the picture was not saved: ${reason}`
      submitting.value = false
      return
    }
  }

  await router.replace(safeReturnUrl())
}

function continueWithoutPicture() {
  router.replace(safeReturnUrl())
}
</script>

<template>
  <main class="login-page">
    <section class="login-shell" aria-labelledby="create-account-title">
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
        <p class="login-eyebrow">Get started</p>
        <h1 id="create-account-title">Create your account</h1>
        <p class="login-subtitle">Pick a username and password to start learning.</p>

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

        <v-btn
          v-if="accountCreated"
          color="primary"
          size="large"
          block
          @click="continueWithoutPicture"
        >
          Continue
        </v-btn>

        <v-form v-else @submit.prevent="submit">
          <v-text-field
            v-model="username"
            label="Username"
            autocomplete="username"
            variant="outlined"
            autofocus
            :rules="usernameRules"
            :disabled="submitting"
          />
          <v-text-field
            v-model="password"
            label="Password"
            type="password"
            autocomplete="new-password"
            variant="outlined"
            :rules="passwordRules"
            :disabled="submitting"
          />
          <v-text-field
            v-model="confirmPassword"
            label="Confirm password"
            type="password"
            autocomplete="new-password"
            variant="outlined"
            :rules="confirmRules"
            :disabled="submitting"
          />
          <ProfilePicturePicker v-model="picture" :busy="submitting" />
          <v-btn
            type="submit"
            color="primary"
            size="large"
            block
            :loading="submitting"
            :disabled="!canSubmit"
          >
            Create account
          </v-btn>
        </v-form>
        <template v-if="!accountCreated">
          <p class="login-divider">Already have an account?</p>
          <v-btn
            :to="{ name: 'login', query: route.query }"
            variant="outlined"
            color="primary"
            size="large"
            block
            :disabled="submitting"
          >
            Sign in
          </v-btn>
        </template>
      </div>
    </section>
  </main>
</template>
