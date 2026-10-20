import { flushPromises, mount, type VueWrapper } from '@vue/test-utils'
import { createMemoryHistory, createRouter } from 'vue-router'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { featureAssistants, type FeatureAssistant } from '../federation/featureAssistants'
import { streamAssistantMessage } from '../composables/garryApi'
import { garryStorageKey } from '../composables/useGarryAssistant'
import GarryAssistant from './GarryAssistant.vue'

vi.mock('../composables/garryApi', async (importOriginal) => ({
  ...await importOriginal<typeof import('../composables/garryApi')>(),
  streamAssistantMessage: vi.fn(),
}))

const courses: FeatureAssistant = {
  apiBase: '/courses/api',
  welcome: 'Courses welcome',
  placeholder: 'Ask about courses',
  context: (route) => ({ routeName: String(route.name) }),
  suggestions: () => ['Explain a lesson'],
  tools: {
    chips: [{ tool: 'courses_list_courses', label: 'My courses', arguments: () => ({}) }],
    view: () => ({ summary: 'Original courses presentation', rows: [] }),
  },
}
const games: FeatureAssistant = {
  ...courses,
  apiBase: '/games/api',
  welcome: 'Games welcome',
  placeholder: 'Ask about games',
  tools: {
    chips: [],
    view: () => ({ summary: 'Wrong games presentation', rows: [] }),
  },
}
let wrapper: VueWrapper | undefined
let contentBounds: DOMRect

beforeEach(() => {
  sessionStorage.clear()
  vi.resetAllMocks()
  featureAssistants.clear()
  featureAssistants.set('courses', courses)
  featureAssistants.set('games', games)
  contentBounds = new DOMRect(88, 0, window.innerWidth - 88, window.innerHeight)
  const content = document.createElement('div')
  content.className = 'app-shell-content'
  vi.spyOn(content, 'getBoundingClientRect').mockImplementation(() => contentBounds)
  document.body.append(content)
  vi.mocked(streamAssistantMessage).mockImplementation(async (_base, _request, handlers) => {
    handlers.onDelta('A response')
    handlers.onDone('stop')
  })
})

afterEach(() => {
  wrapper?.unmount()
  wrapper = undefined
  document.body.innerHTML = ''
  vi.unstubAllGlobals()
})

async function mountAssistant() {
  const router = createRouter({
    history: createMemoryHistory(),
    routes: [
      { path: '/courses', name: 'courses', component: { template: '<div />' } },
      { path: '/games', name: 'games', component: { template: '<div />' } },
    ],
  })
  await router.push('/courses')
  wrapper = mount(GarryAssistant, {
    props: { userId: 1, featureKey: 'courses', assistant: courses },
    attachTo: document.body,
    global: {
      plugins: [router],
      stubs: {
        VBtn: { template: '<button><slot /></button>' },
        VAlert: { template: '<div><slot /><slot name="append" /></div>' },
        VSpeedDial: {
          props: ['modelValue'],
          emits: ['update:modelValue'],
          template: `<div>
            <slot name="activator" :props="{
              onClick: () => $emit('update:modelValue', !modelValue),
              'aria-expanded': modelValue,
            }" />
            <div v-if="modelValue" role="menu"><slot /></div>
          </div>`,
        },
        GarryDocsSearch: true,
      },
    },
  })
  await selectChat(1)
  return router
}

function panel(number = 1): HTMLElement {
  const element = document.querySelector<HTMLElement>(`[aria-label="Garry chat ${number}"]`)
  if (!element) throw new Error(`Missing chat ${number}`)
  return element
}

async function click(root: ParentNode, label: string) {
  const button = root.querySelector<HTMLButtonElement>(`button[aria-label="${label}"]`)
  if (!button) throw new Error(`Missing button: ${label}`)
  button.click()
  await flushPromises()
}

async function selectChat(number: number) {
  await click(document.body, 'Open Garry chat menu')
  await click(document.body, `Open Garry chat ${number}`)
}

async function send(root: HTMLElement, text: string) {
  const input = root.querySelector<HTMLTextAreaElement>('textarea')!
  input.value = text
  input.dispatchEvent(new Event('input', { bubbles: true }))
  await flushPromises()
  root.querySelector('form')!.dispatchEvent(new Event('submit', { bubbles: true, cancelable: true }))
  await flushPromises()
}

describe('Garry floating workspace', () => {
  it('keeps another chat focused when a background response completes', async () => {
    await mountAssistant()
    let finishResponse!: () => void
    vi.mocked(streamAssistantMessage).mockImplementationOnce((_base, _request, handlers) =>
      new Promise<void>((resolve) => {
        finishResponse = () => {
          handlers.onDelta('Background response')
          handlers.onDone('stop')
          resolve()
        }
      }),
    )
    await send(panel(), 'First conversation')
    await click(panel(), 'New Garry chat')
    const second = panel(2)
    const input = second.querySelector<HTMLTextAreaElement>('textarea')!
    input.focus()
    input.value = 'Still writing in the second chat'
    input.dispatchEvent(new Event('input', { bubbles: true }))
    await flushPromises()
    const foregroundOrder = second.style.zIndex
    expect(document.activeElement).toBe(input)
    expect(Number(foregroundOrder)).toBeGreaterThan(Number(panel().style.zIndex))

    finishResponse()
    await flushPromises()

    expect(panel().textContent).toContain('Background response')
    expect(document.activeElement).toBe(input)
    expect(second.style.zIndex).toBe(foregroundOrder)
    expect(Number(second.style.zIndex)).toBeGreaterThan(Number(panel().style.zIndex))
    expect(input.value).toBe('Still writing in the second chat')
  })

  it('opens additional chats at the same bottom-right position without cascading offsets', async () => {
    await mountAssistant()
    const first = panel()
    for (let number = 2; number <= 4; number++) {
      await click(panel(number - 1), 'New Garry chat')
      const next = panel(number)
      expect(next.style.left).toBe(first.style.left)
      expect(next.style.top).toBe(first.style.top)
      expect(next.style.width).toBe(first.style.width)
      expect(next.style.height).toBe(first.style.height)
      expect(Number(next.style.zIndex)).toBeGreaterThan(Number(panel(number - 1).style.zIndex))
    }
  })

  it('resizes from the bottom right with the pointer and keyboard without moving the top left', async () => {
    await mountAssistant()
    const first = panel()
    const handle = first.querySelector<HTMLButtonElement>('.garry-resize-handle-bottom-right')!
    handle.setPointerCapture = vi.fn()
    const left = first.style.left
    const top = first.style.top
    const width = Number.parseInt(first.style.width)
    const height = Number.parseInt(first.style.height)
    async function pointer(type: string, x: number, y: number) {
      const event = new MouseEvent(type, { bubbles: true, button: 0, clientX: x, clientY: y })
      Object.defineProperty(event, 'pointerId', { value: 1 })
      handle.dispatchEvent(event)
      await flushPromises()
    }
    await pointer('pointerdown', 500, 500)
    expect(handle.setPointerCapture).toHaveBeenCalledWith(1)
    await pointer('pointermove', 460, 440)
    expect(first.style.width).toBe(`${width - 40}px`)
    expect(first.style.height).toBe(`${height - 60}px`)
    await pointer('pointermove', 480, 470)
    expect(first.style.width).toBe(`${width - 20}px`)
    expect(first.style.height).toBe(`${height - 30}px`)
    expect(first.style.left).toBe(left)
    expect(first.style.top).toBe(top)
    await pointer('pointermove', 10000, 10000)
    expect(Number.parseInt(first.style.width) + Number.parseInt(left)).toBe(window.innerWidth - 16)
    expect(Number.parseInt(first.style.height) + Number.parseInt(top)).toBe(window.innerHeight - 126)
    await pointer('pointerup', 10000, 10000)
    const cappedWidth = first.style.width
    await pointer('pointermove', 0, 0)
    expect(first.style.width).toBe(cappedWidth)
    handle.dispatchEvent(new KeyboardEvent('keydown', { key: 'ArrowLeft', bubbles: true }))
    await flushPromises()
    expect(first.style.width).toBe(`${Number.parseInt(cappedWidth) - 10}px`)
    expect(first.style.left).toBe(left)
    expect(first.style.top).toBe(top)
    await pointer('pointerdown', 500, 500)
    await pointer('pointermove', -10000, -10000)
    expect(first.style.width).toBe('340px')
    expect(first.style.height).toBe('320px')
    await pointer('pointercancel', -10000, -10000)
    await pointer('pointermove', 10000, 10000)
    expect(first.style.width).toBe('340px')
    expect(first.querySelectorAll('.garry-resize-handle')).toHaveLength(2)
  })

  it('maximizes within the main content area, follows sidebar changes and restores the resized geometry', async () => {
    let notifyLayout = () => {}
    const observe = vi.fn()
    const disconnect = vi.fn()
    vi.stubGlobal('ResizeObserver', class {
      constructor(callback: () => void) { notifyLayout = callback }
      observe = observe
      disconnect = disconnect
    })
    await mountAssistant()
    const first = panel()
    expect(first.closest('.garry-assistant')?.parentElement).toBe(document.body)
    const resize = first.querySelector<HTMLButtonElement>('.garry-resize-handle')!
    const originalWidth = Number.parseInt(first.style.width)
    resize.dispatchEvent(new KeyboardEvent('keydown', { key: 'ArrowLeft', bubbles: true }))
    await flushPromises()
    expect(first.style.width).toBe(`${originalWidth - 10}px`)
    const resized = first.getAttribute('style')
    await click(first, 'Maximize Garry')
    expect(first.style.left).toBe('104px')
    expect(first.style.top).toBe('16px')
    expect(first.style.width).toBe(`${window.innerWidth - 120}px`)
    expect(first.style.height).toBe(`${window.innerHeight - 142}px`)
    expect(first.querySelector('.garry-resize-handle')).toBeNull()
    expect(document.querySelector('.garry-launcher')).not.toBeNull()
    expect(observe).toHaveBeenCalledWith(document.querySelector('.app-shell-content'))
    contentBounds = new DOMRect(310, 0, window.innerWidth - 310, window.innerHeight)
    notifyLayout()
    await flushPromises()
    expect(first.style.left).toBe('326px')
    expect(first.style.width).toBe(`${window.innerWidth - 342}px`)
    await click(first, 'Restore Garry window')
    expect(first.style.width).toBe(`${originalWidth - 10}px`)
    expect(resized).toContain(`width: ${originalWidth - 10}px`)
    wrapper!.unmount()
    wrapper = undefined
    expect(disconnect).toHaveBeenCalledOnce()
  })

  it('keeps mobile navigation and the launcher outside the maximized panel', async () => {
    vi.stubGlobal('innerWidth', 390)
    vi.stubGlobal('innerHeight', 844)
    contentBounds = new DOMRect(0, 0, 390, 844)
    const navigation = document.createElement('button')
    navigation.className = 'mobile-nav-trigger'
    vi.spyOn(navigation, 'getBoundingClientRect').mockReturnValue(new DOMRect(14, 14, 44, 44))
    document.body.append(navigation)
    await mountAssistant()
    await click(panel(), 'Maximize Garry')
    expect(panel().style.left).toBe('16px')
    expect(panel().style.top).toBe('74px')
    expect(panel().style.width).toBe('358px')
    expect(panel().style.height).toBe('644px')
    panel().dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape', bubbles: true }))
    await flushPromises()
    expect(panel().classList.contains('garry-panel-fullscreen')).toBe(false)
  })

  it('uses one persistent launcher to list every chat and create new ones', async () => {
    await mountAssistant()
    await click(document.body, 'Open Garry chat menu')
    expect(document.querySelector('[role="menu"]')?.textContent).toContain('Chat 1')
    await click(document.querySelector('[role="menu"]')!, 'New Garry chat')
    expect(document.querySelector('[role="menu"]')).toBeNull()
    expect(document.querySelectorAll('.garry-launcher')).toHaveLength(1)
    expect(document.querySelector('.garry-window-tabs')).toBeNull()
    await click(panel(), 'Minimize Garry')
    await click(document.body, 'Open Garry chat menu')
    const menu = document.querySelector('[role="menu"]')!
    expect(menu.textContent).toContain('Chat 1Minimized')
    expect(menu.textContent).toContain('Chat 2Open')
    expect(menu.querySelectorAll('[role="menuitem"]')).toHaveLength(3)
    await click(menu, 'Open Garry chat 1')
    expect(panel().style.display).not.toBe('none')
    expect(document.querySelector('[role="menu"]')).toBeNull()
    await selectChat(2)
    expect(Number(panel(2).style.zIndex)).toBeGreaterThan(Number(panel().style.zIndex))
  })

  it('creates parallel windows and minimizes/restores them without replacing their conversations', async () => {
    await mountAssistant()
    await send(panel(), 'First conversation')
    await click(panel(), 'New Garry chat')
    expect(panel(2).textContent).not.toContain('First conversation')
    await send(panel(2), 'Second conversation')
    expect(panel().textContent).not.toContain('Second conversation')
    const second = panel(2)
    await click(second, 'Minimize Garry')
    expect(second.style.display).toBe('none')
    await selectChat(2)
    expect(panel(2)).toBe(second)
    expect(second.style.display).not.toBe('none')
    expect(second.textContent).toContain('Second conversation')
    await click(panel(), 'Clear conversation')
    expect(second.textContent).toContain('Second conversation')
  })

  it('preserves panels and old tool views across feature navigation while switching new requests', async () => {
    const router = await mountAssistant()
    vi.mocked(streamAssistantMessage).mockImplementationOnce(async (_base, _request, handlers) => {
      handlers.onTool({ tool: 'courses_list_courses', isError: false, result: {} })
      handlers.onDelta('Old course answer')
      handlers.onDone('stop')
    })
    await send(panel(), 'Course question')
    const first = panel()
    expect(first.textContent).toContain('Original courses presentation')
    await router.push('/games')
    await wrapper!.setProps({ featureKey: 'games', assistant: games })
    await send(panel(), 'Game question')
    expect(panel()).toBe(first)
    expect(first.textContent).toContain('Old course answer')
    expect(first.textContent).toContain('Original courses presentation')
    expect(first.textContent).not.toContain('Wrong games presentation')
    expect(vi.mocked(streamAssistantMessage).mock.calls[1]?.[0]).toBe('/games/api')
    expect(vi.mocked(streamAssistantMessage).mock.calls[1]?.[1].context)
      .toEqual({ routeName: 'games' })
  })

  it('keeps all windows available on non-feature pages but disables sending to a stale feature', async () => {
    await mountAssistant()
    await send(panel(), 'Saved question')
    await wrapper!.setProps({ featureKey: undefined, assistant: undefined })
    expect(panel().textContent).toContain('Saved question')
    expect(panel().querySelector('textarea')?.disabled).toBe(true)
    expect(panel().textContent).toContain('Open a feature to continue chatting')
    await wrapper!.setProps({ featureKey: 'games', assistant: games })
    expect(panel().querySelector('textarea')?.disabled).toBe(false)
  })

  it('closes only the selected window and removes its persisted transcript', async () => {
    await mountAssistant()
    await send(panel(), 'Saved question')
    await click(panel(), 'New Garry chat')
    expect(sessionStorage.getItem(garryStorageKey(1, 'main'))).not.toBeNull()
    await click(panel(), 'Close Garry chat')
    expect(document.querySelector('[aria-label="Garry chat 1"]')).toBeNull()
    expect(panel(2)).toBeDefined()
    expect(sessionStorage.getItem(garryStorageKey(1, 'main'))).toBeNull()
  })

  it('restores independent saved windows on reload', async () => {
    await mountAssistant()
    await send(panel(), 'First saved conversation')
    await click(panel(), 'New Garry chat')
    await send(panel(2), 'Second saved conversation')
    wrapper!.unmount()
    await mountAssistant()
    expect(panel().textContent).toContain('First saved conversation')
    await selectChat(2)
    expect(panel(2).textContent).toContain('Second saved conversation')
    expect(panel(2).textContent).not.toContain('First saved conversation')
  })
})
