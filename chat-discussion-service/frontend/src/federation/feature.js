import ChatDiscussionComponent from './ChatDiscussionComponent.vue';
import ForumIndexView from '../views/ForumIndexView.vue';
import ForumView from '../views/ForumView.vue';
import PostView from '../views/PostView.vue';
import MyPostsView from '../views/MyPostsView.vue';
import PostCreateView from '../views/PostCreateView.vue';
import PostEditView from '../views/PostEditView.vue';

export { ChatDiscussionComponent };

export const metadata = {
    key: 'chat-discussion',
    displayName: 'Discussion Forum',
    icon: 'discussion',
    basePath: '/chat-discussion',
    requiresAuth: true
};

export const routes = [
    { path: '', name: 'forums', component: ForumIndexView },
    { path: 'forums/:code', name: 'forum', component: ForumView, props: true },
    { path: 'my-posts', name: 'my-posts', component: MyPostsView },
    { path: 'new', name: 'post-create', component: PostCreateView },
    { path: 'posts/:id', name: 'post', component: PostView, props: true },
    { path: 'posts/:id/edit', name: 'post-edit', component: PostEditView, props: true }
];