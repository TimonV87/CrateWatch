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
    <v-btn variant="text" prepend-icon="mdi-arrow-left" to="/" class="mb-4">All artists</v-btn>

    <v-alert v-if="error" type="error" density="compact" class="mb-4">{{ error }}</v-alert>

    <v-progress-linear v-if="loading" indeterminate class="mb-4" />

    <template v-else-if="artist">
      <h1 class="text-h5 mb-6">{{ artist.name }}</h1>

      <section v-for="section in sections" :key="section.key" class="mb-8">
        <h2 class="text-subtitle-1 mb-2">{{ section.label }}</h2>
        <v-row>
          <v-col v-for="release in section.releases" :key="release.id" cols="6" sm="4" md="3" lg="2">
            <ReleaseCard :release="release" @change="error = null" @error="error = 'Could not update this album.'" />
          </v-col>
        </v-row>
      </section>

      <v-alert v-if="sections.length === 0" type="info" variant="tonal">
        No releases found for this artist yet.
      </v-alert>
    </template>
  </v-container>
</template>
