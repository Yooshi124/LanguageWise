import { getCourseCode, getMode } from '../api.js';

const routeNames = {
	'mini-games-home': 'home',
	'mini-games-guess-the-word': 'guess-the-word',
	'mini-games-word-search': 'word-search',
	'mini-games-associations': 'associations'
};

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

function bestTimeRow(gameLabel, seconds) {
	if (seconds === null || seconds === undefined) return null;
	return { primary: gameLabel, secondary: `Best time ${number(seconds)}s` };
}

function toolView(tool, result) {
	const value = asRecord(result);
	switch (tool) {
		case 'games_get_completion_stats': {
			const streak = number(value.currentStreak);
			return {
				summary:
					`Guess the Word: ${number(value.guessTheWordCompletions)} won · ` +
					`Word Search: ${number(value.wordSearchCompletions)} won · ` +
					`Associations: ${number(value.associationsCompletions)} won` +
					(streak > 0 ? ` · ${streak} day streak` : ''),
				rows: [
					bestTimeRow('Guess the Word', value.bestGuessTheWordSeconds),
					bestTimeRow('Word Search', value.bestWordSearchSeconds),
					bestTimeRow('Associations', value.bestAssociationsSeconds)
				].filter((row) => row !== null)
			};
		}
		case 'games_list_game_languages':
			return {
				rows: list(value.languages).map((language) => ({
					primary: text(language.title),
					secondary: text(language.code).toUpperCase()
				}))
			};
		default:
			return { rows: [] };
	}
}

export const assistant = {
	apiBase: '/mini-games/api',
	welcome:
		'Ask me how each mini game works, what the vocabulary modes mean, or how to improve your score.',
	placeholder: 'Ask Garry about the mini games…',
	context: (route) => {
		const courseCode = getCourseCode();
		return {
			routeName: routeNames[route.name] ?? 'home',
			...(courseCode ? { courseCode } : {}),
			mode: getMode()
		};
	},
	suggestions: (route) => {
		if (route.name === 'mini-games-guess-the-word') {
			return [
				'How does Guess the Word work?',
				'What do the tile colours mean in Guess the Word?',
				'Any tips for finding the word in fewer guesses?'
			];
		}
		if (route.name === 'mini-games-word-search') {
			return ['How do I play Word Search?', 'How do hints work in Word Search?'];
		}
		if (route.name === 'mini-games-associations') {
			return ['How does Associations work?', 'How many mistakes can I make in Associations?'];
		}
		return [
			'How does Guess the Word work?',
			'How do I play Word Search?',
			'How does Associations work?'
		];
	},
	tools: {
		chips: [
			{
				tool: 'games_get_completion_stats',
				label: 'My stats',
				arguments: () => {
					const courseCode = getCourseCode();
					return courseCode ? { courseCode } : {};
				}
			},
			{
				tool: 'games_list_game_languages',
				label: 'My languages',
				arguments: () => ({})
			}
		],
		view: (result) => toolView(result.tool, result.result)
	}
};
