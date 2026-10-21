import { beforeEach, describe, expect, it, vi } from 'vitest'
import type { FeatureAssistant } from '../federation/featureAssistants'
import { callAssistantTool, listAssistantTools, streamAssistantMessage } from './garryApi'
import { garryStorageKey, useGarryAssistant } from './useGarryAssistant'

vi.mock('./garryApi', async (importOriginal) => ({
  ...await importOriginal<typeof import('./garryApi')>(),
  streamAssistantMessage: vi.fn(),
  listAssistantTools: vi.fn(),
  callAssistantTool: vi.fn(),
}))

function assistant(apiBase: string): FeatureAssistant {
  return {
    apiBase,
    welcome: 'Welcome',
    placeholder: 'Ask a question',
    context: () => ({}),
    suggestions: () => [],
    tools: { chips: [], view: () => ({ rows: [] }) },
  }
}

function deferred<T = void>() {
  let resolve!: (value: T | PromiseLike<T>) => void
  let reject!: (reason: unknown) => void
  const promise = new Promise<T>((yes, no) => { resolve = yes; reject = no })
  return { promise, resolve, reject }
}

beforeEach(() => {
  sessionStorage.clear()
  vi.resetAllMocks()
})

describe('independent Garry conversations', () => {
  it('streams two conversations concurrently without sharing messages, cancellation or storage', async () => {
    const firstRequest = deferred()
    const secondRequest = deferred()
    vi.mocked(streamAssistantMessage)
      .mockReturnValueOnce(firstRequest.promise)
      .mockReturnValueOnce(secondRequest.promise)
    const first = useGarryAssistant(1, 'first', vi.fn())
    const second = useGarryAssistant(1, 'second', vi.fn())
    first.activate('courses', assistant('/courses/api'))
    second.activate('courses', assistant('/courses/api'))

    const sendingFirst = first.send('First question', { routeName: 'lesson' })
    const sendingSecond = second.send('Second question', { routeName: 'home' })
    const firstCall = vi.mocked(streamAssistantMessage).mock.calls[0]!
    const secondCall = vi.mocked(streamAssistantMessage).mock.calls[1]!
    expect(first.streaming.value).toBe(true)
    expect(second.streaming.value).toBe(true)
    expect(firstCall[3]).not.toBe(secondCall[3])
    secondCall[2].onDelta('Second answer')
    firstCall[2].onDelta('Partial first answer')
    first.cancel()
    expect(firstCall[3].aborted).toBe(true)
    expect(secondCall[3].aborted).toBe(false)
    firstRequest.reject(new DOMException('Stopped', 'AbortError'))
    secondCall[2].onDone('stop')
    secondRequest.resolve()
    await Promise.all([sendingFirst, sendingSecond])

    expect(first.messages.value.map((message) => message.content))
      .toEqual(['First question', 'Partial first answer'])
    expect(second.messages.value.map((message) => message.content))
      .toEqual(['Second question', 'Second answer'])
    expect(first.error.value).toBeNull()
    expect(second.error.value).toBeNull()
    expect(sessionStorage.getItem(garryStorageKey(1, 'first'))).not.toContain('Second answer')
    expect(sessionStorage.getItem(garryStorageKey(1, 'second'))).not.toContain('First question')
  })

  it('keeps an in-flight answer in its original context and sends future messages to the new feature', async () => {
    const pending = deferred()
    vi.mocked(streamAssistantMessage).mockReturnValueOnce(pending.promise)
    const chat = useGarryAssistant(1, 'first', vi.fn())
    chat.activate('courses', assistant('/courses/api'))
    const sending = chat.send('Explain this lesson', { routeName: 'lesson', courseCode: 'it' })
    const firstCall = vi.mocked(streamAssistantMessage).mock.calls[0]!
    chat.activate('games', assistant('/games/api'))
    expect(firstCall[3].aborted).toBe(false)
    firstCall[2].onDelta('The original lesson answer')
    firstCall[2].onDone('stop')
    pending.resolve()
    await sending

    vi.mocked(streamAssistantMessage).mockImplementationOnce(async (_base, _request, handlers) => {
      handlers.onDelta('The new games answer')
      handlers.onDone('stop')
    })
    await chat.send('How do I play?', { routeName: 'word-search' })
    const secondCall = vi.mocked(streamAssistantMessage).mock.calls[1]!
    expect(firstCall[0]).toBe('/courses/api')
    expect(firstCall[1].context).toEqual({ routeName: 'lesson', courseCode: 'it' })
    expect(secondCall[0]).toBe('/games/api')
    expect(secondCall[1].context).toEqual({ routeName: 'word-search' })
    expect(secondCall[1].history).toEqual([
      { role: 'user', content: 'Explain this lesson' },
      { role: 'assistant', content: 'The original lesson answer' },
    ])
    expect(chat.messages.value.map((message) => message.featureKey))
      .toEqual(['courses', 'courses', 'games', 'games'])
    expect(chat.messages.value[1]?.content).toBe('The original lesson answer')
  })

  it('does not send to the old service on a page with no assistant', async () => {
    const chat = useGarryAssistant(1, 'first', vi.fn())
    chat.activate('courses', assistant('/courses/api'))
    chat.activate()
    await chat.send('No feature is active', {})
    expect(streamAssistantMessage).not.toHaveBeenCalled()
    expect(chat.messages.value).toEqual([])
  })

  it('ignores late callbacks after clearing without interrupting the next request', async () => {
    const stale = deferred()
    const current = deferred()
    vi.mocked(streamAssistantMessage)
      .mockReturnValueOnce(stale.promise)
      .mockReturnValueOnce(current.promise)
    const chat = useGarryAssistant(1, 'first', vi.fn())
    chat.activate('games', assistant('/games/api'))
    const sendingOld = chat.send('Old question', {})
    const oldCall = vi.mocked(streamAssistantMessage).mock.calls[0]!
    chat.clear()
    const sendingNew = chat.send('New question', {})
    oldCall[2].onDelta('Stale answer')
    oldCall[2].onDone('fallback')
    stale.reject(new DOMException('Stopped', 'AbortError'))
    await sendingOld
    expect(chat.streaming.value).toBe(true)
    expect(chat.messages.value.map((message) => message.content)).toEqual(['New question', ''])
    const newCall = vi.mocked(streamAssistantMessage).mock.calls[1]!
    newCall[2].onDelta('Fresh answer')
    newCall[2].onDone('stop')
    current.resolve()
    await sendingNew
    expect(chat.streaming.value).toBe(false)
    expect(chat.messages.value[1]?.fallback).toBeUndefined()
    expect(chat.messages.value[1]?.content).toBe('Fresh answer')
  })

  it('aborts all requests on close and ignores their late results', async () => {
    const pending = deferred()
    vi.mocked(streamAssistantMessage).mockReturnValueOnce(pending.promise)
    const unauthorized = vi.fn()
    const chat = useGarryAssistant(1, 'first', unauthorized)
    chat.activate('courses', assistant('/courses/api'))
    const sending = chat.send('Question', {})
    const call = vi.mocked(streamAssistantMessage).mock.calls[0]!
    chat.dispose()
    expect(call[3].aborted).toBe(true)
    call[2].onDelta('A late answer')
    call[2].onDone('stop')
    pending.resolve()
    await sending
    expect(unauthorized).not.toHaveBeenCalled()
    expect(chat.messages.value[1]?.content).toBe('')
    expect(sessionStorage.getItem(garryStorageKey(1, 'first'))).toBeNull()
  })

  it('restores transcripts by window and user, including message context', async () => {
    vi.mocked(streamAssistantMessage).mockImplementation(async (_base, _request, handlers) => {
      handlers.onDelta('Saved answer')
      handlers.onDone('stop')
    })
    const first = useGarryAssistant(1, 'first', vi.fn())
    first.activate('courses', assistant('/courses/api'))
    await first.send('Saved question', {})
    expect(useGarryAssistant(1, 'first', vi.fn()).messages.value).toEqual(first.messages.value)
    expect(useGarryAssistant(1, 'second', vi.fn()).messages.value).toEqual([])
    expect(useGarryAssistant(2, 'first', vi.fn()).messages.value).toEqual([])
  })

  it('imports an existing feature transcript into the main window only once', () => {
    sessionStorage.setItem('languagewise:garry:v2:courses:user:1', JSON.stringify([
      { id: 'old', role: 'assistant', content: 'Existing answer' },
    ]))
    const chat = useGarryAssistant(1, 'main', vi.fn())
    chat.activate('courses', assistant('/courses/api'))
    chat.activate('games', assistant('/games/api'))
    expect(chat.messages.value).toEqual([
      { id: 'old', role: 'assistant', content: 'Existing answer', featureKey: 'courses' },
    ])
  })

  it('discards stale tool catalogues on navigation, but preserves completed old-context tool results', async () => {
    const oldCatalogue = deferred<Awaited<ReturnType<typeof listAssistantTools>>>()
    vi.mocked(listAssistantTools).mockReturnValueOnce(oldCatalogue.promise)
    const toolResult = deferred<Awaited<ReturnType<typeof callAssistantTool>>>()
    vi.mocked(callAssistantTool).mockReturnValueOnce(toolResult.promise)
    const chat = useGarryAssistant(1, 'first', vi.fn())
    chat.activate('courses', assistant('/courses/api'))
    const loading = chat.setToolsEnabled(true)
    const running = chat.runTool({ name: 'courses_list_courses', title: 'Courses', description: '' }, {})
    const catalogueCall = vi.mocked(listAssistantTools).mock.calls[0]!
    chat.activate('games', assistant('/games/api'))
    expect(catalogueCall[1]?.aborted).toBe(true)
    oldCatalogue.resolve([{ name: 'courses_list_courses', title: 'Courses', description: '' }])
    toolResult.resolve({ tool: 'courses_list_courses', isError: false, result: { courses: [] } })
    await Promise.all([loading, running])
    expect(chat.tools.value).toEqual([])
    expect(chat.toolsEnabled.value).toBe(false)
    expect(chat.messages.value[0]?.featureKey).toBe('courses')
    expect(chat.messages.value[0]?.toolResults?.[0]?.tool).toBe('courses_list_courses')
  })
})
