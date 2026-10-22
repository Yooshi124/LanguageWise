<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { useAdminSettings } from '../composables/useAdminSettings'

const adminSettings = useAdminSettings()
const settings = adminSettings.settings
const aiMode = adminSettings.aiMode
const loading = ref(true)
const saving = ref(false)
const error = ref('')

async function load() {
  try {
    await adminSettings.load()
  } catch (cause) {
    error.value = cause instanceof Error ? cause.message : 'Unable to load settings.'
  } finally {
    loading.value = false
  }
}

async function toggle(name: string, isEnabled: boolean | null) {
  saving.value = true
  error.value = ''
  try {
    const response = await fetch(`/api/admin-settings/${encodeURIComponent(name)}`, {
      method: 'PUT',
      credentials: 'same-origin',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ isEnabled: isEnabled === true }),
    })
    if (!response.ok) {
      throw new Error(`Unable to save setting (${response.status} ${response.statusText})`)
    }
  } catch (cause) {
    error.value = cause instanceof Error ? cause.message : 'Unable to save setting.'
  } finally {
    // Reload so the switches show the stored state, including dependents turned off with AI Mode.
    await load()
    saving.value = false
  }
}

onMounted(load)
</script>

<template>
  <section class="account-page" aria-labelledby="admin-settings-title">
    <p class="host-state__eyebrow">Administration</p>
    <h1 id="admin-settings-title">Admin Settings</h1>

    <v-alert v-if="error" type="error" variant="tonal" density="compact" class="mb-4" :text="error" />

    <v-card class="account-card" variant="flat">
      <v-card-text>
        <v-switch
          :model-value="settings.AIMode"
          class="admin-switch"
          label="Enable AI Mode"
          color="primary"
          inset
          hide-details
          :disabled="loading || saving"
          @update:model-value="toggle('AIMode', $event)"
        />
        <v-switch
          :model-value="settings.MCP"
          class="admin-switch"
          label="Enable Model Context Protocol (MCP)"
          color="primary"
          inset
          hide-details
          :disabled="loading || saving || !aiMode"
          @update:model-value="toggle('MCP', $event)"
        />
        <v-switch
          :model-value="settings.RAG"
          class="admin-switch"
          label="Enable Retrieval-Augmented Generation (RAG)"
          color="primary"
          inset
          hide-details
          :disabled="loading || saving || !aiMode"
          @update:model-value="toggle('RAG', $event)"
        />
      </v-card-text>
    </v-card>
  </section>
</template>

<style scoped>
.admin-switch :deep(.v-selection-control__wrapper) {
  transform: scale(0.8);
  transform-origin: left center;
  margin-right: -10px;
}

.admin-switch :deep(.v-label) {
  font-size: 0.875rem;
}
</style>
