<script setup lang="ts">
import { computed } from 'vue'
import { toolLabel } from '../config/assistantTools'
import type { AssistantToolResult } from '../models/api'

interface ToolRow {
	primary: string
	secondary?: string
}

interface ToolView {
	summary?: string
	rows: ToolRow[]
}

const maximumRows = 12
const props = defineProps<{ result: AssistantToolResult }>()

const label = computed(() => toolLabel(props.result.tool))
const raw = computed(() => JSON.stringify(props.result.result, null, 2))

const view = computed<ToolView>(() => {
	const value = asRecord(props.result.result)
	if (props.result.isError) {
		return {
			summary:
				text(value.text).replace(/^An error occurred invoking '[^']+': /, '') ||
				'The tool could not complete this request.',
			rows: [],
		}
	}

	switch (props.result.tool) {
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
					primary: `${capitalise(text(milestone.kind))} completed`,
					secondary: formatDate(text(milestone.completedAt)),
				})),
			}
		default:
			return { rows: [] }
	}
})

const visibleRows = computed(() => view.value.rows.slice(0, maximumRows))
const hiddenRows = computed(() => Math.max(0, view.value.rows.length - maximumRows))

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

function formatDate(value: string) {
	const date = new Date(value)
	return Number.isNaN(date.getTime()) ? value : date.toLocaleString()
}
</script>

<template>
	<section
		class="garry-tool-card"
		:class="{ 'garry-tool-card-error': result.isError }"
		:aria-label="`${label} tool result`"
	>
		<header>
			<span class="garry-tool-card-badge">Tool</span>
			<strong>{{ label }}</strong>
		</header>
		<p v-if="view.summary" class="garry-tool-card-summary">{{ view.summary }}</p>
		<ul v-if="visibleRows.length" class="garry-tool-card-rows">
			<li v-for="(row, index) in visibleRows" :key="index">
				<span>{{ row.primary }}</span>
				<small v-if="row.secondary">{{ row.secondary }}</small>
			</li>
		</ul>
		<p v-if="hiddenRows" class="garry-tool-card-more">+{{ hiddenRows }} more</p>
		<p
			v-if="!result.isError && !view.summary && visibleRows.length === 0"
			class="garry-tool-card-summary"
		>
			No results.
		</p>
		<details class="garry-tool-card-raw">
			<summary>Raw result</summary>
			<pre>{{ raw }}</pre>
		</details>
	</section>
</template>
