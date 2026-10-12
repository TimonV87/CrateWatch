<script setup lang="ts">
</script>

<template>
  <div class="app-logo">
    <svg viewBox="0 0 48 48" width="32" height="32" aria-hidden="true">
      <circle cx="24" cy="24" r="22" fill="url(#disc-gradient)" />
      <circle cx="24" cy="24" r="21" fill="none" stroke="rgba(255,255,255,0.25)" stroke-width="1" />
      <circle cx="24" cy="24" r="7" fill="#121212" />
      <circle cx="24" cy="24" r="2.4" fill="rgba(255,255,255,0.35)" />
      <path
        d="M24 2 A22 22 0 0 1 46 24"
        fill="none"
        stroke="rgba(255,255,255,0.45)"
        stroke-width="1.5"
        stroke-linecap="round"
      />
      <defs>
        <linearGradient id="disc-gradient" x1="0" y1="0" x2="1" y2="1">
          <stop offset="0" stop-color="#7C4DFF" />
          <stop offset="1" stop-color="#18FFFF" />
        </linearGradient>
      </defs>
    </svg>
    <span class="app-logo__text">
      CD<span class="app-logo__accent">Watcher</span>
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
