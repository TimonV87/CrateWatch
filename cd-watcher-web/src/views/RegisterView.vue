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
    await auth.register(username.value, password.value)
    router.push('/')
  } catch (e) {
    error.value = e instanceof ApiError && e.status === 409
      ? 'That username is already taken.'
      : 'Registration failed.'
  } finally {
    submitting.value = false
  }
}
</script>

<template>
  <div class="auth-screen">
    <h1>Register</h1>
    <form @submit.prevent="onSubmit">
      <label>
        Username
        <input v-model="username" type="text" autocomplete="username" required />
      </label>
      <label>
        Password
        <input v-model="password" type="password" autocomplete="new-password" minlength="8" required />
      </label>
      <p v-if="error" class="error">{{ error }}</p>
      <button type="submit" :disabled="submitting">Register</button>
    </form>
    <p>
      Already have an account? <RouterLink to="/login">Log in</RouterLink>
    </p>
  </div>
</template>

<style scoped>
.auth-screen {
  max-width: 320px;
  margin: 4rem auto;
}
form {
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
}
.error {
  color: #c0392b;
}
</style>
