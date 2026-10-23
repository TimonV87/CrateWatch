<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useAuthStore } from '../stores/auth'
import { listArtists, type ArtistResponse } from '../lib/artists'
import { API_BASE } from '../lib/api'
import MediaGridSkeleton from '../components/MediaGridSkeleton.vue'
import EmptyState from '../components/EmptyState.vue'

const auth = useAuthStore()
const artists = ref<ArtistResponse[]>([])
const loading = ref(true)
const error = ref<string | null>(null)
const failedCoverIds = ref(new Set<string>())

onMounted(async () => {
  try {
    artists.value = await listArtists(auth.token!)
  } catch {
    error.value = 'Could not load your artists.'
  } finally {
    loading.value = false
  }
})

function markCoverFailed(id: string) {
  failedCoverIds.value.add(id)
}
</script>

<template>
  <v-container>
    <h1 class="page-title mb-6">Artists</h1>

    <v-alert v-if="error" type="error" density="compact" class="mb-4">{{ error }}</v-alert>

    <MediaGridSkeleton v-if="loading" round />

    <EmptyState v-else-if="artists.length === 0" icon="mdi-account-music" title="No artists yet">
      Use the search bar above to find and add one.
    </EmptyState>

    <div v-else class="media-grid">
      <router-link v-for="artist in artists" :key="artist.id" :to="`/artists/${artist.id}`" class="artist-card">
        <div class="artist-card__photo">
          <v-img
            v-if="artist.coverArtUrl && !failedCoverIds.has(artist.id)"
            :src="API_BASE + artist.coverArtUrl"
            aspect-ratio="1"
            cover
            @error="markCoverFailed(artist.id)"
          />
          <div v-else class="media-placeholder">
            <v-icon icon="mdi-account-music" size="48" />
          </div>
        </div>
        <div class="media-title text-center mt-3">{{ artist.name }}</div>
      </router-link>
    </div>
  </v-container>
</template>

<style scoped>
a.artist-card {
  min-width: 0;
  color: inherit;
}
.artist-card__photo {
  overflow: hidden;
  border-radius: 50%;
  box-shadow: 0 4px 14px rgba(0, 0, 0, 0.25);
}
</style>
