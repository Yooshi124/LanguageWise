import { beforeEach, describe, expect, it, vi } from 'vitest'

vi.mock('../api/assistant', () => {
  class AssistantApiError extends Error {
    constructor(public readonly status: number, message: string) {
      super(message)
    }
  }

  return {
    AssistantApiError,
    streamAssistantMessage: vi.fn(),
  }
})

import { streamAssistantMessage } from '../api/assistant'
import { useGarryAssistant } from './useGarryAssistant'

const context = { routeName: 'quests-achievements-home' as const }
let userId = 100

beforeEach(() => {
  sessionStorage.clear()
  vi.mocked(streamAssistantMessage).mockReset()
  userId++
})

describe('useGarryAssistant', () => {
  it('streams and persists a grounded conversation request', async () => {
    vi.mocked(streamAssistantMessage).mockImplementation(async (request, handlers) => {
      expect(request).toMatchObject({
        message: 'What achievement should I aim for?',
        history: [],
        context,
      })
      handlers.onDelta('Aim for your next incomplete achievement.')
      handlers.onDone()
    })
    const assistant = useGarryAssistant(userId)

    await assistant.send('What achievement should I aim for?', context, vi.fn())

    expect(assistant.messages.value.map((message) => message.content)).toEqual([
      'What achievement should I aim for?',
      'Aim for your next incomplete achievement.',
    ])
    expect(sessionStorage.getItem(
      `languagewise:quests-achievements:assistant:v1:user:${userId}`,
    )).toContain('Aim for your next incomplete achievement.')
  })

  it('reports an expired session through the host callback', async () => {
    const { AssistantApiError } = await import('../api/assistant')
    vi.mocked(streamAssistantMessage).mockRejectedValue(
      new AssistantApiError(401, 'Please sign in again.'),
    )
    const unauthorized = vi.fn()
    const assistant = useGarryAssistant(userId)

    await assistant.send('Explain my notifications.', context, unauthorized)

    expect(unauthorized).toHaveBeenCalledOnce()
    expect(assistant.error.value).toBe('Please sign in again.')
  })
})