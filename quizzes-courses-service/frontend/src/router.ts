import { createRouter, createWebHistory } from 'vue-router'
import HomeView from './views/HomeView.vue'
import CourseView from './views/CourseView.vue'
import CourseCompletionView from './views/CourseCompletionView.vue'
import FlashcardDecksView from './views/FlashcardDecksView.vue'
import FlashcardRevisionView from './views/FlashcardRevisionView.vue'
import LanguageSelectionView from './views/LanguageSelectionView.vue'
import QuizListView from './views/QuizListView.vue'
import QuizRunnerView from './views/QuizRunnerView.vue'
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
      component: LanguageSelectionView,
      props: { kind: 'quizzes' },
    },
    { path: '/quizzes/:courseCode', name: 'quiz-list', component: QuizListView },
    {
      path: '/quizzes/:courseCode/:quizId',
      name: 'quiz-runner',
      component: QuizRunnerView,
      meta: { hideAssistant: true },
    },
    {
      path: '/flashcards',
      name: 'flashcards',
      component: LanguageSelectionView,
      props: { kind: 'flashcards' },
    },
    {
      path: '/flashcards/:courseCode',
      name: 'flashcard-decks',
      component: FlashcardDecksView,
    },
    {
      path: '/flashcards/:courseCode/:lessonSlug',
      name: 'flashcard-revision',
      component: FlashcardRevisionView,
    },
    {
      path: '/courses/:courseCode/completion',
      name: 'course-completion',
      component: CourseCompletionView,
      meta: { hideTopBar: true },
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
