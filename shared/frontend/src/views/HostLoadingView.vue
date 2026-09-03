<template>
  <section class="host-state" role="status" aria-live="polite" aria-labelledby="host-loading-title">
    <v-progress-circular color="primary" indeterminate :size="42" :width="4" />
    <div>
      <p class="host-state__eyebrow">LanguageWise</p>
      <h1 id="host-loading-title">Preparing your workspace</h1>
      <p>Checking your session and loading the shared experience.</p>
    </div>
  </section>
</template>