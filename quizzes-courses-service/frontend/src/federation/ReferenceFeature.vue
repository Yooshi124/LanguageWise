<script setup lang="ts">
import { computed } from 'vue'
import { mdiConnection, mdiOpenInNew } from '@mdi/js'
import { useRoute } from 'vue-router'
import type { FeatureHostContext } from './contracts'

const props = defineProps<{
  hostContext: FeatureHostContext
}>()

const route = useRoute()
const onDetailsPage = computed(() => route.name === 'federation-reference-details')
</script>

<template>
  <section class="federation-spike" aria-labelledby="federation-spike-title">
    <v-card class="federation-spike__panel" elevation="2">
      <v-card-item>
        <template #prepend>
          <v-avatar color="primary" variant="tonal" rounded="lg">
            <v-icon :icon="mdiConnection" />
          </v-avatar>
        </template>
        <v-card-title id="federation-spike-title" tag="h1">Federation reference remote</v-card-title>
        <v-card-subtitle>Rendered by Quizzes and Courses inside the Shared host.</v-card-subtitle>
      </v-card-item>

      <v-card-text>
        <dl class="federation-spike__facts">
          <div><dt>Host route</dt><dd>{{ route.fullPath }}</dd></div>
          <div><dt>Authenticated user</dt><dd>{{ hostContext.user?.name ?? 'Phase 2 contract pending' }}</dd></div>
          <div><dt>Vue application</dt><dd>Shared singleton</dd></div>
          <div><dt>Router and Vuetify</dt><dd>Provided by the host</dd></div>
        </dl>

        <p v-if="onDetailsPage" class="federation-spike__detail" role="status">
          This child route was registered dynamically from the remote module.
        </p>
      </v-card-text>

      <v-card-actions>
        <v-btn
          v-if="!onDetailsPage"
          color="primary"
          variant="flat"
          append-icon=""
          @click="hostContext.navigate('/federation-spike/details')"
        >
          Open child route
          <v-icon :icon="mdiOpenInNew" end />
        </v-btn>
        <v-btn v-else variant="text" @click="hostContext.navigate('/federation-spike')">
          Back to reference
        </v-btn>
      </v-card-actions>
    </v-card>
  </section>
</template>