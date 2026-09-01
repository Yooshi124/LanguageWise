<script setup lang="ts">
import AppIcon from '../components/AppIcon.vue'
import { moduleSummaries, serviceMappings } from '../config/services'
</script>

<template>
  <section class="shared-home">
    <v-container class="shared-home-container">
      <header class="shared-hero">
        <v-chip color="primary" variant="tonal" class="mb-5">LanguageWise platform</v-chip>
        <h1>One place to <span>learn your way.</span></h1>
        <p>
          Move between courses, practice, community, achievements, and progress insights
          from the shared LanguageWise home.
        </p>
      </header>

      <v-row class="mt-8" align="stretch">
        <v-col
          v-for="module in moduleSummaries"
          :key="module.name"
          cols="12"
          md="6"
          xl="4"
        >
          <v-card
            :href="module.href"
            rounded="xl"
            variant="tonal"
            :color="module.color"
            class="module-card pa-4"
          >
            <div class="module-icon">
              <AppIcon :name="module.icon" :size="38" />
            </div>
            <div>
              <v-card-title>{{ module.name }}</v-card-title>
              <v-card-text>{{ module.description }}</v-card-text>
            </div>
          </v-card>
        </v-col>
      </v-row>

      <v-card rounded="xl" class="service-map-card mt-8" elevation="2">
        <v-card-item>
          <template #prepend>
            <v-avatar color="primary" variant="tonal" rounded="lg">
              <AppIcon name="analytics" />
            </v-avatar>
          </template>
          <v-card-title>Microservice runtime map</v-card-title>
          <v-card-subtitle>
            Public development ports, gateway paths, Docker endpoints, and data stores.
          </v-card-subtitle>
        </v-card-item>

        <div class="service-table-wrap">
          <v-table class="service-table">
            <thead>
              <tr>
                <th>Service</th>
                <th>Gateway</th>
                <th>Frontend</th>
                <th>Backend</th>
                <th>Database/API</th>
                <th>Storage</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="mapping in serviceMappings" :key="mapping.service">
                <th scope="row">{{ mapping.service }}</th>
                <td><code>{{ mapping.gateway }}</code></td>
                <td>{{ mapping.frontend }}</td>
                <td>{{ mapping.backend }}</td>
                <td>{{ mapping.database }}</td>
                <td>{{ mapping.technology }}</td>
              </tr>
            </tbody>
          </v-table>
        </div>
      </v-card>
    </v-container>
  </section>
</template>
