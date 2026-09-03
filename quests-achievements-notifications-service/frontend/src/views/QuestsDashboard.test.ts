import { flushPromises, mount } from '@vue/test-utils'
import { afterEach, describe, expect, it, vi } from 'vitest'
import QuestsDashboard from './QuestsDashboard.vue'

const profile = {
  username: 'amber',
  preferences: { email: 'amber@example.com', notifyAll: false, notifyPostEngagement: true, notifyCourseCompletion: false, notifyQuizResults: true, notifyStreaks: false, notifyAchievements: true },
  achievements: [{ achievementId: 1, name: 'First Steps', image: '/images/missing.png', progress: 1, progressNeeded: 1 }],
  notifications: [{ notificationId: 1, trigger: 'quiz-result', time: '2026-01-01T00:00:00Z', emailSubject: 'Quiz result', emailBody: 'Well done.' }],
}

afterEach(() => vi.unstubAllGlobals())

describe('QuestsDashboard', () => {
  it('renders profile data and disables notification types without clearing values', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(new Response(JSON.stringify(profile), { status: 200 })))
    const wrapper = mount(QuestsDashboard)
    await flushPromises()

    expect(wrapper.text()).toContain('amber')
    expect(wrapper.text()).toContain('1 of 1 achievements complete')
    expect(wrapper.get('fieldset').attributes('disabled')).toBeDefined()
    expect((wrapper.findAll('input[type="checkbox"]')[1].element as HTMLInputElement).checked).toBe(true)
    expect(wrapper.get('img').attributes('src')).toBe('/remotes/quests-achievements/achievement.svg')
  })

  it('renders an unbounded login streak as a personal best', async () => {
    const streakProfile = {
      ...profile,
      achievements: [
        ...profile.achievements,
        { achievementId: 11, name: 'Longest Login Streak', image: '', progress: 12, progressNeeded: -1 },
      ],
    }
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(new Response(JSON.stringify(streakProfile), { status: 200 })))
    const wrapper = mount(QuestsDashboard)
    await flushPromises()

    expect(wrapper.text()).toContain('Longest Login Streak')
    expect(wrapper.text()).toContain('Personal best')
    expect(wrapper.text()).toContain('12 days')
    expect(wrapper.text()).toContain('1 of 1 achievements complete')
  })

  it('saves every preference as JSON and displays success feedback', async () => {
    const fetchMock = vi.fn()
      .mockResolvedValueOnce(new Response(JSON.stringify(profile), { status: 200 }))
      .mockResolvedValueOnce(new Response(JSON.stringify({ message: 'Notification preferences saved.' }), { status: 200 }))
    vi.stubGlobal('fetch', fetchMock)
    const wrapper = mount(QuestsDashboard)
    await flushPromises()
    await wrapper.get('form').trigger('submit')
    await flushPromises()

    const request = fetchMock.mock.calls[1][1]
    expect(request.headers['Content-Type']).toBe('application/json')
    expect(JSON.parse(request.body).notifyPostEngagement).toBe(true)
    expect(wrapper.text()).toContain('Notification preferences saved.')
  })

  it('opens notification details and closes them with Escape', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(new Response(JSON.stringify(profile), { status: 200 })))
    const showModal = vi.fn()
    const close = vi.fn()
    HTMLDialogElement.prototype.showModal = showModal
    HTMLDialogElement.prototype.close = close
    const wrapper = mount(QuestsDashboard)
    await flushPromises()

    await wrapper.get('.lw-notification__open').trigger('click')
    expect(showModal).toHaveBeenCalledOnce()
    expect(wrapper.text()).toContain('Well done.')
    await wrapper.get('dialog').trigger('keydown', { key: 'Escape' })
    expect(close).toHaveBeenCalledOnce()
  })
})