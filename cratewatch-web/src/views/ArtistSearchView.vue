<script setup lang="ts">
import { ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useAuthStore } from '../stores/auth'
import { searchArtists, createArtist, type ArtistSearchResult } from '../lib/artists'
import { ApiError } from '../lib/api'

const route = useRoute()
const router = useRouter()
const auth = useAuthStore()

const results = ref<ArtistSearchResult[]>([])
const loading = ref(false)
const error = ref<string | null>(null)
const addingMbid = ref<string | null>(null)

async function runSearch(query: string) {
  if (!query) {
    results.value = []
    return
  }
  loading.value = true
  error.value = null
  try {
    results.value = await searchArtists(query, auth.token!)
  } catch {
    error.value = 'Search failed.'
  } finally {
    loading.value = false
  }
}

async function onAdd(result: ArtistSearchResult) {
  addingMbid.value = result.musicbrainzId
  error.value = null
  try {
    const artist = await createArtist(result.name, result.musicbrainzId, auth.token!)
    router.push(`/artists/${artist.id}`)
  } catch (e) {
    if (e instanceof ApiError && e.status === 409) {
      const existing = e.body as { id?: string } | undefined
      if (existing?.id) {
        router.push(`/artists/${existing.id}`)
        return
      }
      error.value = 'You already added this artist.'
    } else {
      error.value = 'Could not add artist.'
    }
  } finally {
    addingMbid.value = null
  }
}

watch(
  () => route.query.q,
  (q) => runSearch(typeof q === 'string' ? q : ''),
  { immediate: true },
)
</script>

<template>
  <v-container>
    <h1 class="text-h5 mb-4">Search results for "{{ route.query.q }}"</h1>

    <v-alert v-if="error" type="error" density="compact" class="mb-4">{{ error }}</v-alert>

    <v-progress-linear v-if="loading" indeterminate class="mb-4" />

    <v-list v-else lines="two">
      <v-list-item
        v-for="result in results"
        :key="result.musicbrainzId"
      >
        <v-list-item-title>{{ result.name }}</v-list-item-title>
        <v-list-item-subtitle>
          {{ [result.disambiguation, result.country].filter(Boolean).join(' · ') }}
        </v-list-item-subtitle>

        <template #append>
          <v-btn
            color="primary"
            :loading="addingMbid === result.musicbrainzId"
            @click="onAdd(result)"
          >
            Add
          </v-btn>
        </template>
      </v-list-item>

      <v-list-item v-if="!loading && results.length === 0">
        <v-list-item-title>No artists found.</v-list-item-title>
      </v-list-item>
    </v-list>
  </v-container>
</template>
