/**
 * The flag and accent colour for each language forum, keyed by the forum code the
 * course catalogue sets. Kept in step with the quizzes and courses frontend, which
 * uses the same six codes, files and colours on its home page, so a language looks
 * the same in both services.
 *
 * A forum whose code is not listed here — Global, or a language with no course
 * behind it — falls back to an icon rather than a flag.
 */
const languages = {
    de: { flag: 'de.svg', colour: '#f59e0b' },
    fr: { flag: 'fr.svg', colour: '#3b82f6' },
    it: { flag: 'it.svg', colour: '#16a34a' },
    nl: { flag: 'nl.svg', colour: '#f97316' },
    es: { flag: 'es.svg', colour: '#dc2626' },
    pl: { flag: 'pl.svg', colour: '#d4213d' }
};

const flagsPath = `${import.meta.env.BASE_URL}flags`;

/** The accent colour for a forum, falling back to the shared indigo. */
export function forumColour(code) {
    return languages[code]?.colour ?? '#4f46e5';
}

/** The flag URL for a language forum, or null when the forum has no flag. */
export function forumFlag(code) {
    const language = languages[code];
    return language ? `${flagsPath}/${language.flag}` : null;
}
