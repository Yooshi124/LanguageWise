import { computed, ref } from 'vue'

export interface AdminSetting {
  name: string
  isEnabled: boolean
}

const settings = ref<Record<string, boolean>>({})

async function load() {
  const response = await fetch('/api/admin-settings', {
    credentials: 'same-origin',
    headers: { Accept: 'application/json' },
  })
  if (!response.ok) {
    throw new Error(`Unable to load settings (${response.status} ${response.statusText})`)
  }
  const stored: AdminSetting[] = await response.json()
  settings.value = Object.fromEntries(stored.map((setting) => [setting.name, setting.isEnabled]))
}

const aiMode = computed(() => settings.value.AIMode === true)
const mcp = computed(() => aiMode.value && settings.value.MCP === true)
const rag = computed(() => aiMode.value && settings.value.RAG === true)

export function useAdminSettings() {
  return { settings, aiMode, mcp, rag, load }
}
