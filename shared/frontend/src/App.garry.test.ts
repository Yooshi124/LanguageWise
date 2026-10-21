import { flushPromises, shallowMount } from '@vue/test-utils'
import { ref } from 'vue'
import { createMemoryHistory, createRouter } from 'vue-router'
import { afterEach, expect, it, vi } from 'vitest'
import App from './App.vue'
import GarryAssistant from './components/GarryAssistant.vue'
import { featureAssistants, type FeatureAssistant } from './federation/featureAssistants'

const auth = {
  user: ref<{ id: number; name: string } | null>({ id: 1, name: 'First' }),
  status: ref('authenticated'),
  ensureAuthenticated: vi.fn().mockResolvedValue(true),
}
vi.mock('./composables/useAuth', () => ({ useAuth: () => auth }))

const adminSettings = { aiMode: ref(true), load: () => Promise.resolve() }
vi.mock('./composables/useAdminSettings', () => ({ useAdminSettings: () => adminSettings }))

afterEach(() => { featureAssistants.clear() })

it('keeps the workspace above route content and resets it only when the signed-in user changes', async () => {
  const assistant: FeatureAssistant = {
    apiBase: '/courses/api',
    welcome: '',
    placeholder: '',
    context: () => ({}),
    suggestions: () => [],
  }
  featureAssistants.set('courses', assistant)
  featureAssistants.set('games', { ...assistant, apiBase: '/games/api' })
  const router = createRouter({
    history: createMemoryHistory(),
    routes: [
      { path: '/', component: { template: '<div />' } },
      { path: '/courses', component: { template: '<div />' }, meta: { federatedFeature: 'courses' } },
      { path: '/games', component: { template: '<div />' }, meta: { federatedFeature: 'games' } },
    ],
  })
  await router.push('/courses')
  const wrapper = shallowMount(App, {
    global: { plugins: [router], stubs: { VApp: true, VMain: true, RouterView: true } },
  })
  await flushPromises()
  const original = wrapper.getComponent(GarryAssistant).vm.$.uid
  await router.push('/games')
  expect(wrapper.getComponent(GarryAssistant).vm.$.uid).toBe(original)
  expect(wrapper.getComponent(GarryAssistant).props('featureKey')).toBe('games')
  await router.push('/')
  expect(wrapper.getComponent(GarryAssistant).vm.$.uid).toBe(original)
  expect(wrapper.getComponent(GarryAssistant).props('assistant')).toBeUndefined()
  auth.user.value = { id: 2, name: 'Second' }
  await flushPromises()
  expect(wrapper.getComponent(GarryAssistant).vm.$.uid).not.toBe(original)
  auth.user.value = null
  await flushPromises()
  expect(wrapper.findComponent(GarryAssistant).exists()).toBe(false)
  wrapper.unmount()
})
