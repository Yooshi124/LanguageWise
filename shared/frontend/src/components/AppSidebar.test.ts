import { mount } from '@vue/test-utils'
import { computed, ref } from 'vue'
import { createMemoryHistory, createRouter } from 'vue-router'
import { describe, expect, it, vi } from 'vitest'
import AppSidebar from './AppSidebar.vue'

const logout = vi.fn().mockResolvedValue(undefined)

vi.mock('../composables/useAuth', () => ({
  useAuth: () => ({
    status: ref('authenticated'),
    username: ref('amber'),
    isAuthenticated: computed(() => true),
    loginUrl: () => '/login',
    logout,
  }),
}))

describe('AppSidebar', () => {
  it('navigates home after logout so the active feature unmounts', async () => {
    const router = createRouter({
      history: createMemoryHistory(),
      routes: [
        { path: '/', component: { template: '<div />' } },
        { path: '/protected', component: { template: '<div />' } },
      ],
    })
    await router.push('/protected')

    const wrapper = mount(AppSidebar, {
      props: { expanded: true, mobileOpen: false },
      global: {
        plugins: [router],
        stubs: {
          AppIcon: true,
          SidebarNavItem: {
            props: ['label'],
            emits: ['click'],
            template: '<button @click="$emit(\'click\')">{{ label }}</button>',
          },
        },
      },
    })

    await wrapper.get('nav[aria-label="Account"] button:last-of-type').trigger('click')
    await vi.waitFor(() => expect(router.currentRoute.value.path).toBe('/'))

    expect(logout).toHaveBeenCalledOnce()
  })
})