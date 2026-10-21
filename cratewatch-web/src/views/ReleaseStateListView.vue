<script setup lang="ts">
import { computed, ref, watch } from 'vue'
import { useAuthStore } from '../stores/auth'
import { listReleasesByState, type ReleaseResponse, type ReleaseState } from '../lib/artists'
import ReleaseCard from '../components/ReleaseCard.vue'

const props = defineProps<{ state: 'OWNED' | 'WISHLIST'; title: string }>()

const auth = useAuthStore()
const releases = ref<ReleaseResponse[]>([])
const loading = ref(true)
const error = ref<string | null>(null)

interface ArtistGroup {
  id: string
  name: string
  releases: ReleaseResponse[]
}

// Block key: the first character when it is a letter A-Z (accents stripped) or a digit, '#' for anything else.
function letterOf(name: string) {
  const first = name.normalize('NFD').charAt(0).toUpperCase()
  return /[0-9A-Z]/.test(first) ? first : '#'
}

// Digits first, then A-Z, then '#'.
function blockRank(key: string) {
  return key === '#' ? 2 : /[0-9]/.test(key) ? 0 : 1
}

const letters = computed(() => {
  const artists = new Map<string, ArtistGroup>()
  for (const release of releases.value) {
    const group = artists.get(release.artistId) ?? { id: release.artistId, name: release.artistName ?? '', releases: [] }
    group.releases.push(release)
    artists.set(release.artistId, group)
  }

  const byLetter = new Map<string, ArtistGroup[]>()
  const sorted = [...artists.values()].sort((a, b) => a.name.localeCompare(b.name, undefined, { sensitivity: 'base' }))
  for (const artist of sorted) {
    const letter = letterOf(artist.name)
    byLetter.set(letter, [...(byLetter.get(letter) ?? []), artist])
  }

  return [...byLetter.entries()]
    .sort(([a], [b]) => blockRank(a) - blockRank(b) || a.localeCompare(b))
    .map(([letter, artists]) => ({ letter, artists }))
})

watch(
  () => props.state,
  async (state) => {
    loading.value = true
    error.value = null
    try {
      releases.value = await listReleasesByState(state, auth.token!)
    } catch {
      error.value = 'Could not load this list.'
    } finally {
      loading.value = false
    }
  },
  { immediate: true },
)

// A card that was just moved out of this list disappears from it.
function onChange(release: ReleaseResponse, next: ReleaseState) {
  error.value = null
  if (next !== props.state) {
    releases.value = releases.value.filter((r) => r.id !== release.id)
  }
}
</script>

<template>
  <v-container>
    <h1 class="text-h5 mb-6">{{ title }}</h1>

    <v-alert v-if="error" type="error" density="compact" class="mb-4">{{ error }}</v-alert>

    <v-progress-linear v-if="loading" indeterminate class="mb-4" />

    <v-alert v-else-if="releases.length === 0 && !error" type="info" variant="tonal">
      Nothing here yet. Use the buttons on an album to add it.
    </v-alert>

    <template v-else>
      <div v-for="block in letters" :key="block.letter" class="d-flex mb-8">
        <div class="letter text-h2 font-weight-bold text-medium-emphasis">{{ block.letter }}</div>
        <div class="flex-grow-1">
          <section v-for="artist in block.artists" :key="artist.id" class="mb-6">
            <h2 class="text-subtitle-1 mb-2">
              <router-link :to="`/artists/${artist.id}`">{{ artist.name }}</router-link>
            </h2>
            <v-row>
              <v-col v-for="release in artist.releases" :key="release.id" cols="6" sm="4" md="3" lg="2">
                <ReleaseCard
                  :release="release"
                  @change="(next) => onChange(release, next)"
                  @error="error = 'Could not update this album.'"
                />
              </v-col>
            </v-row>
          </section>
        </div>
      </div>
    </template>
  </v-container>
</template>

<style scoped>
.letter {
  width: 5rem;
  flex-shrink: 0;
  line-height: 1;
}
</style>
