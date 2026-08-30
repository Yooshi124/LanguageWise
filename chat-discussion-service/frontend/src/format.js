const dateFormat = new Intl.DateTimeFormat('en-AU', {
    day: 'numeric',
    month: 'short',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit'
});

export function formatDate(value) {
    if (!value) {
        return '';
    }

    const parsed = new Date(value);
    return Number.isNaN(parsed.getTime()) ? '' : dateFormat.format(parsed);
}

export function excerpt(text, length = 140) {
    if (!text) {
        return '';
    }

    const collapsed = text.replace(/\s+/g, ' ').trim();
    return collapsed.length <= length ? collapsed : `${collapsed.slice(0, length - 1)}…`;
}
