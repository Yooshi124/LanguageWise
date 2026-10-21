<script setup lang="ts">
import { onBeforeUnmount, onMounted, ref, watch } from 'vue'
import { mdiChatOutline, mdiChatPlusOutline } from '@mdi/js'
import garryImage from '../assets/garry.png'
import { garryStorageKey } from '../composables/useGarryAssistant'
import type { FeatureAssistant } from '../federation/featureAssistants'
import GarryChatWindow from './GarryChatWindow.vue'
import GarryDocsSearch from './GarryDocsSearch.vue'

const props = defineProps<{
  featureKey?: string
  userId: number
  assistant?: FeatureAssistant
}>()

const storageKey = `languagewise:garry:v3:user:${props.userId}:windows`
const windows = ref(loadWindowIds().map((id, index) => ({
  id,
  number: index + 1,
  minimized: true,
  order: index + 1,
})))
const docsSearchVisible = ref(false)
const chatMenuOpen = ref(false)
const dock = ref<HTMLElement | null>(null)
const maximizedBounds = ref({
  left: 16,
  top: 16,
  width: Math.max(1, window.innerWidth - 32),
  height: Math.max(1, window.innerHeight - 142),
})
let layoutObserver: ResizeObserver | undefined
let nextNumber = windows.value.length + 1
let nextOrder = windows.value.length + 1

function updateBounds() {
  const content = document.querySelector('.app-shell-content')?.getBoundingClientRect()
  const navigation = document.querySelector('.mobile-nav-trigger')?.getBoundingClientRect()
  const left = Math.max(0, content?.left ?? 0) + 16
  const right = Math.min(window.innerWidth, content?.width ? content.right : window.innerWidth) - 16
  const top = navigation?.height ? Math.max(16, navigation.bottom + 16) : 16
  const bottom = (dock.value?.getBoundingClientRect().top || window.innerHeight - 110) - 16
  maximizedBounds.value = {
    left,
    top,
    width: Math.max(1, right - left),
    height: Math.max(1, bottom - top),
  }
}

onMounted(() => {
  updateBounds()
  window.addEventListener('resize', updateBounds)
  if (typeof ResizeObserver !== 'undefined') {
    layoutObserver = new ResizeObserver(updateBounds)
    const content = document.querySelector('.app-shell-content')
    if (content) layoutObserver.observe(content)
    if (dock.value) layoutObserver.observe(dock.value)
  }
})

onBeforeUnmount(() => {
  window.removeEventListener('resize', updateBounds)
  layoutObserver?.disconnect()
})

function loadWindowIds(): string[] {
  try {
    const stored: unknown = JSON.parse(sessionStorage.getItem(storageKey) ?? '["main"]')
    if (!Array.isArray(stored) || !stored.every((id) => typeof id === 'string' && /^[\w-]{1,64}$/.test(id))) {
      throw new Error('Invalid Garry window list.')
    }
    return [...new Set<string>(stored)]
  } catch (cause) {
    console.warn('Garry could not restore its windows.', cause)
    return ['main']
  }
}

watch(
  () => windows.value.map((window) => window.id),
  (ids) => {
    try {
      sessionStorage.setItem(storageKey, JSON.stringify(ids))
    } catch (cause) {
      console.warn('Garry could not save its window list for reload.', cause)
    }
  },
  { immediate: true },
)

function focus(id: string) {
  const window = windows.value.find((window) => window.id === id)
  if (window) window.order = nextOrder++
}

function restore(id: string) {
  const window = windows.value.find((window) => window.id === id)
  if (!window) return
  window.minimized = false
  focus(id)
  chatMenuOpen.value = false
}

function newChat() {
  const id = crypto.randomUUID()
  windows.value.push({ id, number: nextNumber++, minimized: false, order: nextOrder++ })
  chatMenuOpen.value = false
}

function close(id: string) {
  windows.value = windows.value.filter((window) => window.id !== id)
  const prefix = garryStorageKey(props.userId, id)
  for (const key of Object.keys(sessionStorage)) {
    if (key === prefix || key.startsWith(`${prefix}:`)) sessionStorage.removeItem(key)
  }
}
</script>

<template>
  <Teleport to="body">
    <aside class="garry-assistant" aria-label="Garry the LanguageWise assistant">
      <GarryChatWindow
        v-for="window in windows"
        :key="window.id"
        :session-id="window.id"
        :number="window.number"
        :minimized="window.minimized"
        :order="window.order"
        :maximized-bounds="maximizedBounds"
        :feature-key="featureKey"
        :user-id="userId"
        :assistant="assistant"
        @focus="focus(window.id)"
        @minimize="window.minimized = true"
        @close="close(window.id)"
        @new-chat="newChat"
        @docs="docsSearchVisible = true"
      />
      <div ref="dock" class="garry-dock">
        <v-speed-dial
          v-model="chatMenuOpen"
          location="top end"
          content-class="garry-chat-menu"
          :max-height="maximizedBounds.height"
          :close-on-content-click="false"
        >
          <template #activator="{ props: activatorProps }">
            <button
              v-bind="activatorProps"
              type="button"
              class="garry-launcher"
              aria-label="Open Garry chat menu"
            >
              <img :src="garryImage" alt="" />
              <span>Ask Garry</span>
            </button>
          </template>
          <v-btn
            v-for="window in windows"
            :key="window.id"
            class="garry-chat-menu-action"
            :prepend-icon="mdiChatOutline"
            rounded="pill"
            size="large"
            role="menuitem"
            type="button"
            :aria-label="`Open Garry chat ${window.number}`"
            @click="restore(window.id)"
          >
            <span class="garry-chat-menu-label">
              <strong>Chat {{ window.number }}</strong>
              <small>{{ window.minimized ? 'Minimized' : 'Open' }}</small>
            </span>
          </v-btn>
          <v-btn
            key="new-chat"
            class="garry-chat-menu-action"
            :prepend-icon="mdiChatPlusOutline"
            color="primary"
            rounded="pill"
            size="large"
            role="menuitem"
            type="button"
            aria-label="New Garry chat"
            @click="newChat"
          >New chat</v-btn>
        </v-speed-dial>
      </div>
      <GarryDocsSearch :visible="docsSearchVisible" @close="docsSearchVisible = false" />
    </aside>
  </Teleport>
</template>
