import { afterEach, beforeEach, describe, expect, it } from 'vitest';
import {
  AI_UNAVAILABLE_CODE,
  NO_VOCABULARY_CODE,
  getAiLanguage,
  getCourseCode,
  getMode,
  isAiUnavailableError,
  isNoVocabularyError,
  setAiLanguage,
  setCourseCode,
  setMode
} from '../src/api.js';

describe('error classifiers', () => {
  it('recognises the no-vocabulary error code', () => {
    expect(isNoVocabularyError({ code: NO_VOCABULARY_CODE })).toBe(true);
    expect(isNoVocabularyError({ code: 'OTHER' })).toBe(false);
    expect(isNoVocabularyError(new Error('plain'))).toBe(false);
    expect(isNoVocabularyError(null)).toBe(false);
  });

  it('recognises the AI-unavailable error code', () => {
    expect(isAiUnavailableError({ code: AI_UNAVAILABLE_CODE })).toBe(true);
    expect(isAiUnavailableError({ code: NO_VOCABULARY_CODE })).toBe(false);
    expect(isAiUnavailableError(undefined)).toBe(false);
  });
});

describe('stored preferences', () => {
  beforeEach(() => localStorage.clear());
  afterEach(() => localStorage.clear());

  it('defaults the vocabulary mode to content', () => {
    expect(getMode()).toBe('content');
  });

  it('round-trips the vocabulary mode', () => {
    setMode('ai');
    expect(getMode()).toBe('ai');
  });

  it('round-trips the course code', () => {
    expect(getCourseCode()).toBeNull();
    setCourseCode('de');
    expect(getCourseCode()).toBe('de');
  });

  it('round-trips the AI language', () => {
    expect(getAiLanguage()).toBeNull();
    setAiLanguage('fr');
    expect(getAiLanguage()).toBe('fr');
  });
});
