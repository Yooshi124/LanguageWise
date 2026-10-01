// The backend validates routeName against its own allowlist, so keep it in step with feature.js.
export const assistant = {
    apiBase: '/chat-discussion/api',
    welcome: 'Ask me how this forum works — posting, editing, comments, likes and search.',
    placeholder: 'Ask Garry about the forum…',
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
    suggestions: () => [
        'How do I create a new post?',
        'How do I edit my post?',
        'How do likes work?',
        'How do I find the posts I wrote?'
    ]
};
