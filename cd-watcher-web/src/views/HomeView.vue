<script setup lang="ts">
import { useRouter } from 'vue-router'
import { useAuthStore } from '../stores/auth'

const auth = useAuthStore()
const router = useRouter()

function onLogout() {
  auth.logout()
  router.push('/login')
}
</script>

<template>
  <div class="home">
    <p>Logged in as {{ auth.username }}</p>
    <button @click="onLogout">Log out</button>
  </div>
</template>

<style scoped>
.home {
  max-width: 320px;
  margin: 4rem auto;
}
</style>
