<script setup lang="ts">
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '../stores/auth'
import { ApiError } from '../lib/api'

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
    error.value = e instanceof ApiError ? 'Invalid username or password.' : 'Login failed.'
  } finally {
    submitting.value = false
  }
}
</script>

<template>
  <v-container class="fill-height" max-width="420">
    <v-responsive class="align-center mx-auto" max-width="420">
      <v-card>
        <v-card-title>Log in</v-card-title>
        <v-card-text>
          <v-form @submit.prevent="onSubmit">
            <v-text-field
              v-model="username"
              label="Username"
              autocomplete="username"
              required
            />
            <v-text-field
              v-model="password"
              label="Password"
              type="password"
              autocomplete="current-password"
              required
            />
            <v-alert v-if="error" type="error" density="compact" class="mb-4">
              {{ error }}
            </v-alert>
            <v-btn type="submit" color="primary" block :loading="submitting">
              Log in
            </v-btn>
          </v-form>
        </v-card-text>
        <v-card-actions>
          <v-spacer />
          <span>No account yet?</span>
          <v-btn variant="text" color="primary" to="/register">Register</v-btn>
        </v-card-actions>
      </v-card>
    </v-responsive>
  </v-container>
</template>
