<script setup lang="ts">
import LanguageSelection from '../components/LanguageSelection.vue'

defineProps<{ kind: 'quizzes' | 'flashcards' }>()
</script>

<template>
  <LanguageSelection :kind="kind" />
</template>
