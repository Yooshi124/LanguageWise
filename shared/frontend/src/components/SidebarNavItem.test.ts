import { mount } from '@vue/test-utils'
import { describe, expect, it } from 'vitest'
import SidebarNavItem from './SidebarNavItem.vue'

describe('SidebarNavItem', () => {
  it('preserves an absolute href for native links', () => {
    const href = 'http://localhost:3000/login?returnUrl=%2F'
    const wrapper = mount(SidebarNavItem, {
      props: {
        label: 'Not logged in',
        icon: 'profile',
        showLabel: true,
        href,
        native: true,
      },
      global: {
        stubs: {
          AppIcon: true,
          RouterLink: true,
          VTooltip: {
            template: '<div><slot name="activator" :props="{}" /></div>',
          },
        },
      },
    })

    expect(wrapper.get('a').attributes('href')).toBe(href)
    expect(wrapper.findComponent({ name: 'RouterLink' }).exists()).toBe(false)
  })
})