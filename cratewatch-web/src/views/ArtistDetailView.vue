<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useRoute } from 'vue-router'
import { useAuthStore } from '../stores/auth'
import { getArtist, listReleases, type ArtistResponse, type ReleaseResponse } from '../lib/artists'
import { API_BASE } from '../lib/api'

const TYPE_SECTIONS: Array<{ key: string; label: string }> = [
  { key: 'STUDIO_ALBUM', label: 'Studio Albums' },
  { key: 'EP', label: 'EPs' },
  { key: 'COMPILATION', label: 'Compilations' },
  { key: 'LIVE', label: 'Live' },
  { key: 'SOUNDTRACK', label: 'Soundtracks' },
  { key: 'SINGLE', label: 'Singles' },
  { key: 'OTHER', label: 'Other' },
]

const route = useRoute()
const auth = useAuthStore()

const artist = ref<ArtistResponse | null>(null)
const releases = ref<ReleaseResponse[]>([])
const loading = ref(true)
const error = ref<string | null>(null)

const sections = computed(() =>
  TYPE_SECTIONS
    .map((section) => ({
      ...section,
      releases: releases.value.filter((r) => r.releaseType === section.key),
    }))
    .filter((section) => section.releases.length > 0),
)

const failedCoverIds = ref(new Set<string>())

function markCoverFailed(id: string) {
  failedCoverIds.value.add(id)
}

onMounted(async () => {
  const artistId = route.params.id as string
  try {
    [artist.value, releases.value] = await Promise.all([
      getArtist(artistId, auth.token!),
      listReleases(artistId, auth.token!),
    ])
  } catch {
    error.value = 'Could not load this artist.'
  } finally {
    loading.value = false
  }
})
</script>

<template>
  <v-container>
    <v-alert v-if="error" type="error" density="compact" class="mb-4">{{ error }}</v-alert>

    <v-progress-linear v-if="loading" indeterminate class="mb-4" />

    <template v-else-if="artist">
      <h1 class="text-h5 mb-6">{{ artist.name }}</h1>

      <section v-for="section in sections" :key="section.key" class="mb-8">
        <h2 class="text-subtitle-1 mb-2">{{ section.label }}</h2>
        <v-row>
          <v-col v-for="release in section.releases" :key="release.id" cols="6" sm="4" md="3" lg="2">
            <v-card>
              <v-img
                v-if="release.coverArtUrl && !failedCoverIds.has(release.id)"
                :src="API_BASE + release.coverArtUrl"
                aspect-ratio="1"
                cover
                @error="markCoverFailed(release.id)"
              />
              <div v-else class="release-placeholder">
                <v-icon icon="mdi-album" size="48" />
              </div>
              <v-card-text>
                <div class="text-body-2">{{ release.title }}</div>
              </v-card-text>
            </v-card>
          </v-col>
        </v-row>
      </section>

      <v-alert v-if="sections.length === 0" type="info" variant="tonal">
        No releases found for this artist yet.
      </v-alert>
    </template>
  </v-container>
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
