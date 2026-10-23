<script setup lang="ts">
import { computed, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useDisplay } from 'vuetify'
import { useAuthStore } from './stores/auth'
import AppLogo from './components/AppLogo.vue'

const auth = useAuthStore()
const router = useRouter()
const route = useRoute()
const { mdAndUp } = useDisplay()
const search = ref('')

const NAV = [
  { key: 'artists', to: '/', label: 'Artists', short: 'Artists', icon: 'mdi-account-music' },
  { key: 'collection', to: '/collection', label: 'Collection', short: 'Collection', icon: 'mdi-bookshelf' },
  { key: 'wishlist', to: '/wishlist', label: 'Wishlist', short: 'Wishlist', icon: 'mdi-heart' },
]
// Desktop shows these two under a "Shops" menu; the mobile bottom bar lists them directly.
const SHOP_NAV = [
  { key: 'shops', to: '/shops', label: 'Add stores', short: 'Stores', icon: 'mdi-store-plus' },
  { key: 'availability', to: '/shops/availability', label: 'Availability', short: 'In stock', icon: 'mdi-store-search' },
]
const MOBILE_NAV = [...NAV, ...SHOP_NAV]

// Artist detail and search pages belong to the Artists section.
const section = computed(() => MOBILE_NAV.find((item) => item.to === route.path)?.key ?? 'artists')
const inShops = computed(() => SHOP_NAV.some((item) => item.key === section.value))

function onLogout() {
  auth.logout()
  router.push('/login')
}

function onSearch() {
  if (search.value.trim()) {
    router.push({ path: '/artists/search', query: { q: search.value.trim() } })
  }
}
</script>

<template>
  <v-app>
    <v-app-bar v-if="auth.isAuthenticated" flat class="glass">
      <router-link to="/" class="logo-link ml-4" aria-label="Home">
        <AppLogo />
      </router-link>

      <nav v-if="mdAndUp" class="d-flex ga-1 ml-6" aria-label="Main">
        <v-btn
          v-for="item in NAV"
          :key="item.key"
          :to="item.to"
          variant="text"
          :color="section === item.key ? 'primary' : undefined"
          :prepend-icon="item.icon"
        >
          {{ item.label }}
        </v-btn>

        <v-menu>
          <template #activator="{ props }">
            <v-btn
              v-bind="props"
              variant="text"
              :color="inShops ? 'primary' : undefined"
              prepend-icon="mdi-storefront"
              append-icon="mdi-chevron-down"
            >
              Shops
            </v-btn>
          </template>
          <v-list>
            <v-list-item v-for="item in SHOP_NAV" :key="item.key" :to="item.to" :prepend-icon="item.icon" :title="item.label" />
          </v-list>
        </v-menu>
      </nav>

      <v-spacer v-if="mdAndUp" />

      <v-text-field
        v-model="search"
        class="search-field mx-4"
        max-width="360"
        density="compact"
        hide-details
        single-line
        rounded="pill"
        prepend-inner-icon="mdi-magnify"
        placeholder="Search artists..."
        aria-label="Search artists"
        @keydown.enter="onSearch"
      />

      <v-btn v-if="mdAndUp" variant="text" class="mr-2" @click="onLogout">Log out</v-btn>
      <v-btn v-else icon="mdi-logout" variant="text" aria-label="Log out" @click="onLogout" />
    </v-app-bar>

    <v-bottom-navigation v-if="auth.isAuthenticated && !mdAndUp" :model-value="section" grow color="primary" class="glass">
      <v-btn v-for="item in MOBILE_NAV" :key="item.key" :to="item.to" :value="item.key">
        <v-icon :icon="item.icon" />
        {{ item.short }}
      </v-btn>
    </v-bottom-navigation>

    <v-main>
      <RouterView />
    </v-main>
  </v-app>
</template>

<style scoped>
.logo-link {
  color: inherit;
}
.search-field {
  flex: 1 1 auto;
}
@media (max-width: 599px) {
  .logo-link {
    margin-left: 0.75rem !important;
  }
  .logo-link :deep(.app-logo__text) {
    display: none;
  }
}
</style>

<style>
/* Plain links only; Vuetify components rendered as <a> (v-btn, v-card, v-list-item) keep their own styling. */
a:not([class*='v-']) {
  color: rgb(var(--v-theme-info));
  text-decoration: none;
}
</style>
