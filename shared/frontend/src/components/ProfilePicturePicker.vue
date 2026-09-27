<script setup lang="ts">
import { onBeforeUnmount, ref, watch } from 'vue'
import {
  ACCEPTED_PICTURE_TYPES,
  MAX_PICTURE_BYTES,
  type PendingPicture,
} from '../composables/profilePicture'

defineProps<{ busy?: boolean }>()

const pending = defineModel<PendingPicture | null>({ default: null })
const message = ref('')
const maxMegabytes = Math.round(MAX_PICTURE_BYTES / (1024 * 1024))

function choose(event: Event) {
  const input = event.target as HTMLInputElement
  const file = input.files?.[0]
  // Clearing the input lets the same file be picked again after it is removed.
  input.value = ''
  if (!file) {
    return
  }

  if (!ACCEPTED_PICTURE_TYPES.includes(file.type)) {
    message.value = `${file.name} is not a PNG, JPEG, GIF or WebP image.`
    return
  }

  if (file.size > MAX_PICTURE_BYTES) {
    message.value = `${file.name} is larger than ${maxMegabytes} MB.`
    return
  }

  message.value = ''
  pending.value = { file, url: URL.createObjectURL(file) }
}

watch(pending, (_, previous) => {
  if (previous) {
    URL.revokeObjectURL(previous.url)
  }
})

onBeforeUnmount(() => {
  if (pending.value) {
    URL.revokeObjectURL(pending.value.url)
  }
})
</script>

<template>
  <div class="profile-picker">
    <p id="profile-picture-label" class="profile-picker__label">Profile picture</p>
    <p class="profile-picker__hint">Optional. PNG, JPEG, GIF or WebP, up to {{ maxMegabytes }} MB.</p>
    <p v-if="message" class="profile-picker__message" role="alert">{{ message }}</p>
    <div v-if="pending" class="profile-picker__item">
      <img class="profile-picker__thumb" :src="pending.url" :alt="pending.file.name" />
      <v-btn size="small" variant="text" :disabled="busy" @click="pending = null">Remove</v-btn>
    </div>
    <input
      v-else
      class="profile-picker__input"
      type="file"
      :accept="ACCEPTED_PICTURE_TYPES.join(',')"
      :disabled="busy"
      aria-labelledby="profile-picture-label"
      @change="choose"
    />
  </div>
</template>
