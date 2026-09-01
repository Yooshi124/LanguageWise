// Cross-service navigation shown in the sidebar. Mirrors the navigation used
// by the quizzes-courses frontend so every service shows the same entries.

const sharedFrontend = `${window.location.protocol}//${window.location.hostname}:3000`;

export const sharedHomeHref = `${sharedFrontend}/`;

export const serviceNavigation = [
	{ label: 'Home', href: sharedHomeHref, icon: 'home' },
	{ label: 'Mini Games', href: `${sharedFrontend}/mini-games/`, icon: 'games', current: true },
	{ label: 'Discussion Forum', href: `${sharedFrontend}/chat-discussion/`, icon: 'discussion' },
	{ label: 'Quizzes & Courses', href: `${sharedFrontend}/quizzes-and-courses/`, icon: 'courses' },
	{ label: 'Quests & Achievements', href: `${sharedFrontend}/quests-and-achievements/`, icon: 'quests' },
	{ label: 'Leaderboard & Analytics', icon: 'analytics', disabled: true },
];
