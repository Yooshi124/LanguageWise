<script setup lang="ts">
import { computed } from 'vue'
import { useRoute } from 'vue-router'

const route = useRoute()
const activeSection = computed(() => {
  const routeName = String(route.name ?? '')
  if (routeName === 'quizzes' || routeName === 'quiz-list' || routeName === 'quiz-runner') {
    return 'quizzes'
  }
  if (
    routeName === 'flashcards' ||
    routeName === 'flashcard-decks' ||
    routeName === 'flashcard-revision'
  ) {
    return 'flashcards'
  }
  return 'courses'
})
</script>

<template>
  <header class="local-topbar">
    <nav aria-label="Quizzes and courses navigation" class="local-topbar-nav">
      <v-btn
        :to="{ name: 'quizzes-courses-home' }"
        :active="activeSection === 'courses'"
        variant="text"
      >
        Courses
      </v-btn>
      <v-btn
        :to="{ name: 'quizzes' }"
        :active="activeSection === 'quizzes'"
        variant="text"
      >
        Quizzes
      </v-btn>
      <v-btn
        :to="{ name: 'flashcards' }"
        :active="activeSection === 'flashcards'"
        variant="text"
      >
        Flashcards
      </v-btn>
    </nav>
  </header>
</template>
