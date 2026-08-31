import { onScopeDispose, ref } from 'vue'
import { coursesApi } from '../api/client'
import type {
  Course,
  CourseProgress,
  FlashcardDeckSummary,
  Lesson,
  LessonSummary,
  QuizSummary,
} from '../models/api'
import { errorMessage, isAbortError } from './useAsyncResource'

export function useCourse() {
  const course = ref<Course | null>(null)
  const lessons = ref<LessonSummary[]>([])
  const lesson = ref<Lesson | null>(null)
  const quizzes = ref<QuizSummary[]>([])
  const flashcardDecks = ref<FlashcardDeckSummary[]>([])
  const progress = ref<CourseProgress | null>(null)
  const loadingCourse = ref(false)
  const loadingLesson = ref(false)
  const error = ref<string | null>(null)
  let courseController: AbortController | null = null
  let lessonController: AbortController | null = null

  async function loadCourse(code: string) {
    courseController?.abort()
    lessonController?.abort()
    courseController = new AbortController()
    const requestController = courseController
    loadingCourse.value = true
    error.value = null
    lesson.value = null
    try {
      const [courseResult, lessonResult, quizResult, deckResult, progressResult] = await Promise.all([
        coursesApi.get(code, requestController.signal),
        coursesApi.lessons(code, requestController.signal),
        coursesApi.quizzes(code, requestController.signal),
        coursesApi.flashcardDecks(code, requestController.signal),
        coursesApi.progress(code, requestController.signal),
      ])
      course.value = courseResult
      lessons.value = [...lessonResult].sort((a, b) => a.sortOrder - b.sortOrder)
      quizzes.value = [...quizResult].sort((a, b) => a.lessonSortOrder - b.lessonSortOrder)
      flashcardDecks.value = [...deckResult].sort(
        (a, b) => a.lessonSortOrder - b.lessonSortOrder,
      )
      progress.value = progressResult
    } catch (cause) {
      if (!isAbortError(cause)) {
        error.value = errorMessage(cause, 'Unable to load this course.')
      }
    } finally {
      if (courseController === requestController && !requestController.signal.aborted) {
        loadingCourse.value = false
      }
    }
  }

  async function loadLesson(code: string, slug: string) {
    lessonController?.abort()
    lessonController = new AbortController()
    const requestController = lessonController
    loadingLesson.value = true
    error.value = null
    try {
      lesson.value = await coursesApi.lesson(code, slug, requestController.signal)
    } catch (cause) {
      if (!isAbortError(cause)) {
        error.value = errorMessage(cause, 'Unable to load this lesson.')
      }
    } finally {
      if (lessonController === requestController && !requestController.signal.aborted) {
        loadingLesson.value = false
      }
    }
  }

  onScopeDispose(() => {
    courseController?.abort()
    lessonController?.abort()
  })

  return {
    course,
    lessons,
    lesson,
    quizzes,
    flashcardDecks,
    progress,
    loadingCourse,
    loadingLesson,
    error,
    loadCourse,
    loadLesson,
  }
}
