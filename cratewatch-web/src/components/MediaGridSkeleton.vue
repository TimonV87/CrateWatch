<script setup lang="ts">
defineProps<{ round?: boolean }>()
</script>

<template>
  <div class="media-grid" aria-hidden="true">
    <div v-for="n in 12" :key="n">
      <div class="skeleton" :class="{ 'skeleton--round': round }" />
      <div class="skeleton skeleton--line mt-3" />
    </div>
  </div>
</template>

<style scoped>
.skeleton {
  aspect-ratio: 1;
  border-radius: 10px;
  background-color: rgba(var(--v-theme-on-surface), 0.08);
  animation: pulse 1.4s ease-in-out infinite;
}
.skeleton--round {
  border-radius: 50%;
}
.skeleton--line {
  aspect-ratio: auto;
  height: 0.9rem;
  width: 70%;
  border-radius: 4px;
}
@keyframes pulse {
  50% {
    opacity: 0.5;
  }
}
</style>
