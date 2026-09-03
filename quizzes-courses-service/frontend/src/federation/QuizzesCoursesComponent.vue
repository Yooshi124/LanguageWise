<script setup lang="ts">
import { computed, onBeforeUnmount } from 'vue'
import { useRoute } from 'vue-router'
import GarryAssistant from '../components/GarryAssistant.vue'
import type { FeatureHostContext } from './contracts'
import { setFeatureHostContext } from './featureHost'

const props = defineProps<{
  hostContext?: FeatureHostContext
}>()

const route = useRoute()
const showAssistant = computed(
  () => props.hostContext?.user != null && !route.meta.hideAssistant,
)

setFeatureHostContext(props.hostContext)
onBeforeUnmount(() => setFeatureHostContext(undefined))
</script>

<template>
  <section class="feature-quizzes-courses">
    <router-view />
    <GarryAssistant
      v-if="showAssistant && hostContext?.user"
      :key="hostContext.user.id"
      :user-id="hostContext.user.id"
    />
  </section>
</template>