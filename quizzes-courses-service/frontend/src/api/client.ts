import type { Course, Flashcard, Lesson, LessonSummary, Quiz } from '../models/api'

const apiBase = `${import.meta.env.BASE_URL}api`

async function get<T>(path: string, signal?: AbortSignal): Promise<T> {
  const response = await fetch(path, { signal, headers: { Accept: 'application/json' } })
  if (!response.ok) {
    throw new Error(`Request failed (${response.status} ${response.statusText})`)
  }
  return response.json() as Promise<T>
}

export const coursesApi = {
  list: (signal?: AbortSignal) => get<Course[]>(`${apiBase}/courses`, signal),
  get: (code: string, signal?: AbortSignal) =>
    get<Course>(`${apiBase}/courses/${encodeURIComponent(code)}`, signal),
  lessons: (code: string, signal?: AbortSignal) =>
    get<LessonSummary[]>(`${apiBase}/courses/${encodeURIComponent(code)}/lessons`, signal),
  lesson: (code: string, slug: string, signal?: AbortSignal) =>
    get<Lesson>(
      `${apiBase}/courses/${encodeURIComponent(code)}/lessons/${encodeURIComponent(slug)}`,
      signal,
    ),
  quizzes: (code: string, signal?: AbortSignal) =>
    get<Quiz[]>(`${apiBase}/courses/${encodeURIComponent(code)}/quizzes`, signal),
  flashcards: (code: string, signal?: AbortSignal) =>
    get<Flashcard[]>(`${apiBase}/courses/${encodeURIComponent(code)}/flashcards`, signal),
}
