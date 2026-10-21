<script setup lang="ts">
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from './stores/auth'
import AppLogo from './components/AppLogo.vue'

const auth = useAuthStore()
const router = useRouter()
const search = ref('')
const drawer = ref(false)

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
    <v-app-bar v-if="auth.isAuthenticated" flat>
      <template #prepend>
        <v-app-bar-nav-icon aria-label="Menu" @click="drawer = !drawer" />
        <router-link to="/" class="logo-link ml-2" aria-label="Home">
          <AppLogo />
        </router-link>
      </template>

      <v-spacer />

      <v-text-field
        v-model="search"
        class="search-field"
        max-width="360"
        density="compact"
        variant="solo-filled"
        flat
        hide-details
        single-line
        prepend-inner-icon="mdi-magnify"
        placeholder="Search artists..."
        @keydown.enter="onSearch"
      />

      <v-spacer />

      <v-btn variant="text" @click="onLogout">Log out</v-btn>
    </v-app-bar>

    <v-navigation-drawer v-if="auth.isAuthenticated" v-model="drawer" temporary>
      <v-list nav @click="drawer = false">
        <v-list-item to="/" prepend-icon="mdi-account-music" title="Artists" />
        <v-list-item to="/collection" prepend-icon="mdi-bookshelf" title="Collection" />
        <v-list-item to="/wishlist" prepend-icon="mdi-heart" title="Wishlist" />
      </v-list>
    </v-navigation-drawer>

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
</style>

<style>
/* Plain links only; Vuetify components rendered as <a> (v-btn, v-card, v-list-item) keep their own styling. */
a:not([class*='v-']) {
  color: rgb(var(--v-theme-info));
  text-decoration: none;
}
</style>
