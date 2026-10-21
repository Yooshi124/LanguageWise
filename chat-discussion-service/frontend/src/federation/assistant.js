function postId(route) {
    return route.name === 'post' || route.name === 'post-edit' ? Number(route.params.id) : null;
}

function searchTerm(route) {
    return typeof route.query.q === 'string' ? route.query.q.trim() : '';
}

function canSearchPosts(route) {
    return route.name === 'forum' || route.name === 'my-posts';
}

function asRecord(value) {
    return typeof value === 'object' && value !== null && !Array.isArray(value) ? value : {};
}

function list(value) {
    return Array.isArray(value) ? value.map(asRecord) : [];
}

function text(value) {
    return typeof value === 'string' ? value : '';
}

function number(value) {
    return typeof value === 'number' ? value : 0;
}

function toolView(tool, result) {
    const value = asRecord(result);

    switch (tool) {
        case 'chat_list_forums':
            return {
                rows: list(value.forums).map((forum) => ({
                    primary: text(forum.name),
                    secondary: text(forum.code)
                }))
            };
        case 'chat_search_posts':
            return {
                rows: list(value.posts).map((post) => ({
                    primary: text(post.title),
                    secondary: `${text(post.forumName)} · ${number(post.commentCount)} comments`
                }))
            };
        case 'chat_get_post':
            return {
                summary: `${text(value.title)} — by ${text(value.authorName)} in ${text(value.forumName)}`,
                rows: list(value.comments).map((comment) => ({
                    primary: text(comment.content),
                    secondary: text(comment.authorName)
                }))
            };
        default:
            return { rows: [] };
    }
}

export const assistant = {
    apiBase: '/chat-discussion/api',
    welcome: 'Ask me how this forum works — posting, editing, comments, likes and search.',
    placeholder: 'Ask Garry about the forum…',
    // The backend validates routeName against its own allowlist, so keep it in step with feature.js.
    context: (route) => {
        const name = String(route.name ?? 'forums');

        if (name === 'forum') {
            return { routeName: name, forumCode: String(route.params.code ?? '') };
        }

        if (name === 'post' || name === 'post-edit') {
            return { routeName: name, postId: Number(route.params.id) };
        }

        return { routeName: name };
    },
    suggestions: (route) => {
        if (route.name === 'post') {
            return ['Summarise this thread', 'How do I comment on this post?', 'How do likes work?'];
        }

        if (route.name === 'post-create') {
            return ['Help me draft a post about my progress', 'Which forum should I post in?'];
        }

        return [
            'How do I create a new post?',
            'How do I edit my post?',
            'How do likes work?',
            'How do I find the posts I wrote?'
        ];
    },
    tools: {
        chips: [
            {
                tool: 'chat_list_forums',
                label: 'Forums',
                arguments: () => ({})
            },
            {
                tool: 'chat_search_posts',
                label: 'Search posts',
                arguments: (route) => {
                    const query = searchTerm(route) || (window.prompt('Search posts for…') ?? '').trim();
                    if (!query) return null;
                    return route.name === 'forum'
                        ? { query, forumCode: String(route.params.code ?? '') }
                        : { query };
                },
                unavailable: (route) => canSearchPosts(route) ? null : 'Open a forum or your posts to search.'
            },
            {
                tool: 'chat_get_post',
                label: 'This post',
                arguments: (route) => ({ postId: postId(route) }),
                unavailable: (route) => postId(route) ? null : 'Open a post to use this tool.'
            }
        ],
        view: (result) => toolView(result.tool, result.result)
    }
};
