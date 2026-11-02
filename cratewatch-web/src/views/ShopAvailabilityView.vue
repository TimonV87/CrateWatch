<script setup lang="ts">
import { computed, onBeforeUnmount, onMounted, ref } from 'vue'
import { useAuthStore } from '../stores/auth'
import { getScanStatus, listAvailability, startScan, type AvailableRelease, type ScanResult, type ScanStatus } from '../lib/shops'
import { ApiError, API_BASE } from '../lib/api'
import MediaGridSkeleton from '../components/MediaGridSkeleton.vue'
import EmptyState from '../components/EmptyState.vue'

const auth = useAuthStore()
const items = ref<AvailableRelease[]>([])
const loading = ref(true)
const scanning = ref(false)
const error = ref<string | null>(null)
const result = ref<ScanResult | null>(null)
const progress = ref<ScanStatus | null>(null)
let pollTimer: number | undefined

const lastChecked = computed(() => {
  const times = items.value.flatMap((item) => item.offers.map((offer) => offer.checkedAt))
  return times.length ? new Date(times.sort().at(-1)!).toLocaleString() : null
})

async function load() {
  items.value = await listAvailability(auth.token!)
}

onMounted(async () => {
  try {
    await load()
    // A scan started earlier (before a page reload) is still running on the server: pick it up again.
    const status = await getScanStatus(auth.token!)
    if (status.running) {
      scanning.value = true
      await poll()
    }
  } catch {
    error.value = 'Could not load the availability.'
  } finally {
    loading.value = false
  }
})

// The scan can take minutes, longer than a single request may last, so it runs on the server and we ask for progress.
async function poll() {
  try {
    const status = await getScanStatus(auth.token!)
    progress.value = status
    if (status.running) {
      pollTimer = window.setTimeout(poll, 2000)
      return
    }
    scanning.value = false
    if (status.failed) {
      error.value = 'The scan failed.'
    } else {
      result.value = status.result
      await load()
    }
  } catch {
    scanning.value = false
    error.value = 'Lost contact with the server. Reload the page to see the result.'
  }
}

async function onScan() {
  error.value = null
  result.value = null
  scanning.value = true
  try {
    progress.value = await startScan(auth.token!)
  } catch (e) {
    // 409 means a scan is already running (maybe started in another tab): just follow it.
    if (!(e instanceof ApiError && e.status === 409)) {
      scanning.value = false
      error.value = 'The scan failed.'
      return
    }
  }
  await poll()
}

onBeforeUnmount(() => window.clearTimeout(pollTimer))
</script>

<template>
  <v-container max-width="900">
    <div class="d-flex align-center flex-wrap ga-4 mb-6">
      <div class="flex-grow-1">
        <h1 class="page-title">Availability</h1>
        <div v-if="scanning && progress?.total" class="text-body-2 text-medium-emphasis">
          Checked {{ progress.done }} of {{ progress.total }} ...
        </div>
        <div v-else-if="lastChecked" class="text-body-2 text-medium-emphasis">Last checked {{ lastChecked }}</div>
      </div>
      <v-btn color="primary" prepend-icon="mdi-radar" :loading="scanning" @click="onScan">Scan wishlist</v-btn>
    </div>

    <v-alert v-if="error" type="error" density="compact" class="mb-4">{{ error }}</v-alert>
    <v-alert v-if="result" type="info" variant="tonal" density="compact" class="mb-4">
      Found {{ result.found }} {{ result.found === 1 ? 'match' : 'matches' }}.
      <template v-if="result.failed > 0">
        {{ result.failed }} {{ result.failed === 1 ? 'check' : 'checks' }} failed; their old results were kept.
      </template>
      <template v-if="result.checked + result.failed === 0">Add a store and put albums on your wishlist first.</template>
    </v-alert>

    <MediaGridSkeleton v-if="loading" />

    <EmptyState v-else-if="items.length === 0" icon="mdi-store-search-outline" title="Nothing found yet">
      Scan to check your wishlist in your stores.
    </EmptyState>

    <div v-else class="d-flex flex-column ga-4">
      <v-card v-for="item in items" :key="item.release.id" flat class="d-flex pa-3 ga-4">
        <div class="cover">
          <v-img
            v-if="item.release.coverArtUrl"
            :src="API_BASE + item.release.coverArtUrl"
            aspect-ratio="1"
            cover
          />
          <div v-else class="media-placeholder">
            <v-icon icon="mdi-album" />
          </div>
        </div>
        <div class="min-width-0">
          <div class="media-title">{{ item.release.title }}</div>
          <div class="media-meta mb-2">
            <router-link :to="`/artists/${item.release.artistId}`" class="plain-link">{{ item.release.artistName }}</router-link>
          </div>
          <div class="d-flex flex-wrap ga-2">
            <v-chip
              v-for="offer in item.offers"
              :key="offer.shopId"
              :href="offer.url"
              target="_blank"
              rel="noopener noreferrer"
              color="primary"
              variant="tonal"
              append-icon="mdi-open-in-new"
            >
              {{ offer.shopName }}
            </v-chip>
          </div>
        </div>
      </v-card>
    </div>
  </v-container>
</template>

<style scoped>
.cover {
  flex-shrink: 0;
  width: 72px;
  height: 72px;
  overflow: hidden;
  border-radius: 8px;
}
.min-width-0 {
  min-width: 0;
}
a.plain-link {
  color: inherit;
}
</style>
