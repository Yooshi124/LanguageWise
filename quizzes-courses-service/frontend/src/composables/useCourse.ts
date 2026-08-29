import { ref } from 'vue'
import { coursesApi } from '../api/client'
import type { Course, Lesson, LessonSummary } from '../models/api'

export function useCourse() {
  const course = ref<Course | null>(null)
  const lessons = ref<LessonSummary[]>([])
  const lesson = ref<Lesson | null>(null)
  const loadingCourse = ref(false)
  const loadingLesson = ref(false)
  const error = ref<string | null>(null)
  let controller: AbortController | null = null

  async function loadCourse(code: string) {
    controller?.abort()
    controller = new AbortController()
    loadingCourse.value = true
    error.value = null
    lesson.value = null
    try {
      const [courseResult, lessonResult] = await Promise.all([
        coursesApi.get(code, controller.signal),
        coursesApi.lessons(code, controller.signal),
      ])
      course.value = courseResult
      lessons.value = [...lessonResult].sort((a, b) => a.sortOrder - b.sortOrder)
    } catch (cause) {
      if (!(cause instanceof DOMException && cause.name === 'AbortError')) {
        error.value = cause instanceof Error ? cause.message : 'Unable to load this course.'
      }
    } finally {
      loadingCourse.value = false
    }
  }

  async function loadLesson(code: string, slug: string) {
    loadingLesson.value = true
    error.value = null
    try {
      lesson.value = await coursesApi.lesson(code, slug, controller?.signal)
    } catch (cause) {
      if (!(cause instanceof DOMException && cause.name === 'AbortError')) {
        error.value = cause instanceof Error ? cause.message : 'Unable to load this lesson.'
      }
    } finally {
      loadingLesson.value = false
    }
  }

  return { course, lessons, lesson, loadingCourse, loadingLesson, error, loadCourse, loadLesson }
}
