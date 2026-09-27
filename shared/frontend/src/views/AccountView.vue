<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import ProfilePicturePicker from '../components/ProfilePicturePicker.vue'
import {
  deleteProfilePicture,
  fetchProfilePicture,
  profilePictureContentUrl,
  uploadProfilePicture,
  type PendingPicture,
  type ProfilePicture,
} from '../composables/profilePicture'
import { useAuth } from '../composables/useAuth'

const auth = useAuth()
const userId = computed(() => auth.user.value!.id)

const username = ref(auth.username.value ?? '')
const newPassword = ref('')
const confirmPassword = ref('')
const currentPassword = ref('')
const saving = ref(false)
const detailsError = ref('')
const detailsSaved = ref(false)

const picture = ref<ProfilePicture | null>(null)
const pendingPicture = ref<PendingPicture | null>(null)
const pictureBusy = ref(false)
const pictureError = ref('')

const usernameRules = [
  (value: string) => /^[A-Za-z0-9._-]{3,32}$/.test(value.trim()) ||
    "3-32 characters: letters, numbers, '.', '_' or '-'.",
]
const newPasswordRules = [
  (value: string) => value === '' || value.length >= 8 || 'At least 8 characters.',
]
const confirmRules = [(value: string) => value === newPassword.value || 'Passwords do not match.']

const usernameChanged = computed(() => username.value.trim() !== auth.username.value)
const canSave = computed(
  () =>
    (usernameChanged.value || newPassword.value !== '') &&
    usernameRules.every((rule) => rule(username.value) === true) &&
    newPasswordRules.every((rule) => rule(newPassword.value) === true) &&
    confirmPassword.value === newPassword.value &&
    currentPassword.value !== '',
)

const pictureUrl = computed(() =>
  picture.value ? profilePictureContentUrl(userId.value, picture.value) : null,
)
const initial = computed(() => (auth.username.value ?? '?').charAt(0).toUpperCase())

async function saveDetails() {
  if (!canSave.value || saving.value) {
    return
  }

  saving.value = true
  detailsError.value = ''
  detailsSaved.value = false

  try {
    await auth.updateAccount({
      username: usernameChanged.value ? username.value.trim() : undefined,
      newPassword: newPassword.value || undefined,
      currentPassword: currentPassword.value,
    })
    username.value = auth.username.value ?? username.value
    newPassword.value = ''
    confirmPassword.value = ''
    currentPassword.value = ''
    detailsSaved.value = true
  } catch (error) {
    detailsError.value =
      error instanceof Error ? error.message : 'Unable to reach the server. Please try again later.'
  } finally {
    saving.value = false
  }
}

async function savePicture() {
  if (!pendingPicture.value || pictureBusy.value) {
    return
  }

  pictureBusy.value = true
  pictureError.value = ''
  try {
    picture.value = await uploadProfilePicture(userId.value, pendingPicture.value.file)
    pendingPicture.value = null
  } catch (error) {
    pictureError.value = error instanceof Error ? error.message : 'Unable to upload your picture.'
  } finally {
    pictureBusy.value = false
  }
}

async function removePicture() {
  pictureBusy.value = true
  pictureError.value = ''
  try {
    await deleteProfilePicture(userId.value)
    picture.value = null
  } catch (error) {
    pictureError.value = error instanceof Error ? error.message : 'Unable to remove your picture.'
  } finally {
    pictureBusy.value = false
  }
}

onMounted(async () => {
  try {
    picture.value = await fetchProfilePicture(userId.value)
  } catch (error) {
    pictureError.value = error instanceof Error ? error.message : 'Unable to load your profile picture.'
  }
})
</script>

<template>
  <section class="account-page" aria-labelledby="account-title">
    <p class="host-state__eyebrow">Your account</p>
    <h1 id="account-title">Manage your details</h1>

    <v-card class="account-card" variant="flat">
      <v-card-title>Profile picture</v-card-title>
      <v-card-text class="account-picture">
        <template v-if="picture">
          <v-avatar size="96" color="primary">
            <img :src="pictureUrl!" alt="Your profile picture" />
          </v-avatar>
          <v-btn variant="text" :loading="pictureBusy" @click="removePicture">Remove picture</v-btn>
        </template>
        <template v-else>
          <v-avatar v-if="!pendingPicture" size="96" color="primary">
            <span class="account-initial">{{ initial }}</span>
          </v-avatar>
          <div class="account-picture-actions">
            <ProfilePicturePicker v-model="pendingPicture" :busy="pictureBusy" />
            <v-btn
              v-if="pendingPicture"
              color="primary"
              variant="flat"
              :loading="pictureBusy"
              @click="savePicture"
            >
              Save picture
            </v-btn>
          </div>
        </template>
      </v-card-text>
      <v-card-text v-if="pictureError" class="pt-0">
        <v-alert type="error" variant="tonal" density="compact" role="alert">{{ pictureError }}</v-alert>
      </v-card-text>
    </v-card>

    <v-card class="account-card" variant="flat">
      <v-card-title>Account details</v-card-title>
      <v-card-text>
        <v-alert
          v-if="detailsError"
          type="error"
          variant="tonal"
          density="compact"
          class="mb-5"
          role="alert"
        >
          {{ detailsError }}
        </v-alert>
        <v-alert
          v-else-if="detailsSaved"
          type="success"
          variant="tonal"
          density="compact"
          class="mb-5"
          role="status"
        >
          Your account has been updated.
        </v-alert>

        <v-form @submit.prevent="saveDetails">
          <v-text-field
            v-model="username"
            label="Username"
            autocomplete="username"
            variant="outlined"
            :rules="usernameRules"
            :disabled="saving"
          />
          <v-text-field
            v-model="newPassword"
            label="New password (optional)"
            type="password"
            autocomplete="new-password"
            variant="outlined"
            :rules="newPasswordRules"
            :disabled="saving"
          />
          <v-text-field
            v-if="newPassword"
            v-model="confirmPassword"
            label="Confirm new password"
            type="password"
            autocomplete="new-password"
            variant="outlined"
            :rules="confirmRules"
            :disabled="saving"
          />
          <v-text-field
            v-model="currentPassword"
            label="Current password"
            type="password"
            autocomplete="current-password"
            variant="outlined"
            hint="Required to save changes."
            persistent-hint
            :disabled="saving"
          />
          <v-btn
            type="submit"
            color="primary"
            size="large"
            class="mt-4"
            :loading="saving"
            :disabled="!canSave"
          >
            Save changes
          </v-btn>
        </v-form>
      </v-card-text>
    </v-card>
  </section>
</template>
