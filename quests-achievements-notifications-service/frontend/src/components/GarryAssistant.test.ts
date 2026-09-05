import { mount } from '@vue/test-utils'
import { describe, expect, it } from 'vitest'
import GarryAssistant from './GarryAssistant.vue'

describe('GarryAssistant', () => {
  it('shows the achievements and notifications example prompts', async () => {
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

    expect(wrapper.text()).toContain('What achievements should I aim for next?')
    expect(wrapper.text()).toContain('Explain my most recent notifications.')
    expect(wrapper.text()).toContain('Why am I not getting emails for post engagement?')
  })
})