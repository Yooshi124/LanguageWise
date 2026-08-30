<script setup lang="ts">
import type { VocabularyItem } from '../models/api'
import AppIcon from './AppIcon.vue'

defineProps<{
  modelValue: boolean
  items: readonly VocabularyItem[]
}>()

defineEmits<{
  'update:modelValue': [value: boolean]
}>()
</script>

<template>
  <v-bottom-sheet
    :model-value="modelValue"
    inset
    content-class="vocabulary-sheet-content"
    @update:model-value="$emit('update:modelValue', $event)"
  >
    <v-card rounded="t-xl" elevation="0" class="vocabulary-sheet">
      <v-card-title class="d-flex align-center px-8 pt-7">
        Lesson vocabulary
        <v-spacer />
        <v-btn
          icon
          variant="text"
          aria-label="Close vocabulary"
          @click="$emit('update:modelValue', false)"
        >
          <AppIcon name="close" />
        </v-btn>
      </v-card-title>
      <v-card-text class="vocabulary-sheet-body px-8 pb-8">
        <v-row>
          <v-col
            v-for="item in items"
            :key="`${item.word}-${item.meaning}`"
            cols="12"
            sm="6"
            lg="4"
          >
            <div class="vocabulary-item">
              <strong>{{ item.word }}</strong>
              <span>{{ item.meaning }}</span>
            </div>
          </v-col>
        </v-row>
      </v-card-text>
    </v-card>
  </v-bottom-sheet>
</template>
