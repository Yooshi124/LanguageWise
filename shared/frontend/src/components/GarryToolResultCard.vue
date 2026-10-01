<script setup lang="ts">
import { computed } from 'vue'
import type { AssistantToolResult, AssistantToolView } from '../federation/featureAssistants'

const maximumRows = 12
const props = defineProps<{
  result: AssistantToolResult
  label: string
  present?: (result: AssistantToolResult) => AssistantToolView
}>()

const raw = computed(() => JSON.stringify(props.result.result, null, 2))

const view = computed<AssistantToolView>(() => {
  if (props.result.isError) {
    const record = props.result.result as Record<string, unknown> | null
    const text = typeof record?.text === 'string' ? record.text : ''
    return {
      summary:
        text.replace(/^An error occurred invoking '[^']+': /, '') ||
        'The tool could not complete this request.',
      rows: [],
    }
  }
  return props.present?.(props.result) ?? { rows: [] }
})

const visibleRows = computed(() => view.value.rows.slice(0, maximumRows))
const hiddenRows = computed(() => Math.max(0, view.value.rows.length - maximumRows))
</script>

<template>
  <section
    class="garry-tool-card"
    :class="{ 'garry-tool-card-error': result.isError }"
    :aria-label="`${label} tool result`"
  >
    <header>
      <span class="garry-tool-card-badge">Tool</span>
      <strong>{{ label }}</strong>
    </header>
    <p v-if="view.summary" class="garry-tool-card-summary">{{ view.summary }}</p>
    <ul v-if="visibleRows.length" class="garry-tool-card-rows">
      <li v-for="(row, index) in visibleRows" :key="index">
        <span>{{ row.primary }}</span>
        <small v-if="row.secondary">{{ row.secondary }}</small>
      </li>
    </ul>
    <p v-if="hiddenRows" class="garry-tool-card-more">+{{ hiddenRows }} more</p>
    <p
      v-if="!result.isError && !view.summary && visibleRows.length === 0"
      class="garry-tool-card-summary"
    >
      No results.
    </p>
    <details class="garry-tool-card-raw">
      <summary>Raw result</summary>
      <pre>{{ raw }}</pre>
    </details>
  </section>
</template>
