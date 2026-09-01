/**
 * API client for mini-games service.
 * Handles userId management and provides helper methods for API calls.
 */

const DEFAULT_USER_ID = 1;
const USER_ID_STORAGE_KEY = 'mini_games_user_id';
const COURSE_CODE_STORAGE_KEY = 'mini_games_course_code';

// Through the shared-frontend gateway this app lives under /mini-games/, so its
// API is /mini-games/api/. Vite inlines the configured base into BASE_URL.
const API_BASE = `${import.meta.env.BASE_URL}api`;

/** Error code the backend returns when the user has no playable vocabulary yet. */
export const NO_VOCABULARY_CODE = 'NO_VOCABULARY';

/** Friendly message shown when a game cannot start because the user has no vocabulary. */
export const NO_VOCABULARY_MESSAGE =
  'There were no words available to start the game with. Completing more course content will unlock new vocabulary';

/** True when an API error means the user has not unlocked any vocabulary yet. */
export function isNoVocabularyError(error) {
  return error?.code === NO_VOCABULARY_CODE;
}

/**
 * Get the current user ID from storage or default.
 */
export function getUserId() {
  const stored = localStorage.getItem(USER_ID_STORAGE_KEY);
  if (stored) {
    return parseInt(stored, 10);
  }
  return DEFAULT_USER_ID;
}

/**
 * Set the user ID in storage.
 */
export function setUserId(userId) {
  localStorage.setItem(USER_ID_STORAGE_KEY, String(userId));
}

/**
 * Get the course code (default: 'de' for German).
 */
export function getCourseCode() {
  return localStorage.getItem(COURSE_CODE_STORAGE_KEY) || 'de';
}

/**
 * Set the course code.
 */
export function setCourseCode(courseCode) {
  localStorage.setItem(COURSE_CODE_STORAGE_KEY, courseCode);
}

/**
 * Build an Error from a failed API response, preserving the backend's error code (if any).
 */
async function toApiError(response, fallbackMessage) {
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

async function post(path, body) {
  const response = await fetch(path, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: body === undefined ? undefined : JSON.stringify(body)
  });

  if (!response.ok) {
    throw await toApiError(response, `Request failed: ${response.status}`);
  }

  return response.status === 204 ? null : response.json();
}

/**
 * Initialize a game.
 * @param {string} gameType - 'guess-the-word', 'word-search', or 'associations'
 * @param {number} userId - Optional user ID (uses stored or default if not provided)
 * @param {string} courseCode - Optional course code (uses stored or default if not provided)
 */
export async function initializeGame(gameType, userId, courseCode) {
  const id = userId ?? getUserId();
  const code = courseCode ?? getCourseCode();
  return post(`${API_BASE}/${gameType}/init?userId=${id}&courseCode=${encodeURIComponent(code)}`);
}

/**
 * Get the current game state.
 * @param {string} gameType - 'guess-the-word', 'word-search', or 'associations'
 * @param {number} userId - Optional user ID (uses stored or default if not provided)
 */
export async function getGameState(gameType, userId) {
  const id = userId ?? getUserId();

  const response = await fetch(`${API_BASE}/${gameType}?userId=${id}`, {
    method: 'GET',
    headers: { 'Content-Type': 'application/json' }
  });

  if (!response.ok) {
    throw await toApiError(response, `Failed to load ${gameType}`);
  }

  return response.json();
}

/**
 * Submit a guess for Guess the Word game.
 * @param {string} guess - The guess
 * @param {number} userId - Optional user ID
 */
export function submitGuessTheWordGuess(guess, userId) {
  const id = userId ?? getUserId();
  return post(`${API_BASE}/guess-the-word/guess?userId=${id}`, { guess });
}

/**
 * Submit a word for Word Search game.
 * @param {string} word - The word
 * @param {number[]} indices - The indices of the word on the board
 * @param {number} userId - Optional user ID
 */
export function submitWordSearchWord(word, indices, userId) {
  const id = userId ?? getUserId();
  return post(`${API_BASE}/word-search/guess?userId=${id}`, { word, indices: indices || [] });
}

/**
 * Use a hint in Word Search game.
 * @param {number} userId - Optional user ID
 */
export function useWordSearchHint(userId) {
  const id = userId ?? getUserId();
  return post(`${API_BASE}/word-search/hint?userId=${id}`);
}

/**
 * Give up on Word Search game.
 * @param {number} userId - Optional user ID
 */
export function giveUpWordSearch(userId) {
  const id = userId ?? getUserId();
  return post(`${API_BASE}/word-search/give-up?userId=${id}`);
}

/**
 * Submit a group for Associations game.
 * @param {string[]} words - The words in the group
 * @param {number} userId - Optional user ID
 */
export function submitAssociationsGuess(words, userId) {
  const id = userId ?? getUserId();
  return post(`${API_BASE}/associations/guess?userId=${id}`, { words });
}

/**
 * Reset a game.
 * @param {string} gameType - 'guess-the-word', 'word-search', or 'associations'
 * @param {number} userId - Optional user ID
 */
export function resetGame(gameType, userId) {
  const id = userId ?? getUserId();
  return post(`${API_BASE}/${gameType}/reset?userId=${id}`);
}
