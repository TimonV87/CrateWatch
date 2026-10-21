<script setup lang="ts">
import { ref } from 'vue'
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
</script>

<template>
  <v-card>
    <v-img
      v-if="release.coverArtUrl && !coverFailed"
      :src="API_BASE + release.coverArtUrl"
      aspect-ratio="1"
      cover
      @error="coverFailed = true"
    />
    <div v-else class="release-placeholder">
      <v-icon icon="mdi-album" size="48" />
    </div>
    <v-card-text>
      <div class="text-body-2">{{ release.title }}</div>
    </v-card-text>
    <v-card-actions class="ga-1">
      <v-btn
        icon
        size="small"
        title="Collection"
        aria-label="Collection"
        :variant="state === 'OWNED' ? 'flat' : 'tonal'"
        :color="state === 'OWNED' ? 'success' : undefined"
        :aria-pressed="state === 'OWNED'"
        @click="toggle('OWNED')"
      >
        <svg viewBox="0 0 24 24" width="24" height="24" aria-hidden="true">
                    <path fill="currentColor" d="M12.061 5c.697 0 1.265.576 1.265 1.284v13.431c0 .708-.568 1.285-1.265 1.285-.697 0-1.264-.577-1.264-1.285V6.284c0-.708.567-1.284 1.264-1.284m3.337 0c.697 0 1.265.576 1.265 1.284v13.431c0 .708-.568 1.285-1.265 1.285-.697 0-1.264-.577-1.264-1.285V6.284c0-.708.567-1.284 1.264-1.284m3.339 0c.697 0 1.264.576 1.264 1.284v13.431c0 .708-.567 1.285-1.264 1.285-.697 0-1.264-.577-1.264-1.285V6.284c0-.708.567-1.284 1.264-1.284M7.532 6.244c.235-.668.958-1.013 1.615-.775.657.239.997.974.762 1.641L5.452 19.755c-.187.524-.672.849-1.19.849l-.107-.004c-.107-.009-.214-.032-.318-.071-.657-.239-.997-.974-.762-1.641z" />
                  </svg>
      </v-btn>
      <v-btn
        icon="mdi-heart"
        size="small"
        title="Wishlist"
        aria-label="Wishlist"
        :variant="state === 'WISHLIST' ? 'flat' : 'tonal'"
        :color="state === 'WISHLIST' ? 'primary' : undefined"
        :aria-pressed="state === 'WISHLIST'"
        @click="toggle('WISHLIST')"
      />
    </v-card-actions>
  </v-card>
</template>

<style scoped>
.release-placeholder {
  aspect-ratio: 1;
  display: flex;
  align-items: center;
  justify-content: center;
  background-color: rgba(255, 255, 255, 0.05);
}
</style>
