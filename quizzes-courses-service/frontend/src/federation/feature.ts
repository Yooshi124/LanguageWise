import CourseCompletionView from '../views/CourseCompletionView.vue'
import CourseView from '../views/CourseView.vue'
import FlashcardDecksView from '../views/FlashcardDecksView.vue'
import FlashcardRevisionView from '../views/FlashcardRevisionView.vue'
import HomeView from '../views/HomeView.vue'
import LanguageSelectionView from '../views/LanguageSelectionView.vue'
import QuizListView from '../views/QuizListView.vue'
import QuizRunnerView from '../views/QuizRunnerView.vue'
import QuizzesCoursesComponent from './QuizzesCoursesComponent.vue'
import type { FederatedFeatureModule } from './contracts'

export { QuizzesCoursesComponent }

export const metadata: FederatedFeatureModule['metadata'] = {
  key: 'quizzes-courses',
  displayName: 'Quizzes & Courses',
  icon: 'courses',
  basePath: '/quizzes-and-courses',
  requiresAuth: true,
}

export const routes = [
  { path: '', name: 'quizzes-courses-home', component: HomeView },
  { path: 'courses/:courseCode', name: 'course', component: CourseView },
  {
    path: 'courses/:courseCode/lessons/:lessonSlug',
    name: 'lesson',
    component: CourseView,
  },
  {
    path: 'quizzes',
    name: 'quizzes',
    component: LanguageSelectionView,
    props: { kind: 'quizzes' },
  },
  { path: 'quizzes/:courseCode', name: 'quiz-list', component: QuizListView },
  {
    path: 'quizzes/:courseCode/:quizId',
    name: 'quiz-runner',
    component: QuizRunnerView,
    meta: { hideAssistant: true },
  },
  {
    path: 'flashcards',
    name: 'flashcards',
    component: LanguageSelectionView,
    props: { kind: 'flashcards' },
  },
  {
    path: 'flashcards/:courseCode',
    name: 'flashcard-decks',
    component: FlashcardDecksView,
  },
  {
    path: 'flashcards/:courseCode/:lessonSlug',
    name: 'flashcard-revision',
    component: FlashcardRevisionView,
  },
  {
    path: 'courses/:courseCode/completion',
    name: 'course-completion',
    component: CourseCompletionView,
    meta: { hideAssistant: true, hideTopBar: true },
  },
] as const