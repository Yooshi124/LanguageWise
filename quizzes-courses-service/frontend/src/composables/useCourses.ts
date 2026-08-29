import { readonly, ref } from 'vue'
import { coursesApi } from '../api/client'
import type { Course } from '../models/api'

const courses = ref<Course[]>([])
const loading = ref(false)
const error = ref<string | null>(null)

export function useCourses() {
  async function load(force = false) {
    if (courses.value.length && !force) return
    loading.value = true
    error.value = null
    try {
      courses.value = await coursesApi.list()
    } catch (cause) {
      error.value = cause instanceof Error ? cause.message : 'Unable to load courses.'
    } finally {
      loading.value = false
    }
  }

  return {
    courses: readonly(courses),
    loading: readonly(loading),
    error: readonly(error),
    load,
  }
}
