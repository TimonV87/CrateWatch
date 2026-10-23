<script setup lang="ts">
import { computed, ref } from 'vue'
import { useAuthStore } from '../stores/auth'
import { setReleaseState, type ReleaseResponse, type ReleaseState } from '../lib/artists'
import { API_BASE } from '../lib/api'

const props = defineProps<{ release: ReleaseResponse }>()
const emit = defineEmits<{ change: [state: ReleaseState]; error: [] }>()

const auth = useAuthStore()
const state = ref<ReleaseState>(props.release.state)
const coverFailed = ref(false)

// Clicking the button of the current state clears it; WISHLIST/OWNED exclude each other server-side.
async function toggle(target: 'WISHLIST' | 'OWNED') {
  const next: ReleaseState = state.value === target ? 'NONE' : target
  try {
    await setReleaseState(props.release.id, next, auth.token!)
    state.value = next
    emit('change', next)
  } catch {
    emit('error')
  }
}

// Year when known, otherwise the release type, so the second line is never empty.
const meta = computed(
  () =>
    props.release.firstReleaseDate?.slice(0, 4) ||
    props.release.releaseType.charAt(0) + props.release.releaseType.slice(1).toLowerCase().replaceAll('_', ' '),
)
</script>

<template>
  <div class="release-card">
    <div class="release-card__cover">
      <v-img
        v-if="release.coverArtUrl && !coverFailed"
        :src="API_BASE + release.coverArtUrl"
        aspect-ratio="1"
        cover
        @error="coverFailed = true"
      />
      <div v-else class="media-placeholder">
        <v-icon icon="mdi-album" size="48" />
      </div>
      <div class="release-card__shade" />
      <v-btn
        icon
        size="small"
        variant="flat"
        class="action action--left"
        :class="{ 'action--active': state === 'OWNED' }"
        :color="state === 'OWNED' ? 'primary' : undefined"
        title="Collection"
        aria-label="Collection"
        :aria-pressed="state === 'OWNED'"
        @click="toggle('OWNED')"
      >
        <svg viewBox="0 0 24 24" width="22" height="22" aria-hidden="true">
          <path fill="currentColor" d="M12.061 5c.697 0 1.265.576 1.265 1.284v13.431c0 .708-.568 1.285-1.265 1.285-.697 0-1.264-.577-1.264-1.285V6.284c0-.708.567-1.284 1.264-1.284m3.337 0c.697 0 1.265.576 1.265 1.284v13.431c0 .708-.568 1.285-1.265 1.285-.697 0-1.264-.577-1.264-1.285V6.284c0-.708.567-1.284 1.264-1.284m3.339 0c.697 0 1.264.576 1.264 1.284v13.431c0 .708-.567 1.285-1.264 1.285-.697 0-1.264-.577-1.264-1.285V6.284c0-.708.567-1.284 1.264-1.284M7.532 6.244c.235-.668.958-1.013 1.615-.775.657.239.997.974.762 1.641L5.452 19.755c-.187.524-.672.849-1.19.849l-.107-.004c-.107-.009-.214-.032-.318-.071-.657-.239-.997-.974-.762-1.641z" />
        </svg>
      </v-btn>
      <v-btn
        icon="mdi-heart"
        size="small"
        variant="flat"
        class="action action--right"
        :class="{ 'action--active': state === 'WISHLIST' }"
        :color="state === 'WISHLIST' ? 'love' : undefined"
        title="Wishlist"
        aria-label="Wishlist"
        :aria-pressed="state === 'WISHLIST'"
        @click="toggle('WISHLIST')"
      />
    </div>
    <div class="media-title mt-2" :title="release.title">{{ release.title }}</div>
    <div class="media-meta">{{ meta }}</div>
  </div>
</template>

<style scoped>
.release-card {
  min-width: 0;
}
.release-card__cover {
  position: relative;
  overflow: hidden;
  border-radius: 10px;
  box-shadow: 0 4px 14px rgba(0, 0, 0, 0.25);
}
.release-card__shade {
  position: absolute;
  inset: auto 0 0 0;
  height: 40%;
  background: linear-gradient(to top, rgba(0, 0, 0, 0.6), transparent);
  opacity: 0;
  transition: opacity 0.15s;
  pointer-events: none;
}
.action {
  position: absolute;
  bottom: 8px;
  opacity: 0;
  transition: opacity 0.15s;
  background-color: rgba(0, 0, 0, 0.55);
  color: #fff;
  backdrop-filter: blur(8px);
}
.action--left {
  left: 8px;
}
.action--right {
  right: 8px;
}
.release-card__cover:hover .release-card__shade,
.release-card__cover:focus-within .release-card__shade {
  opacity: 1;
}
.release-card__cover:hover .action,
.release-card__cover:focus-within .action,
.action--active {
  opacity: 1;
}
/* No hover on touch screens, so the buttons stay visible. */
@media (hover: none) {
  .release-card__shade,
  .action {
    opacity: 1;
  }
}
</style>
