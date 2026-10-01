import { beforeEach, describe, expect, it, vi } from 'vitest';
import { mount, RouterLinkStub } from '@vue/test-utils';

// GuessTheWord talks to the network through api.js; replace it with a controllable stub
// before importing the component.
vi.mock('../src/api.js', () => ({
  initializeGame: vi.fn(),
  resetGame: vi.fn(),
  submitGuessTheWordGuess: vi.fn(),
  isNoVocabularyError: (error) => error?.code === 'NO_VOCABULARY',
  isAiUnavailableError: (error) => error?.code === 'AI_UNAVAILABLE',
  NO_VOCABULARY_MESSAGE: 'no vocabulary',
  AI_UNAVAILABLE_MESSAGE: 'ai unavailable'
}));

import { initializeGame, submitGuessTheWordGuess } from '../src/api.js';
import GuessTheWord from '../src/GuessTheWord.vue';

const stubs = {
  RouterLink: RouterLinkStub,
  AppIcon: true,
  GameHelp: true,
  WordDefinitions: true,
  GameSummary: true,
  GeneratingState: true
};

function flushPromises() {
  return new Promise((resolve) => setTimeout(resolve));
}

describe('GuessTheWord', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('starts a round on mount and renders the empty board', async () => {
    initializeGame.mockResolvedValue({ guesses: [], isComplete: false, isWon: false, specialLetters: [] });

    const wrapper = mount(GuessTheWord, { global: { stubs } });
    await flushPromises();

    expect(initializeGame).toHaveBeenCalledWith('guess-the-word');
    expect(wrapper.findAll('.grid-box')).toHaveLength(30);
  });

  it('shows the no-vocabulary message when the backend has nothing to play with', async () => {
    initializeGame.mockRejectedValue({ code: 'NO_VOCABULARY', message: 'nope' });

    const wrapper = mount(GuessTheWord, { global: { stubs } });
    await flushPromises();

    expect(wrapper.find('.empty-state__message').text()).toBe('no vocabulary');
  });

  it('submits a guess and renders the result', async () => {
    initializeGame.mockResolvedValue({ guesses: [], isComplete: false, isWon: false, specialLetters: [] });
    submitGuessTheWordGuess.mockResolvedValue({
      guess: 'apple',
      colours: ['G', 'G', 'G', 'G', 'G'],
      isCorrect: true,
      correctAnswer: 'APPLE'
    });

    const wrapper = mount(GuessTheWord, { global: { stubs } });
    await flushPromises();

    await wrapper.find('#guess').setValue('apple');
    await wrapper.find('form.guess-form').trigger('submit.prevent');
    await flushPromises();

    expect(submitGuessTheWordGuess).toHaveBeenCalledWith('apple');
    expect(wrapper.find('.game-message').text()).toBe('Correct. You found the word!');
  });
});
