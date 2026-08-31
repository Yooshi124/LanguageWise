export interface Course {
  id: number
  code: string
  title: string
  description: string
}

export interface LessonSummary {
  id: number
  slug: string
  title: string
  sortOrder: number
}

export interface VocabularyItem {
  word: string
  meaning: string
}

export interface Lesson {
  id: number
  course: Course
  slug: string
  title: string
  sortOrder: number
  contentMarkdown: string
  vocabulary: VocabularyItem[]
}

export interface QuizSummary {
  id: number
  title: string
  lessonId: number
  lessonSlug: string
  lessonTitle: string
  lessonSortOrder: number
}

export type QuizQuestionType = 'multiple_choice' | 'word_ordering' | 'free_text' | string

export interface QuizQuestion {
  id: number
  sortOrder: number
  content: string
  type: QuizQuestionType
  questionData: {
    options?: readonly string[]
    tokens?: readonly string[]
    [key: string]: unknown
  }
}

export interface QuizDetail extends QuizSummary {
  questions: QuizQuestion[]
}

export interface QuizAttempt {
  id: number
  quizId: number
  startedAt: string
}

export interface QuizSubmissionAnswer {
  questionId: number
  studentResponse: string
  isCorrect: boolean
  correctAnswer: string
}

export interface QuizSubmission {
  attemptId: number
  quizId: number
  score: number
  totalQuestions: number
  passed: boolean
  completedAt: string
  answers: QuizSubmissionAnswer[]
}

export interface FlashcardDeckSummary {
  lessonId: number
  lessonSlug: string
  lessonTitle: string
  lessonSortOrder: number
  cardCount: number
}

export interface Flashcard {
  id: number
  frontText: string
  backText: string
}

export interface FlashcardDeck extends Omit<FlashcardDeckSummary, 'cardCount'> {
  cards: Flashcard[]
}

export interface LessonProgress {
  lessonId: number
  completed: boolean
}

export interface QuizProgress {
  quizId: number
  lessonId: number
  completed: boolean
  bestScore: number | null
  totalQuestions: number
}

export interface CourseProgress {
  courseCompleted: boolean
  courseEligible: boolean
  lessons: LessonProgress[]
  quizzes: QuizProgress[]
}
