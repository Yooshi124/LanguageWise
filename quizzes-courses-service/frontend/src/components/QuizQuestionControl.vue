<script setup lang="ts">
import { computed, ref, watch } from 'vue'
import type { QuizQuestion } from '../models/api'

const props = defineProps<{
  question: QuizQuestion
  modelValue: string
  disabled?: boolean
}>()
const emit = defineEmits<{ 'update:modelValue': [value: string] }>()

const normalizedType = computed(() => props.question.type.toLowerCase().replaceAll('-', '_'))
const tokens = computed(() => props.question.questionData.tokens ?? [])
const selectedIndexes = ref<number[]>([])

function hydrateSelection() {
  const remaining = props.modelValue.trim().split(/\s+/).filter(Boolean)
  const used = new Set<number>()
  selectedIndexes.value = remaining.flatMap((word) => {
    const index = tokens.value.findIndex((token, tokenIndex) => token === word && !used.has(tokenIndex))
    if (index < 0) return []
    used.add(index)
    return [index]
  })
}

watch(() => props.question.id, hydrateSelection, { immediate: true })

function selectToken(index: number) {
  if (props.disabled || selectedIndexes.value.includes(index)) return
  selectedIndexes.value.push(index)
  syncWordResponse()
}

function removeToken(position: number) {
  if (props.disabled) return
  selectedIndexes.value.splice(position, 1)
  syncWordResponse()
}

function undoToken() {
  if (props.disabled) return
  selectedIndexes.value.pop()
  syncWordResponse()
}

function resetTokens() {
  if (props.disabled) return
  selectedIndexes.value = []
  syncWordResponse()
}

function syncWordResponse() {
  emit(
    'update:modelValue',
    selectedIndexes.value.map((index) => tokens.value[index]).join(' '),
  )
}
</script>

<template>
  <div class="question-control">
    <v-radio-group
      v-if="normalizedType === 'multiple_choice' || normalizedType === 'mc'"
      :model-value="modelValue"
      :disabled="disabled"
      hide-details
      @update:model-value="emit('update:modelValue', String($event ?? ''))"
    >
      <v-radio
        v-for="option in question.questionData.options ?? []"
        :key="option"
        :label="option"
        :value="option"
        class="quiz-option"
      />
    </v-radio-group>

    <div v-else-if="normalizedType === 'word_ordering'">
      <p class="control-label">Build the sentence</p>
      <div class="word-sentence" aria-live="polite">
        <button
          v-for="(index, position) in selectedIndexes"
          :key="`${index}-${position}`"
          type="button"
          class="word-token selected"
          :disabled="disabled"
          :aria-label="`Remove ${tokens[index]}`"
          @click="removeToken(position)"
        >
          {{ tokens[index] }}
        </button>
        <span v-if="!selectedIndexes.length" class="text-medium-emphasis">
          Select words below
        </span>
      </div>
      <div class="word-bank" aria-label="Available words">
        <button
          v-for="(token, index) in tokens"
          :key="`${token}-${index}`"
          type="button"
          class="word-token"
          :disabled="disabled || selectedIndexes.includes(index)"
          @click="selectToken(index)"
        >
          {{ token }}
        </button>
      </div>
      <div class="d-flex ga-2 mt-4">
        <v-btn
          size="small"
          variant="text"
          :disabled="disabled || !selectedIndexes.length"
          @click="undoToken"
        >
          Undo
        </v-btn>
        <v-btn
          size="small"
          variant="text"
          :disabled="disabled || !selectedIndexes.length"
          @click="resetTokens"
        >
          Reset
        </v-btn>
      </div>
    </div>

    <v-textarea
      v-else
      :model-value="modelValue"
      :disabled="disabled"
      label="Your answer"
      variant="outlined"
      rows="3"
      auto-grow
      counter
      @update:model-value="emit('update:modelValue', String($event ?? ''))"
    />
  </div>
</template>
