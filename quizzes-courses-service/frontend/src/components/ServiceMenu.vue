<script setup lang="ts">
const host = window.location.hostname || 'localhost'

interface ServiceLink {
  name: string
  owner: string
  href: string
  current?: boolean
}

const services: readonly ServiceLink[] = [
  { name: 'Home', owner: 'Team', href: `http://${host}:3000/` },
  { name: 'Mini Games', owner: 'Kyan', href: `http://${host}:3001/` },
  { name: 'Discussion Forum', owner: 'Lachlan', href: `http://${host}:3002/` },
  {
    name: 'Quizzes & Courses',
    owner: 'Justin',
    href: `http://${host}:3000/quizzes-and-courses/`,
    current: true,
  },
  { name: 'Quests & Achievements', owner: 'Amber', href: `http://${host}:3004/` },
  { name: 'Leaderboard & Analytics', owner: 'Roan', href: `http://${host}:3005/` },
]
</script>

<template>
  <v-menu location="bottom start" :offset="8">
    <template #activator="{ props }">
      <button v-bind="props" type="button" class="brand" aria-label="Open LanguageWise services">
        <span class="brand-mark">L</span>
        <span>LanguageWise</span>
        <span class="brand-chevron" aria-hidden="true">▾</span>
      </button>
    </template>

    <v-list class="service-menu" rounded="lg" elevation="8" aria-label="LanguageWise services">
      <v-list-subheader>LanguageWise services</v-list-subheader>
      <v-list-item
        v-for="service in services"
        :key="service.href"
        :href="service.href"
        :active="service.current"
        color="primary"
      >
        <v-list-item-title>{{ service.name }}</v-list-item-title>
        <v-list-item-subtitle>{{ service.owner }}</v-list-item-subtitle>
      </v-list-item>
    </v-list>
  </v-menu>
</template>
