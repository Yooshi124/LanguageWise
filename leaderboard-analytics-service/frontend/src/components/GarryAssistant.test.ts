import { mount } from '@vue/test-utils'
import { describe, expect, it } from 'vitest'
import GarryAssistant from './GarryAssistant.vue'

describe('GarryAssistant', () => {
  it('shows the leaderboard and analytics example prompts', async () => {
    const wrapper = mount(GarryAssistant, {
      props: { userId: 901 },
      global: {
        stubs: {
          VAlert: { template: '<div><slot /><slot name="append" /></div>' },
          VBtn: { template: '<button><slot /></button>' },
        },
      },
    })

    await wrapper.get('.garry-launcher').trigger('click')

    expect(wrapper.text()).toContain('How am I doing across my languages this month?')
    expect(wrapper.text()).toContain('Which course should I focus on next?')
    expect(wrapper.text()).toContain('Explain my current ranks.')
  })
})
