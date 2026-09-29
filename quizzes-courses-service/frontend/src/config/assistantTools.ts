export type AssistantToolRequirement = 'none' | 'course' | 'lesson'

export interface AssistantToolContext {
	courseCode?: string
	lessonSlug?: string
}

export interface AssistantToolChip {
	tool: string
	label: string
	requires: AssistantToolRequirement
	arguments: (context: AssistantToolContext) => Record<string, unknown>
}

export const assistantToolChips: AssistantToolChip[] = [
	{
		tool: 'courses_list_courses',
		label: 'Courses',
		requires: 'none',
		arguments: () => ({}),
	},
	{
		tool: 'courses_get_my_progress',
		label: 'My progress',
		requires: 'course',
		arguments: ({ courseCode }) => ({ courseCode }),
	},
	{
		tool: 'courses_list_lessons',
		label: 'Lessons',
		requires: 'course',
		arguments: ({ courseCode }) => ({ courseCode }),
	},
	{
		tool: 'courses_list_quizzes',
		label: 'Quizzes',
		requires: 'course',
		arguments: ({ courseCode }) => ({ courseCode }),
	},
	{
		tool: 'courses_get_lesson_vocabulary',
		label: 'Lesson vocabulary',
		requires: 'lesson',
		arguments: ({ courseCode, lessonSlug }) => ({ courseCode, lessonSlug }),
	},
	{
		tool: 'courses_get_flashcards',
		label: 'Flashcards',
		requires: 'lesson',
		arguments: ({ courseCode, lessonSlug }) => ({ courseCode, lessonSlug }),
	},
	{
		tool: 'courses_get_my_vocabulary',
		label: 'My vocabulary',
		requires: 'none',
		arguments: ({ courseCode }) => (courseCode ? { courseCode } : {}),
	},
	{
		tool: 'courses_get_my_milestones',
		label: 'Milestones',
		requires: 'none',
		arguments: () => ({ limit: 10 }),
	},
]

export function toolLabel(tool: string) {
	return assistantToolChips.find((chip) => chip.tool === tool)?.label ?? tool
}

export function missingRequirement(
	chip: AssistantToolChip,
	context: AssistantToolContext,
) {
	if (chip.requires === 'course' && !context.courseCode) return 'Open a course to use this tool.'
	if (chip.requires === 'lesson' && !(context.courseCode && context.lessonSlug)) {
		return 'Open a lesson to use this tool.'
	}
	return null
}
