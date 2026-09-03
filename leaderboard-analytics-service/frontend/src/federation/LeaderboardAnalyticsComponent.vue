<script setup lang="ts">
interface HostContext {
  readonly user: { id: number; name: string } | null
  navigate(path: string): Promise<void>
  signIn(returnUrl?: string): void
  signOut(): Promise<void>
}

defineProps<{ hostContext?: HostContext }>()
</script>

<template>
  <section class="feature-leaderboard-analytics">
    <RouterView />
  </section>
</template>