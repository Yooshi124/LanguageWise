import { beforeEach, describe, expect, it, vi } from 'vitest';
import { mount, RouterLinkStub } from '@vue/test-utils';

// GamePage talks to the network and localStorage through api.js; replace it with a
// controllable stub before importing the component.
vi.mock('../src/api.js', () => ({
  fetchGameModes: vi.fn(),
  fetchCompletionStats: vi.fn(),
  getMode: vi.fn(() => 'content'),
  setMode: vi.fn(),
  getCourseCode: vi.fn(() => null),
  setCourseCode: vi.fn(),
  getAiLanguage: vi.fn(() => null),
  setAiLanguage: vi.fn()
}));

import { fetchGameModes, fetchCompletionStats } from '../src/api.js';
import GamePage from '../src/GamePage.vue';

const stubs = { RouterLink: RouterLinkStub, AppIcon: true, DocsSearch: true };

describe('GamePage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    fetchGameModes.mockResolvedValue({
      contentAvailable: true,
      aiAvailable: true,
      defaultMode: 'content',
      contentLanguages: [{ code: 'de', title: 'German' }],
      aiLanguages: []
    });
    fetchCompletionStats.mockResolvedValue({
      courseCode: 'de',
      guessTheWord: 3,
      wordSearch: 1,
      associations: 0,
      currentStreak: 2
    });
  });

  it('lists all three games', () => {
    const wrapper = mount(GamePage, { global: { stubs } });

    const names = wrapper.findAll('.game-card__body h2').map((node) => node.text());
    expect(names).toEqual(['Guess the word', 'Word Search', 'Associations']);
  });

  it('selects the content language returned by the modes endpoint and shows completion stats', async () => {
    const wrapper = mount(GamePage, { global: { stubs } });

    // onMounted's async resolution happens across two microtask hops (modes, then stats).
    await flushPromises();
    await flushPromises();

    expect(fetchCompletionStats).toHaveBeenCalledWith('de');
    expect(wrapper.find('[aria-label="Language your games use"] .is-active').text()).toBe('German');
    expect(wrapper.text()).toContain('2 days streak');
  });

  it('falls back to AI generation when course content is unavailable', async () => {
    fetchGameModes.mockResolvedValue({
      contentAvailable: false,
      aiAvailable: true,
      defaultMode: 'ai',
      contentLanguages: [],
      aiLanguages: [{ code: 'de', title: 'German' }]
    });

    const wrapper = mount(GamePage, { global: { stubs } });
    await flushPromises();
    await flushPromises();

    expect(wrapper.find('.mode-picker').text()).toContain('Course content is unavailable');
  });
});

function flushPromises() {
  return new Promise((resolve) => setTimeout(resolve));
}
