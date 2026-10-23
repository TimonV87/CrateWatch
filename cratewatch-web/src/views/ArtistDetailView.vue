<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useRoute } from 'vue-router'
import { useAuthStore } from '../stores/auth'
import {
  getArtist,
  listReleases,
  type ArtistResponse,
  type ReleaseResponse,
} from '../lib/artists'
import ReleaseCard from '../components/ReleaseCard.vue'
import MediaGridSkeleton from '../components/MediaGridSkeleton.vue'
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
const photoFailed = ref(false)

const STUDIO = 'STUDIO_ALBUM'
const studioCount = computed(() => releases.value.filter((r) => r.releaseType === STUDIO).length)
const optionalTypes = TYPE_SECTIONS.filter((section) => section.key !== STUDIO)
const enabledTypes = ref(new Set<string>())
const loadingTypes = ref(new Set<string>())

// Studio albums are always shown; a switched-on type is shown even when empty so the user sees it came back with nothing.
const sections = computed(() =>
  TYPE_SECTIONS
    .filter((section) => section.key === STUDIO || enabledTypes.value.has(section.key))
    .map((section) => ({
      ...section,
      releases: releases.value.filter((r) => r.releaseType === section.key),
    }))
    .filter((section) => section.releases.length > 0 || enabledTypes.value.has(section.key)),
)

async function toggleType(type: string, on: boolean) {
  error.value = null
  if (!on) {
    enabledTypes.value.delete(type)
    return
  }

  loadingTypes.value.add(type)
  try {
    const fetched = await listReleases(route.params.id as string, auth.token!, [type])
    releases.value = [...releases.value.filter((r) => r.releaseType !== type), ...fetched]
    enabledTypes.value.add(type)
  } catch {
    error.value = 'Could not load these releases. Try again in a moment.'
  } finally {
    loadingTypes.value.delete(type)
  }
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
    <v-btn variant="text" prepend-icon="mdi-chevron-left" to="/" class="mb-4 ml-n3">Artists</v-btn>

    <v-alert v-if="error" type="error" density="compact" class="mb-4">{{ error }}</v-alert>

    <MediaGridSkeleton v-if="loading" />

    <template v-else-if="artist">
      <header class="d-flex align-center ga-4 ga-sm-6 mb-6">
        <div class="artist-photo">
          <v-img
            v-if="artist.coverArtUrl && !photoFailed"
            :src="API_BASE + artist.coverArtUrl"
            aspect-ratio="1"
            cover
            @error="photoFailed = true"
          />
          <div v-else class="media-placeholder">
            <v-icon icon="mdi-account-music" size="48" />
          </div>
        </div>
        <div class="min-width-0">
          <h1 class="page-title">{{ artist.name }}</h1>
          <div class="text-body-1 text-medium-emphasis">
            {{ studioCount }} studio {{ studioCount === 1 ? 'album' : 'albums' }}
          </div>
        </div>
      </header>

      <div class="d-flex flex-wrap ga-2 mb-8" role="group" aria-label="Show more release types">
        <v-chip
          v-for="option in optionalTypes"
          :key="option.key"
          size="large"
          :variant="enabledTypes.has(option.key) ? 'flat' : 'tonal'"
          :color="enabledTypes.has(option.key) ? 'primary' : undefined"
          :prepend-icon="loadingTypes.has(option.key) ? 'mdi-loading mdi-spin' : enabledTypes.has(option.key) ? 'mdi-check' : undefined"
          :disabled="loadingTypes.has(option.key)"
          :aria-pressed="enabledTypes.has(option.key)"
          @click="toggleType(option.key, !enabledTypes.has(option.key))"
        >
          {{ option.label }}
        </v-chip>
      </div>

      <section v-for="section in sections" :key="section.key" class="mb-10">
        <h2 class="section-title mb-3">{{ section.label }}</h2>
        <p v-if="section.releases.length === 0" class="text-body-2 text-medium-emphasis">None found.</p>
        <div class="media-grid">
          <ReleaseCard
            v-for="release in section.releases"
            :key="release.id"
            :release="release"
            @change="error = null"
            @error="error = 'Could not update this album.'"
          />
        </div>
      </section>

      <v-alert v-if="sections.length === 0" type="info" variant="tonal">
        No studio albums found for this artist. Use the chips above to look for other releases.
      </v-alert>
    </template>
  </v-container>
</template>

<style scoped>
.artist-photo {
  flex-shrink: 0;
  width: 96px;
  overflow: hidden;
  border-radius: 50%;
  box-shadow: 0 4px 14px rgba(0, 0, 0, 0.25);
}
.min-width-0 {
  min-width: 0;
}
@media (min-width: 600px) {
  .artist-photo {
    width: 160px;
  }
}
</style>
