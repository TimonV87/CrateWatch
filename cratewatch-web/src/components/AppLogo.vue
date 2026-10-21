<script setup lang="ts">
</script>

<template>
  <div class="app-logo">
    <img src="/favicon.svg" width="36" height="36" alt="" />
    <span class="app-logo__text">
      Crate<span class="app-logo__accent">Watch</span>
    </span>
  </div>
</template>

<style scoped>
.app-logo {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  font-weight: 600;
  font-size: 1.25rem;
  letter-spacing: 0.02em;
  white-space: nowrap;
}
.app-logo__accent {
  font-weight: 400;
  opacity: 0.75;
}
</style>
