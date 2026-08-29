<script setup lang="ts">
import { useRoute } from 'vue-router'
import ServiceMenu from './components/ServiceMenu.vue'

const route = useRoute()
</script>

<template>
  <v-app>
    <v-app-bar v-if="route.name !== 'course' && route.name !== 'lesson'" flat class="app-bar">
      <v-container class="d-flex align-center">
        <ServiceMenu />
        <v-spacer />
        <nav aria-label="Main navigation" class="d-flex ga-2">
          <v-btn to="/" variant="text">Courses</v-btn>
          <v-btn to="/quizzes" variant="text">Quizzes</v-btn>
          <v-btn to="/flashcards" variant="text">Flashcards</v-btn>
        </nav>
      </v-container>
    </v-app-bar>
    <v-main>
      <router-view />
    </v-main>
  </v-app>
</template>
