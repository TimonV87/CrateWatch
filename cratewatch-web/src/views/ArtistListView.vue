<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useAuthStore } from '../stores/auth'
import { listArtists, type ArtistResponse } from '../lib/artists'
import { API_BASE } from '../lib/api'

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
    <v-alert v-if="error" type="error" density="compact" class="mb-4">{{ error }}</v-alert>

    <v-progress-linear v-if="loading" indeterminate class="mb-4" />

    <template v-else-if="artists.length === 0">
      <v-alert type="info" variant="tonal">
        No artists yet. Use the search bar above to find and add one.
      </v-alert>
    </template>

    <v-row v-else>
      <v-col v-for="artist in artists" :key="artist.id" cols="6" sm="4" md="3" lg="2">
        <v-card :to="`/artists/${artist.id}`">
          <v-img
            v-if="artist.coverArtUrl && !failedCoverIds.has(artist.id)"
            :src="API_BASE + artist.coverArtUrl"
            aspect-ratio="1"
            cover
            @error="markCoverFailed(artist.id)"
          />
          <div v-else class="artist-placeholder">
            <v-icon icon="mdi-account-music" size="48" />
          </div>
          <v-card-text>
            <div class="text-body-2 text-center">{{ artist.name }}</div>
          </v-card-text>
        </v-card>
      </v-col>
    </v-row>
  </v-container>
</template>

<style scoped>
.artist-placeholder {
  aspect-ratio: 1;
  display: flex;
  align-items: center;
  justify-content: center;
  background-color: rgba(255, 255, 255, 0.05);
}
</style>
