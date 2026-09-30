/**
 * Quick-action tool chips shown in the mini games assistant panel, mirroring the
 * quizzes-courses-service assistant tool chip configuration.
 * @typedef {{ courseCode?: string }} AssistantToolContext
 * @typedef {{ tool: string, label: string, requires: 'none' | 'course', arguments: (context: AssistantToolContext) => Record<string, unknown> }} AssistantToolChip
 */

/** @type {AssistantToolChip[]} */
export const assistantToolChips = [
  {
    tool: 'games_get_completion_stats',
    label: 'My stats',
    requires: 'none',
    arguments: ({ courseCode }) => (courseCode ? { courseCode } : {})
  },
  {
    tool: 'games_list_game_languages',
    label: 'My languages',
    requires: 'none',
    arguments: () => ({})
  }
];

export function toolLabel(tool) {
  return assistantToolChips.find((chip) => chip.tool === tool)?.label ?? tool;
}

/**
 * @param {AssistantToolChip} chip
 * @param {AssistantToolContext} context
 */
export function missingRequirement(chip, context) {
  if (chip.requires === 'course' && !context.courseCode) return 'Select a language to use this tool.';
  return null;
}
