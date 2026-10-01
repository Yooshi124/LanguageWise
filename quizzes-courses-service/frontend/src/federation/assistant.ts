import type { RouteLocationNormalizedLoaded } from 'vue-router'
import type { AssistantToolView, FeatureAssistant, FeatureAssistantTool } from './contracts'

type Requirement = 'none' | 'course' | 'lesson'

function param(route: RouteLocationNormalizedLoaded, name: string) {
  const value = route.params[name]
  return typeof value === 'string' ? value : undefined
}

function courseCode(route: RouteLocationNormalizedLoaded) {
  return param(route, 'courseCode')?.toLowerCase()
}

function chip(
  tool: string,
  label: string,
  requires: Requirement,
  args: (route: RouteLocationNormalizedLoaded) => Record<string, unknown>,
): FeatureAssistantTool {
  return {
    tool,
    label,
    arguments: args,
    unavailable: (route) => {
      if (requires === 'course' && !courseCode(route)) return 'Open a course to use this tool.'
      if (requires === 'lesson' && !(courseCode(route) && param(route, 'lessonSlug'))) {
        return 'Open a lesson to use this tool.'
      }
      return null
    },
  }
}

const withCourse = (route: RouteLocationNormalizedLoaded) => ({ courseCode: courseCode(route) })
const withLesson = (route: RouteLocationNormalizedLoaded) => ({
  courseCode: courseCode(route),
  lessonSlug: param(route, 'lessonSlug'),
})

function asRecord(value: unknown): Record<string, unknown> {
  return typeof value === 'object' && value !== null && !Array.isArray(value)
    ? (value as Record<string, unknown>)
    : {}
}

function list(value: unknown) {
  return Array.isArray(value) ? value.map(asRecord) : []
}

function text(value: unknown) {
  return typeof value === 'string' ? value : ''
}

function number(value: unknown) {
  return typeof value === 'number' ? value : 0
}

function capitalise(value: string) {
  return value ? value[0]!.toUpperCase() + value.slice(1) : 'Milestone'
}

function milestoneName(milestone: Record<string, unknown>) {
  const kind = text(milestone.kind)
  const name =
    kind === 'quiz'
      ? text(milestone.quizTitle)
      : kind === 'lesson'
        ? text(milestone.lessonTitle)
        : text(milestone.courseTitle)
  const course = kind === 'course' ? '' : text(milestone.courseTitle)
  if (!name) return `${capitalise(kind)} completed`
  return course ? `${capitalise(kind)}: ${name} (${course})` : `${capitalise(kind)}: ${name}`
}

function formatDate(value: string) {
  const date = new Date(value)
  return Number.isNaN(date.getTime()) ? value : date.toLocaleString()
}

function toolView(tool: string, result: unknown): AssistantToolView {
  const value = asRecord(result)
  switch (tool) {
    case 'courses_list_courses':
      return {
        rows: list(value.courses).map((course) => ({
          primary: `${text(course.title)} (${text(course.code)})`,
          secondary: text(course.description),
        })),
      }
    case 'courses_get_my_progress': {
      const quizzes = list(value.quizzes)
      const passed = quizzes.filter((quiz) => quiz.completed === true).length
      return {
        summary:
          `${number(value.lessonsCompleted)} of ${number(value.lessonsTotal)} lessons completed · ` +
          `${passed} of ${quizzes.length} quizzes passed` +
          (value.courseCompleted === true ? ' · Course complete' : ''),
        rows: quizzes.map((quiz) => ({
          primary: `Quiz ${number(quiz.quizId)}`,
          secondary:
            quiz.bestScore === null || quiz.bestScore === undefined
              ? 'Not attempted'
              : `Best score ${number(quiz.bestScore)}/${number(quiz.totalQuestions)}`,
        })),
      }
    }
    case 'courses_list_lessons':
      return {
        rows: list(value.lessons).map((lesson) => ({
          primary: `${number(lesson.sortOrder)}. ${text(lesson.title)}`,
          secondary: text(lesson.slug),
        })),
      }
    case 'courses_list_quizzes':
      return {
        rows: list(value.quizzes).map((quiz) => ({
          primary: text(quiz.title),
          secondary: text(quiz.lessonTitle),
        })),
      }
    case 'courses_get_lesson_vocabulary':
      return {
        summary: text(value.lessonTitle),
        rows: list(value.vocabulary).map((item) => ({
          primary: text(item.word),
          secondary: text(item.meaning),
        })),
      }
    case 'courses_get_flashcards':
      return {
        summary: text(value.lessonTitle),
        rows: list(value.cards).map((card) => ({
          primary: text(card.front),
          secondary: text(card.back),
        })),
      }
    case 'courses_get_my_vocabulary':
      return {
        summary: `${number(value.totalWords)} words learnt`,
        rows: list(value.courses).flatMap((course) =>
          list(course.lessons).map((lesson) => ({
            primary: `${text(course.courseTitle)} · ${text(lesson.lessonTitle)}`,
            secondary: list(lesson.vocabulary)
              .map((item) => text(item.word))
              .join(', '),
          })),
        ),
      }
    case 'courses_get_my_milestones':
      return {
        rows: list(value.milestones).map((milestone) => ({
          primary: milestoneName(milestone),
          secondary: formatDate(text(milestone.completedAt)),
        })),
      }
    default:
      return { rows: [] }
  }
}

export const assistant: FeatureAssistant = {
  apiBase: '/quizzes-and-courses/api',
  welcome:
    'Ask me about your course, the lesson you’re studying, or how quizzes work on LanguageWise.',
  placeholder: 'Ask Garry about language learning…',
  context: (route) => ({
    routeName: route.name === 'quizzes-courses-home' ? 'home' : String(route.name ?? 'home'),
    ...(param(route, 'courseCode') ? { courseCode: param(route, 'courseCode') } : {}),
    ...(param(route, 'lessonSlug') ? { lessonSlug: param(route, 'lessonSlug') } : {}),
  }),
  suggestions: (route) => {
    if (route.name === 'lesson') {
      return [
        'Explain the main idea in this lesson.',
        'Give me a short practice example.',
        'Help me remember this vocabulary.',
      ]
    }
    if (route.name === 'quiz-list' || route.name === 'quizzes') {
      return ['How should I prepare for a quiz?', 'Which language skills do these quizzes practise?']
    }
    return [
      'What can I learn on LanguageWise?',
      'Help me choose a course.',
      'How can I build a study routine?',
    ]
  },
  tools: {
    chips: [
      chip('courses_list_courses', 'Courses', 'none', () => ({})),
      chip('courses_get_my_progress', 'My progress', 'course', withCourse),
      chip('courses_list_lessons', 'Lessons', 'course', withCourse),
      chip('courses_list_quizzes', 'Quizzes', 'course', withCourse),
      chip('courses_get_lesson_vocabulary', 'Lesson vocabulary', 'lesson', withLesson),
      chip('courses_get_flashcards', 'Flashcards', 'lesson', withLesson),
      chip('courses_get_my_vocabulary', 'My vocabulary', 'none', (route) =>
        courseCode(route) ? withCourse(route) : {},
      ),
      chip('courses_get_my_milestones', 'Milestones', 'none', () => ({ limit: 10 })),
    ],
    view: (result) => toolView(result.tool, result.result),
  },
}
