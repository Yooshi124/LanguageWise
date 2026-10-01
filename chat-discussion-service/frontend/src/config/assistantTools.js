/**
 * Quick-action tool chips shown in Garry's panel, mirroring the
 * quizzes-courses-service and mini-games-service chip configuration.
 * @typedef {{ forumCode?: string, postId?: number, query?: string }} AssistantToolContext
 * @typedef {{ tool: string, label: string, requires: 'none' | 'post' | 'query', arguments: (context: AssistantToolContext) => Record<string, unknown> }} AssistantToolChip
 */

/** @type {AssistantToolChip[]} */
export const assistantToolChips = [
    {
        tool: 'chat_list_forums',
        label: 'Forums',
        requires: 'none',
        arguments: () => ({})
    },
    {
        tool: 'chat_search_posts',
        label: 'Search posts',
        requires: 'query',
        arguments: ({ query, forumCode }) => (forumCode ? { query, forumCode } : { query })
    },
    {
        tool: 'chat_get_post',
        label: 'This post',
        requires: 'post',
        arguments: ({ postId }) => ({ postId })
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
    if (chip.requires === 'post' && !context.postId) {
        return 'Open a post to use this tool.';
    }

    if (chip.requires === 'query' && !context.query?.trim()) {
        return 'Type what to search for, then use this tool.';
    }

    return null;
}
