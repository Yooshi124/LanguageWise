import { createRouter, createWebHistory } from 'vue-router'
import HomeView from './views/HomeView.vue'
import CourseView from './views/CourseView.vue'
import WorkInProgressView from './views/WorkInProgressView.vue'
import { ensureAuthenticated } from './composables/useAuth'

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: [
    { path: '/', name: 'home', component: HomeView },
    { path: '/courses/:courseCode', name: 'course', component: CourseView },
    {
      path: '/courses/:courseCode/lessons/:lessonSlug',
      name: 'lesson',
      component: CourseView,
    },
    {
      path: '/quizzes',
      name: 'quizzes',
      component: WorkInProgressView,
      props: { kind: 'quizzes' },
    },
    {
      path: '/flashcards',
      name: 'flashcards',
      component: WorkInProgressView,
      props: { kind: 'flashcards' },
    },
  ],
  scrollBehavior: () => ({ top: 0 }),
})

router.beforeEach(async () => {
  try {
    await ensureAuthenticated()
  } catch (error) {
    console.error('Unable to verify the current login.', error)
  }

  return true
})

export default router
