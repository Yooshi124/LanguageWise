<script setup lang="ts">
import { computed, onMounted, reactive, ref } from 'vue'
import { ApiError, profileApi } from '../api'
import type { Notification, Preferences, Profile } from '../models'

const emit = defineEmits<{ unauthorized: [] }>()
const profile = ref<Profile | null>(null)
const loading = ref(true)
const loadFailed = ref(false)
const saving = ref(false)
const saveMessage = ref('')
const saveFailed = ref(false)
const selectedNotification = ref<Notification | null>(null)
const dialog = ref<HTMLDialogElement | null>(null)
const achievementImage = '/remotes/quests-achievements/achievement.svg'
const preferences = reactive<Preferences>({
  email: '', notifyAll: true, notifyPostEngagement: true,
  notifyCourseCompletion: true, notifyQuizResults: true,
  notifyStreaks: true, notifyAchievements: true,
})

const completed = computed(() => profile.value?.achievements.filter(
  (achievement) => achievement.progressNeeded > 0 && achievement.progress >= achievement.progressNeeded,
).length ?? 0)
const boundedAchievementCount = computed(() => profile.value?.achievements.filter(
  (achievement) => achievement.progressNeeded > 0,
).length ?? 0)
const notifications = computed(() => [...(profile.value?.notifications ?? [])].sort(
  (left, right) => new Date(right.time).getTime() - new Date(left.time).getTime(),
))

function formatTrigger(trigger: string) {
  return trigger.split('-').map((word) => word.charAt(0).toUpperCase() + word.slice(1)).join(' ')
}

function formatTime(value: string) {
  const date = new Date(value)
  return Number.isNaN(date.getTime())
    ? 'Unknown time'
    : new Intl.DateTimeFormat(undefined, { dateStyle: 'medium', timeStyle: 'short' }).format(date)
}

async function load() {
  loading.value = true
  loadFailed.value = false
  try {
    profile.value = await profileApi.load()
    Object.assign(preferences, profile.value.preferences)
  } catch (error) {
    if (error instanceof ApiError && error.status === 401) emit('unauthorized')
    else loadFailed.value = true
  } finally {
    loading.value = false
  }
}

async function save() {
  saving.value = true
  saveMessage.value = ''
  saveFailed.value = false
  try {
    saveMessage.value = (await profileApi.savePreferences({ ...preferences })).message
  } catch (error) {
    saveFailed.value = true
    saveMessage.value = error instanceof ApiError && error.status === 400
      ? 'Enter a valid email address.'
      : 'Preferences could not be saved.'
  } finally {
    saving.value = false
  }
}

function openNotification(notification: Notification) {
  selectedNotification.value = notification
  dialog.value?.showModal()
}

function closeDialog() {
  dialog.value?.close()
  selectedNotification.value = null
}

onMounted(load)
</script>

<template>
  <div v-if="loading" class="lw-loading" role="status">Loading your progress...</div>
  <section v-else-if="loadFailed" class="lw-card lw-state lw-state--error">
    <h2 class="lw-card__title">Unable to load your profile</h2>
    <p>Please refresh the page and try again.</p>
    <button class="lw-command" type="button" @click="load">Try again</button>
  </section>
  <div v-else-if="profile" class="lw-dashboard">
    <section class="lw-profile-band" aria-labelledby="profile-name">
      <div><p class="lw-eyebrow">Learner profile</p><h2 id="profile-name">{{ profile.username }}</h2></div>
      <p class="lw-profile-band__summary">{{ completed }} of {{ boundedAchievementCount }} achievements complete</p>
    </section>

    <div class="lw-dashboard-grid">
      <section class="lw-card lw-preferences" aria-labelledby="preferences-title">
        <h2 id="preferences-title" class="lw-card__title">Email notifications</h2>
        <p class="lw-card__hint">Choose what arrives in your inbox.</p>
        <form @submit.prevent="save">
          <label class="lw-field"><span>Email address</span><input v-model="preferences.email" type="email" autocomplete="email" required></label>
          <label class="lw-master-toggle">
            <span><strong>All notifications</strong><small>Pause every email at once</small></span>
            <span class="lw-switch"><input v-model="preferences.notifyAll" type="checkbox" role="switch"><span class="lw-switch__track" aria-hidden="true"></span></span>
          </label>
          <fieldset class="notification-types" :disabled="!preferences.notifyAll">
            <legend>Notification types</legend>
            <label><input v-model="preferences.notifyPostEngagement" type="checkbox"> Post engagement</label>
            <label><input v-model="preferences.notifyCourseCompletion" type="checkbox"> Course completion</label>
            <label><input v-model="preferences.notifyQuizResults" type="checkbox"> Quiz results</label>
            <label><input v-model="preferences.notifyStreaks" type="checkbox"> Learning streaks</label>
            <label><input v-model="preferences.notifyAchievements" type="checkbox"> New achievements</label>
          </fieldset>
          <div class="lw-form-actions">
            <button class="lw-command" type="submit" :disabled="saving">{{ saving ? 'Saving...' : 'Save preferences' }}</button>
            <output class="save-status" :data-error="saveFailed" aria-live="polite">{{ saveMessage }}</output>
          </div>
        </form>
      </section>

      <section class="lw-achievements" aria-labelledby="achievements-title">
        <div class="lw-section-heading"><div><p class="lw-eyebrow">Your collection</p><h2 id="achievements-title">Achievements</h2></div><span class="lw-badge">{{ completed }} complete</span></div>
        <div class="lw-achievement-grid">
          <article v-for="achievement in profile.achievements" :key="achievement.achievementId" class="lw-achievement" :data-complete="achievement.progressNeeded > 0 && achievement.progress >= achievement.progressNeeded">
            <div class="lw-achievement__image-wrap"><img class="lw-achievement__image" :src="achievementImage" :alt="`${achievement.name} badge`"></div>
            <div class="lw-achievement__body">
              <div class="lw-achievement__heading"><h3>{{ achievement.name }}</h3><span class="lw-achievement__state">{{ achievement.progressNeeded < 0 ? 'Personal best' : achievement.progress >= achievement.progressNeeded ? 'Earned' : 'In progress' }}</span></div>
              <progress v-if="achievement.progressNeeded > 0" :value="achievement.progress" :max="achievement.progressNeeded" :aria-label="`${achievement.name} progress`"></progress>
              <p class="lw-achievement__progress">{{ achievement.progressNeeded < 0 ? `${achievement.progress} days` : `${achievement.progress} / ${achievement.progressNeeded}` }}</p>
            </div>
          </article>
        </div>
      </section>
    </div>

    <section class="lw-notifications" aria-labelledby="notifications-title">
      <div class="lw-section-heading"><div><p class="lw-eyebrow">Your updates</p><h2 id="notifications-title">Past notifications</h2></div><span class="lw-badge">{{ notifications.length }} {{ notifications.length === 1 ? 'update' : 'updates' }}</span></div>
      <p v-if="notifications.length === 0" class="lw-notifications__empty">No notifications yet.</p>
      <ol v-else class="lw-notification-list">
        <li v-for="notification in notifications" :key="notification.notificationId" class="lw-notification">
          <button class="lw-notification__open" type="button" @click="openNotification(notification)"><span class="lw-notification__subject">{{ notification.emailSubject }}</span><span class="lw-notification__meta">{{ formatTrigger(notification.trigger) }} &middot; {{ formatTime(notification.time) }}</span></button>
        </li>
      </ol>
    </section>
  </div>

  <dialog ref="dialog" class="lw-notification-dialog" aria-labelledby="notification-dialog-title" @click.self="closeDialog" @cancel.prevent="closeDialog" @keydown.esc.prevent="closeDialog">
    <div v-if="selectedNotification" class="lw-notification-dialog__heading">
      <div><p class="lw-notification-dialog__meta">{{ formatTrigger(selectedNotification.trigger) }} &middot; {{ formatTime(selectedNotification.time) }}</p><h2 id="notification-dialog-title">{{ selectedNotification.emailSubject }}</h2></div>
      <button class="lw-dialog-close" type="button" @click="closeDialog">Close</button>
    </div>
    <p v-if="selectedNotification" class="lw-notification-dialog__body">{{ selectedNotification.emailBody }}</p>
  </dialog>
</template>