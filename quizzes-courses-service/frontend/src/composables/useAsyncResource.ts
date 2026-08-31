import { onScopeDispose, readonly, ref } from 'vue'

export function isAbortError(cause: unknown) {
  return cause instanceof DOMException && cause.name === 'AbortError'
}

export function errorMessage(cause: unknown, fallback: string) {
  return cause instanceof Error ? cause.message : fallback
}

export function useAsyncResource<T>(loader: (signal: AbortSignal) => Promise<T>) {
  const data = ref<T | null>(null)
  const loading = ref(false)
  const error = ref<string | null>(null)
  let controller: AbortController | null = null

  async function load() {
    controller?.abort()
    controller = new AbortController()
    const requestController = controller
    loading.value = true
    error.value = null
    try {
      data.value = await loader(requestController.signal)
      return data.value
    } catch (cause) {
      if (!isAbortError(cause)) {
        error.value = errorMessage(cause, 'Unable to load this content.')
      }
      return null
    } finally {
      if (controller === requestController && !requestController.signal.aborted) {
        loading.value = false
      }
    }
  }

  function cancel() {
    controller?.abort()
    loading.value = false
  }

  onScopeDispose(cancel)

  return {
    data: readonly(data),
    loading: readonly(loading),
    error: readonly(error),
    load,
    retry: load,
    cancel,
  }
}
