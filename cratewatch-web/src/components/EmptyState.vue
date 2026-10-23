<script setup lang="ts">
defineProps<{ icon: string; title: string }>()
</script>

<template>
  <div class="d-flex flex-column align-center text-center py-12 text-medium-emphasis">
    <v-icon :icon="icon" size="56" class="mb-4" />
    <div class="text-h6 mb-1">{{ title }}</div>
    <div class="text-body-2"><slot /></div>
  </div>
</template>
