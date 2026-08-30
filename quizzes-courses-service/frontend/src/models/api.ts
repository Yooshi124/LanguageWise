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

export interface Quiz {
  id: number
  courseId: number
  title: string
  isAi: boolean
}

export interface Flashcard {
  id: number
  courseId: number
  frontText: string
  backText: string
  isAi: boolean
}
