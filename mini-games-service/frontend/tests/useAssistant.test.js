import { beforeEach, describe, expect, it, vi } from 'vitest';

// The composable talks to the network through this module; replace it with a
// controllable stub before importing the composable.
vi.mock('../src/api/assistant.js', () => ({
  streamAssistantMessage: vi.fn()
}));

import { streamAssistantMessage } from '../src/api/assistant.js';
import { useAssistant } from '../src/composables/useAssistant.js';

function resolveWithFullReply() {
  streamAssistantMessage.mockImplementation(async (_request, handlers) => {
    handlers.onDelta('Hello');
    handlers.onDelta(' there');
    handlers.onDone();
  });
}

describe('useAssistant', () => {
  // Each test uses a unique user id so the module-level conversation state is never
  // shared between tests (initialize() no-ops when the user id is unchanged).
  let userId = 999000;
  const nextUserId = () => ++userId;

  beforeEach(() => {
    sessionStorage.clear();
    vi.clearAllMocks();
  });

  it('starts empty and collapsed', () => {
    const assistant = useAssistant(nextUserId());

    expect(assistant.messages.value).toEqual([]);
    expect(assistant.expanded.value).toBe(false);
    expect(assistant.streaming.value).toBe(false);
    expect(assistant.error.value).toBeNull();
  });

  it('streams the assistant reply into a single message and persists it', async () => {
    resolveWithFullReply();
    const id = nextUserId();
    const assistant = useAssistant(id);
    const context = { routeName: 'home' };

    await assistant.send('How does Word Search work?', context);

    const messages = assistant.messages.value;
    expect(messages).toHaveLength(2);
    expect(messages[0].role).toBe('user');
    expect(messages[1]).toMatchObject({ role: 'assistant', content: 'Hello there' });
    expect(sessionStorage.getItem(`languagewise:assistant:v1:user:${id}`)).toContain('Hello there');
  });

  it('sends route context and bounded history to the API', async () => {
    resolveWithFullReply();
    const assistant = useAssistant(nextUserId());
    const context = { routeName: 'guess-the-word', courseCode: 'de', mode: 'content' };

    await assistant.send('First question', context);
    await assistant.send('Second question', context);

    const lastRequest = streamAssistantMessage.mock.calls.at(-1)[0];
    expect(lastRequest.message).toBe('Second question');
    expect(lastRequest.context).toEqual(context);
    expect(lastRequest.history).toHaveLength(2);
    expect(lastRequest.history.map((entry) => entry.role)).toEqual(['user', 'assistant']);
  });

  it('removes the pending assistant message and reports an error when the stream fails', async () => {
    streamAssistantMessage.mockRejectedValue(new Error('Garry is unavailable (500)'));
    const assistant = useAssistant(nextUserId());

    await assistant.send('Ping', { routeName: 'home' });

    expect(assistant.error.value).toBe('Garry is unavailable (500)');
    // Only the user message remains; the empty assistant placeholder is dropped.
    expect(assistant.messages.value).toHaveLength(1);
    expect(assistant.messages.value[0].role).toBe('user');
    expect(assistant.streaming.value).toBe(false);
  });

  it('clear empties the conversation and storage', async () => {
    resolveWithFullReply();
    const id = nextUserId();
    const assistant = useAssistant(id);
    await assistant.send('Hi', { routeName: 'home' });

    assistant.clear();

    expect(assistant.messages.value).toEqual([]);
    expect(sessionStorage.getItem(`languagewise:assistant:v1:user:${id}`)).toBe('[]');
  });

  it('ignores blank messages', async () => {
    resolveWithFullReply();
    const assistant = useAssistant(nextUserId());

    await assistant.send('   ', { routeName: 'home' });

    expect(assistant.messages.value).toEqual([]);
    expect(streamAssistantMessage).not.toHaveBeenCalled();
  });

  it('restores a persisted conversation for the same user', async () => {
    resolveWithFullReply();
    const id = nextUserId();
    const first = useAssistant(id);
    await first.send('Remember me', { routeName: 'home' });

    // A fresh user has no transcript; switching back reloads the stored one.
    const other = useAssistant(nextUserId());
    expect(other.messages.value).toEqual([]);

    const reloaded = useAssistant(id);
    expect(reloaded.messages.value.map((message) => message.content)).toEqual([
      'Remember me',
      'Hello there'
    ]);
  });
});
