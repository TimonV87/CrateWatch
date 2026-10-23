<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { useAuthStore } from '../stores/auth'
import { createShop, deleteShop, listShops, type ShopResponse } from '../lib/shops'
import { ApiError } from '../lib/api'
import EmptyState from '../components/EmptyState.vue'

const auth = useAuthStore()
const shops = ref<ShopResponse[]>([])
const loading = ref(true)
const error = ref<string | null>(null)
const name = ref('')
const searchUrlTemplate = ref('')
const exampleArtist = ref('')
const exampleTitle = ref('')
const adding = ref(false)

onMounted(async () => {
  try {
    shops.value = await listShops(auth.token!)
  } catch {
    error.value = 'Could not load your stores.'
  } finally {
    loading.value = false
  }
})

async function onAdd() {
  error.value = null
  adding.value = true
  try {
    const shop = await createShop(name.value, searchUrlTemplate.value, exampleArtist.value, exampleTitle.value, auth.token!)
    shops.value = [...shops.value, shop].sort((a, b) => a.name.localeCompare(b.name))
    name.value = ''
    searchUrlTemplate.value = ''
    exampleArtist.value = ''
    exampleTitle.value = ''
  } catch (e) {
    error.value = (e instanceof ApiError && (e.body as { detail?: string } | undefined)?.detail) || 'Could not add this store.'
  } finally {
    adding.value = false
  }
}

async function onDelete(shop: ShopResponse) {
  error.value = null
  try {
    await deleteShop(shop.id, auth.token!)
    shops.value = shops.value.filter((s) => s.id !== shop.id)
  } catch {
    error.value = 'Could not remove this store.'
  }
}
</script>

<template>
  <v-container max-width="800">
    <h1 class="page-title mb-6">Add stores</h1>

    <v-alert v-if="error" type="error" density="compact" class="mb-4">{{ error }}</v-alert>

    <v-card flat class="pa-4 mb-8">
      <v-form @submit.prevent="onAdd">
        <v-text-field v-model="name" label="Store name" maxlength="100" class="mb-2" required />
        <v-text-field
          v-model="searchUrlTemplate"
          label="Search address"
          placeholder="https://www.example.com/search?q={query}+cd"
          hint="Open the store, search for something, and copy the address. Replace the search words with {query}."
          persistent-hint
          maxlength="1000"
          class="mb-4"
          required
        />
        <v-text-field v-model="exampleArtist" label="Example artist" maxlength="200" class="mb-2" required />
        <v-text-field
          v-model="exampleTitle"
          label="Example album"
          hint="An album this store sells. We search for it to learn how the store's answer is laid out."
          persistent-hint
          maxlength="200"
          class="mb-4"
          required
        />
        <v-btn
          type="submit"
          color="primary"
          :loading="adding"
          :disabled="!name.trim() || !searchUrlTemplate.trim() || !exampleArtist.trim() || !exampleTitle.trim()"
        >
          Add store
        </v-btn>
      </v-form>
    </v-card>

    <v-progress-linear v-if="loading" indeterminate />

    <EmptyState v-else-if="shops.length === 0" icon="mdi-storefront-outline" title="No stores yet">
      Add a store above, then scan from the Availability page.
    </EmptyState>

    <v-list v-else lines="two" class="bg-transparent">
      <v-list-item v-for="shop in shops" :key="shop.id" :title="shop.name" :subtitle="shop.searchUrlTemplate">
        <template #prepend>
          <v-icon icon="mdi-storefront" />
        </template>
        <template #append>
          <v-btn
            icon="mdi-delete-outline"
            variant="text"
            :aria-label="`Remove ${shop.name}`"
            @click="onDelete(shop)"
          />
        </template>
      </v-list-item>
    </v-list>
  </v-container>
</template>
