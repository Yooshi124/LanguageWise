<script setup lang="ts">
import { ref } from 'vue'

const aiMode = ref(false)
const mcp = ref(false)
const rag = ref(false)
</script>

<template>
  <section class="account-page" aria-labelledby="admin-settings-title">
    <p class="host-state__eyebrow">Administration</p>
    <h1 id="admin-settings-title">Admin Settings</h1>

    <v-card class="account-card" variant="flat">
      <v-card-text>
        <v-switch v-model="aiMode" label="Enable AI Mode" color="primary" inset hide-details />
        <v-switch v-model="mcp" label="Enable Model Context Protocol (MCP)" color="primary" inset hide-details />
        <v-switch v-model="rag" label="Enable Retrieval-Augmented Generation (RAG)" color="primary" inset hide-details />
      </v-card-text>
    </v-card>
  </section>
</template>
