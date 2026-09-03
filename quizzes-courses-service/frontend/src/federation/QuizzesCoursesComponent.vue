<script setup lang="ts">
import { computed, onBeforeUnmount } from 'vue'
import { useRoute } from 'vue-router'
import AppTopBar from '../components/AppTopBar.vue'
import GarryAssistant from '../components/GarryAssistant.vue'
import type { FeatureHostContext } from './contracts'
import { setFeatureHostContext } from './featureHost'

const props = defineProps<{
  hostContext?: FeatureHostContext
}>()

const route = useRoute()
const showTopBar = computed(
  () => route.name !== 'course' && route.name !== 'lesson' && !route.meta.hideTopBar,
)
const showAssistant = computed(
  () => props.hostContext?.user != null && !route.meta.hideAssistant,
)

setFeatureHostContext(props.hostContext)
onBeforeUnmount(() => setFeatureHostContext(undefined))
</script>

<template>
  <section class="feature-quizzes-courses">
    <AppTopBar v-if="showTopBar" />
    <router-view />
    <GarryAssistant
      v-if="showAssistant && hostContext?.user"
      :key="hostContext.user.id"
      :user-id="hostContext.user.id"
    />
  </section>
</template>