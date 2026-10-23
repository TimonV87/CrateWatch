<script setup lang="ts">
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '../stores/auth'
import { ApiError } from '../lib/api'
import AppLogo from '../components/AppLogo.vue'

const username = ref('')
const password = ref('')
const error = ref<string | null>(null)
const submitting = ref(false)

const auth = useAuthStore()
const router = useRouter()

async function onSubmit() {
  error.value = null
  submitting.value = true
  try {
    await auth.login(username.value, password.value)
    router.push('/')
  } catch (e) {
    if (e instanceof ApiError && e.status === 429) {
      error.value = 'Too many attempts. Wait a minute and try again.'
    } else {
      error.value = e instanceof ApiError ? 'Invalid username or password.' : 'Login failed.'
    }
  } finally {
    submitting.value = false
  }
}
</script>

<template>
  <v-container class="login fill-height" max-width="none">
    <v-responsive class="align-center mx-auto" max-width="400">
      <div class="d-flex justify-center mb-8 text-h5">
        <AppLogo />
      </div>
      <v-card flat class="pa-4">
        <v-card-title class="page-title">Log in</v-card-title>
        <v-card-text>
          <v-form @submit.prevent="onSubmit">
            <v-text-field
              v-model="username"
              label="Username"
              autocomplete="username"
              class="mb-2"
              required
            />
            <v-text-field
              v-model="password"
              label="Password"
              type="password"
              autocomplete="current-password"
              class="mb-2"
              required
            />
            <v-alert v-if="error" type="error" density="compact" class="mb-4">
              {{ error }}
            </v-alert>
            <v-btn type="submit" color="primary" size="large" block :loading="submitting">
              Log in
            </v-btn>
          </v-form>
        </v-card-text>
      </v-card>
    </v-responsive>
  </v-container>
</template>

<style scoped>
.login {
  background: radial-gradient(ellipse at top, rgba(10, 132, 255, 0.18), transparent 60%);
}
</style>
