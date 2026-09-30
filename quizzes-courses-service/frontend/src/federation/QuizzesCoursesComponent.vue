<script setup lang="ts">
import { onBeforeUnmount } from 'vue'
import AppTopBar from '../components/AppTopBar.vue'
import type { FeatureHostContext } from './contracts'
import { setFeatureHostContext } from './featureHost'

const props = defineProps<{
  hostContext?: FeatureHostContext
}>()

setFeatureHostContext(props.hostContext)
onBeforeUnmount(() => setFeatureHostContext(undefined))
</script>

<template>
  <section class="feature-quizzes-courses">
    <AppTopBar />
    <router-view />
  </section>
</template>