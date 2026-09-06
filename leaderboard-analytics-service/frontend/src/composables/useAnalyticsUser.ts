import { computed, inject, provide, type ComputedRef, type InjectionKey, type Ref } from 'vue'

export type AnalyticsUserIdRef = ComputedRef<number | null>

export const AnalyticsUserIdKey: InjectionKey<AnalyticsUserIdRef> = Symbol('analytics-user-id')

export function provideAnalyticsUserId(source: Ref<number | null | undefined>) {
  provide(AnalyticsUserIdKey, computed(() => source.value ?? null))
}

export function useAnalyticsUserId(): AnalyticsUserIdRef {
  return inject(AnalyticsUserIdKey, computed(() => null))
}
