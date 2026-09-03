<script setup lang="ts">
import { computed } from 'vue'
import { useRoute } from 'vue-router'

const route = useRoute()

const activeSection = computed(() => {
  const name = String(route.name ?? '')
  if (['quizzes', 'quiz-list', 'quiz-runner'].includes(name)) return 'quizzes'
  if (['flashcards', 'flashcard-decks', 'flashcard-revision'].includes(name)) return 'flashcards'
  return 'courses'
})

const items = [
  { label: 'Courses', section: 'courses', to: { name: 'quizzes-courses-home' } },
  { label: 'Quizzes', section: 'quizzes', to: { name: 'quizzes' } },
  { label: 'Flashcards', section: 'flashcards', to: { name: 'flashcards' } },
] as const
</script>

<template>
  <header class="local-topbar">
    <nav aria-label="Quizzes and courses navigation" class="local-topbar-nav">
      <v-btn
        v-for="item in items"
        :key="item.section"
        :to="item.to"
        :active="activeSection === item.section"
        variant="text"
        :aria-current="activeSection === item.section ? 'page' : undefined"
      >
        {{ item.label }}
      </v-btn>
    </nav>
  </header>
</template>
