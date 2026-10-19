<script setup lang="ts">
import { ref, watch } from 'vue'

const aiMode = ref(false)
const mcp = ref(false)
const rag = ref(false)

watch(aiMode, (enabled) => {
  if (!enabled) {
    mcp.value = false
    rag.value = false
  }
})
</script>

<template>
  <section class="account-page" aria-labelledby="admin-settings-title">
    <p class="host-state__eyebrow">Administration</p>
    <h1 id="admin-settings-title">Admin Settings</h1>

    <v-card class="account-card" variant="flat">
      <v-card-text>
        <v-switch v-model="aiMode" class="admin-switch" label="Enable AI Mode" color="primary" inset hide-details />
        <v-switch
          v-model="mcp"
          class="admin-switch"
          label="Enable Model Context Protocol (MCP)"
          color="primary"
          inset
          hide-details
          :disabled="!aiMode"
        />
        <v-switch
          v-model="rag"
          class="admin-switch"
          label="Enable Retrieval-Augmented Generation (RAG)"
          color="primary"
          inset
          hide-details
          :disabled="!aiMode"
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
