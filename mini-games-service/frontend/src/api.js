/**
 * API client for mini-games service.
 * Provides helper methods for authenticated Mini Games API calls.
 */

import { handleUnauthorized } from './federation/featureHost.js';

const COURSE_CODE_STORAGE_KEY = 'mini_games_course_code';
const MODE_STORAGE_KEY = 'mini_games_mode';
const AI_LANGUAGE_STORAGE_KEY = 'mini_games_ai_language';

const API_BASE = '/mini-games/api';

/** Abort a request if the server hasn't responded within this long. */
const REQUEST_TIMEOUT_MS = 15000;
/** GET requests are read-only, so a single retry after a network blip is safe. */
const GET_RETRY_ATTEMPTS = 2;
const GET_RETRY_DELAY_MS = 400;

/** Error code the backend returns when the user has no playable vocabulary yet. */
export const NO_VOCABULARY_CODE = 'NO_VOCABULARY';

/** Error code the backend returns when AI word generation is unavailable. */
export const AI_UNAVAILABLE_CODE = 'AI_UNAVAILABLE';

/** Friendly message shown when a game cannot start because the user has no vocabulary. */
export const NO_VOCABULARY_MESSAGE =
  'There were no words available to start the game with. Completing more course content will unlock new vocabulary';

/** Friendly message shown when AI word generation cannot fulfil a request. */
export const AI_UNAVAILABLE_MESSAGE =
  'AI word generation is unavailable right now. Try again in a moment, or switch to Content Focus.';

/** True when an API error means the user has not unlocked any vocabulary yet. */
export function isNoVocabularyError(error) {
  return error?.code === NO_VOCABULARY_CODE;
}

/** True when an API error means the AI provider could not generate words. */
export function isAiUnavailableError(error) {
  return error?.code === AI_UNAVAILABLE_CODE;
}

/**
 * Get the course code used to restrict a game's vocabulary.
 * Null when unset: call ensureCourseCode() to resolve one from the user's started courses.
 */
export function getCourseCode() {
  return localStorage.getItem(COURSE_CODE_STORAGE_KEY);
}

/**
 * Set the course code.
 */
export function setCourseCode(courseCode) {
  localStorage.setItem(COURSE_CODE_STORAGE_KEY, courseCode);
}

/** The selected vocabulary mode: 'content' (course words) or 'ai' (generated). */
export function getMode() {
  return localStorage.getItem(MODE_STORAGE_KEY) || 'content';
}

/** Persist the vocabulary mode. */
export function setMode(mode) {
  localStorage.setItem(MODE_STORAGE_KEY, mode);
}

/** The language code the AI mode generates words for. */
export function getAiLanguage() {
  return localStorage.getItem(AI_LANGUAGE_STORAGE_KEY);
}

/** Persist the AI mode language code. */
export function setAiLanguage(code) {
  localStorage.setItem(AI_LANGUAGE_STORAGE_KEY, code);
}

/**
 * Which vocabulary modes are usable right now, plus the languages each mode can offer.
 * contentAvailable=false means the courses service is unreachable or the user has no
 * unlocked vocabulary — the frontend locks the toggle onto AI generation in that case.
 * @returns {Promise<{contentAvailable: boolean, aiAvailable: boolean, defaultMode: string, contentLanguages: Array, aiLanguages: Array}>}
 */
export async function fetchGameModes() {
  return get(`${API_BASE}/game-modes`, 'Failed to load game modes');
}

/**
 * Languages the user has unlocked vocabulary in (started courses with completed lessons),
 * as seen by the backend for the logged-in user. Empty when signed out or nothing unlocked.
 * @returns {Promise<Array<{code: string, title: string}>>}
 */
export async function fetchGameLanguages() {
  return get(`${API_BASE}/game-languages`, 'Failed to load your languages');
}

/**
 * Successful completions per game type for the user, scoped to the given course
 * (the language selected on the game page).
 * @param {string} courseCode - Optional course code; omit for all languages.
 * @param {number} userId - Optional user ID (uses stored or default if not provided)
 * @returns {Promise<{courseCode: string|null, guessTheWord: number, wordSearch: number, associations: number}>}
 */
export async function fetchCompletionStats(courseCode, userId) {
  const query = courseCode
    ? `?courseCode=${encodeURIComponent(courseCode)}`
    : '';

  return get(`${API_BASE}/stats/completions${query}`, 'Failed to load your completion stats');
}

/**
 * Resolve the single course the games should use: the stored selection when it is still
 * one of the user's unlocked languages, otherwise the first unlocked language (persisted
 * so every game uses the same one). Null when the user has no unlocked vocabulary.
 */
export async function ensureCourseCode() {
  try {
    const languages = await fetchGameLanguages();
    const stored = getCourseCode();
    if (stored && languages.some((language) => language.code === stored)) {
      return stored;
    }
    if (languages.length > 0) {
      setCourseCode(languages[0].code);
      return languages[0].code;
    }
  } catch {
    // Signed out or the service is unreachable — fall back to the stored value, if any.
  }
  return getCourseCode();
}

/**
 * Build an Error from a failed API response, preserving the backend's error code (if any).
 */
async function toApiError(response, fallbackMessage) {
  if (response.status === 401) {
    handleUnauthorized();
  }
  const body = await response.json().catch(() => null);
  const detail =
    body?.error ??
    (body?.errors ? Object.values(body.errors)[0]?.[0] : null) ??
    fallbackMessage;
  const error = new Error(detail);
  if (body?.code) {
    error.code = body.code;
  }
  return error;
}

/** True for errors that mean the request never reached (or heard back from) the server. */
function isNetworkError(error) {
  return error instanceof TypeError || error?.name === 'AbortError';
}

/** Replace raw fetch/AbortController errors with a message a player can act on. */
function toNetworkAwareError(error) {
  if (error?.name === 'AbortError') {
    return new Error('The request timed out. Check your connection and try again.');
  }
  if (error instanceof TypeError) {
    return new Error('Could not reach the server. Check your connection and try again.');
  }
  return error;
}

/** fetch() with a hard timeout, so a hung request surfaces as an error instead of an endless spinner. */
async function fetchWithTimeout(path, init) {
  const controller = new AbortController();
  const timer = window.setTimeout(() => controller.abort(), REQUEST_TIMEOUT_MS);
  try {
    return await fetch(path, { ...init, signal: controller.signal });
  } finally {
    window.clearTimeout(timer);
  }
}

/**
 * GET is idempotent, so a transient network failure (timeout, DNS blip, dropped connection)
 * is retried once after a short delay before giving up.
 */
async function get(path, fallbackMessage) {
  for (let attempt = 1; attempt <= GET_RETRY_ATTEMPTS; attempt += 1) {
    try {
      const response = await fetchWithTimeout(path, { method: 'GET', headers: { Accept: 'application/json' } });
      if (!response.ok) {
        throw await toApiError(response, fallbackMessage);
      }
      return response.status === 204 ? null : response.json();
    } catch (error) {
      if (attempt < GET_RETRY_ATTEMPTS && isNetworkError(error)) {
        await new Promise((resolve) => window.setTimeout(resolve, GET_RETRY_DELAY_MS));
        continue;
      }
      throw toNetworkAwareError(error);
    }
  }
}

// POST requests trigger gameplay side effects (guesses, hints, resets), so they are never
// retried automatically — a duplicate submit could cost the player an extra guess or mistake.
async function post(path, body) {
  try {
    const response = await fetchWithTimeout(path, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: body === undefined ? undefined : JSON.stringify(body)
    });

    if (!response.ok) {
      throw await toApiError(response, `Request failed: ${response.status}`);
    }

    return response.status === 204 ? null : response.json();
  } catch (error) {
    throw toNetworkAwareError(error);
  }
}

/**
 * Initialize a game.
 * @param {string} gameType - 'guess-the-word', 'word-search', or 'associations'
 * @param {number} userId - Optional user ID (uses stored or default if not provided)
 * @param {string} courseCode - Optional course code; when omitted the stored selection is
 *   used, defaulting to the user's first unlocked language, so a game only ever draws words
 *   from a single course.
 */
export async function initializeGame(gameType, userId, courseCode) {
  const mode = getMode();
  const params = new URLSearchParams({ mode });
  if (mode === 'ai') {
    // AI mode: language comes from the AI language picker (any supported language).
    const language = getAiLanguage();
    if (language) {
      params.set('language', language);
    }
  } else {
    // Content mode: scope to the selected course (or the first unlocked one).
    const code = courseCode ?? await ensureCourseCode();
    if (code) {
      params.set('courseCode', code);
    }
  }
  return post(`${API_BASE}/${gameType}/init?${params.toString()}`);
}

/**
 * Get the current game state.
 * @param {string} gameType - 'guess-the-word', 'word-search', or 'associations'
 * @param {number} userId - Optional user ID (uses stored or default if not provided)
 */
export async function getGameState(gameType, userId) {
  return get(`${API_BASE}/${gameType}`, `Failed to load ${gameType}`);
}

/**
 * Submit a guess for Guess the Word game.
 * @param {string} guess - The guess
 * @param {number} userId - Optional user ID
 */
export function submitGuessTheWordGuess(guess, userId) {
  return post(`${API_BASE}/guess-the-word/guess`, { guess });
}

/**
 * Submit a word for Word Search game.
 * @param {string} word - The word
 * @param {number[]} indices - The indices of the word on the board
 * @param {number} userId - Optional user ID
 */
export function submitWordSearchWord(word, indices, userId) {
  return post(`${API_BASE}/word-search/guess`, { word, indices: indices || [] });
}

/**
 * Use a hint in Word Search game.
 * @param {number} userId - Optional user ID
 */
export function useWordSearchHint(userId) {
  return post(`${API_BASE}/word-search/hint`);
}

/**
 * Give up on Word Search game.
 * @param {number} userId - Optional user ID
 */
export function giveUpWordSearch(userId) {
  return post(`${API_BASE}/word-search/give-up`);
}

/**
 * Submit a group for Associations game.
 * @param {string[]} words - The words in the group
 * @param {number} userId - Optional user ID
 */
export function submitAssociationsGuess(words, userId) {
  return post(`${API_BASE}/associations/guess`, { words });
}

/**
 * Reset a game.
 * @param {string} gameType - 'guess-the-word', 'word-search', or 'associations'
 * @param {number} userId - Optional user ID
 */
export function resetGame(gameType, userId) {
  return post(`${API_BASE}/${gameType}/reset`);
}

