import type {
  Course,
  CourseProgress,
  FlashcardDeck,
  FlashcardDeckSummary,
  Lesson,
  LessonSummary,
  QuizAttempt,
  QuizDetail,
  QuizSubmission,
  QuizSummary,
} from '../models/api'
import { markSignedOut } from '../composables/useAuth'

const apiBase = `${import.meta.env.BASE_URL}api`

export class ApiError extends Error {
  constructor(
    message: string,
    public readonly status: number,
    public readonly body?: unknown,
  ) {
    super(message)
    this.name = 'ApiError'
  }
}

interface RequestOptions {
  body?: unknown
  signal?: AbortSignal
}

async function request<T>(
  method: 'GET' | 'POST' | 'PUT' | 'DELETE',
  path: string,
  options: RequestOptions = {},
): Promise<T> {
  const response = await fetch(path, {
    method,
    signal: options.signal,
    credentials: 'same-origin',
    headers: {
      Accept: 'application/json',
      ...(options.body === undefined ? {} : { 'Content-Type': 'application/json' }),
    },
    body: options.body === undefined ? undefined : JSON.stringify(options.body),
  })
  if (response.status === 401) {
    markSignedOut()
  }
  if (!response.ok) {
    const responseText = await response.text().catch(() => '')
    let body: unknown = responseText || undefined
    if (responseText) {
      try {
        body = JSON.parse(responseText)
      } catch {
        // Keep non-JSON error responses as text.
      }
    }
    const message =
      typeof body === 'object' &&
      body !== null &&
      'message' in body &&
      typeof body.message === 'string'
        ? body.message
        : `Request failed (${response.status} ${response.statusText})`
    throw new ApiError(message, response.status, body)
  }
  if (response.status === 204) {
    return undefined as T
  }
  return response.json() as Promise<T>
}

export const apiClient = {
  get: <T>(path: string, signal?: AbortSignal) =>
    request<T>('GET', path, { signal }),
  post: <T>(path: string, body?: unknown, signal?: AbortSignal) =>
    request<T>('POST', path, { body, signal }),
  put: <T>(path: string, body?: unknown, signal?: AbortSignal) =>
    request<T>('PUT', path, { body, signal }),
  delete: <T>(path: string, signal?: AbortSignal) =>
    request<T>('DELETE', path, { signal }),
}

export const coursesApi = {
  list: (signal?: AbortSignal) => apiClient.get<Course[]>(`${apiBase}/courses`, signal),
  get: (code: string, signal?: AbortSignal) =>
    apiClient.get<Course>(`${apiBase}/courses/${encodeURIComponent(code)}`, signal),
  lessons: (code: string, signal?: AbortSignal) =>
    apiClient.get<LessonSummary[]>(
      `${apiBase}/courses/${encodeURIComponent(code)}/lessons`,
      signal,
    ),
  lesson: (code: string, slug: string, signal?: AbortSignal) =>
    apiClient.get<Lesson>(
      `${apiBase}/courses/${encodeURIComponent(code)}/lessons/${encodeURIComponent(slug)}`,
      signal,
    ),
  quizzes: (code: string, signal?: AbortSignal) =>
    apiClient.get<QuizSummary[]>(
      `${apiBase}/courses/${encodeURIComponent(code)}/quizzes`,
      signal,
    ),
  flashcardDecks: (code: string, signal?: AbortSignal) =>
    apiClient.get<FlashcardDeckSummary[]>(
      `${apiBase}/courses/${encodeURIComponent(code)}/flashcard-decks`,
      signal,
    ),
  flashcardDeck: (code: string, slug: string, signal?: AbortSignal) =>
    apiClient.get<FlashcardDeck>(
      `${apiBase}/courses/${encodeURIComponent(code)}/flashcard-decks/${encodeURIComponent(slug)}`,
      signal,
    ),
  progress: (code: string, signal?: AbortSignal) =>
    apiClient.get<CourseProgress>(
      `${apiBase}/courses/${encodeURIComponent(code)}/progress`,
      signal,
    ),
  complete: (code: string, signal?: AbortSignal) =>
    apiClient.put<void>(
      `${apiBase}/courses/${encodeURIComponent(code)}/milestone`,
      undefined,
      signal,
    ),
  uncomplete: (code: string, signal?: AbortSignal) =>
    apiClient.delete<void>(
      `${apiBase}/courses/${encodeURIComponent(code)}/milestone`,
      signal,
    ),
}

export const lessonsApi = {
  complete: (lessonId: number, signal?: AbortSignal) =>
    apiClient.put<void>(`${apiBase}/lessons/${lessonId}/milestone`, undefined, signal),
  uncomplete: (lessonId: number, signal?: AbortSignal) =>
    apiClient.delete<void>(`${apiBase}/lessons/${lessonId}/milestone`, signal),
}

export const quizzesApi = {
  get: (id: number, signal?: AbortSignal) =>
    apiClient.get<QuizDetail>(`${apiBase}/quizzes/${id}`, signal),
  startAttempt: (id: number, signal?: AbortSignal) =>
    apiClient.post<QuizAttempt>(`${apiBase}/quizzes/${id}/attempts`, {}, signal),
  submitAttempt: (
    id: number,
    answers: { questionId: number; response: string }[],
    signal?: AbortSignal,
  ) =>
    apiClient.post<QuizSubmission>(
      `${apiBase}/quiz-attempts/${id}/submit`,
      { answers },
      signal,
    ),
}
