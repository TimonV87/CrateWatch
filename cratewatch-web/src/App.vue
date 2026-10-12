<script setup lang="ts">
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from './stores/auth'
import AppLogo from './components/AppLogo.vue'

const auth = useAuthStore()
const router = useRouter()
const search = ref('')

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
        <AppLogo class="ml-2" />
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

    <v-main>
      <RouterView />
    </v-main>
  </v-app>
</template>

<style scoped>
.search-field {
  flex: 1 1 auto;
}
</style>
