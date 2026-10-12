import { defineStore } from 'pinia'
import { postJson } from '../lib/api'

interface LoginResponse {
  access_token: string
  username: string
}

interface RegisterResponse {
  id: string
  username: string
}

interface AuthState {
  token: string | null
  username: string | null
}

export const useAuthStore = defineStore('auth', {
  state: (): AuthState => ({
    token: localStorage.getItem('cratewatch.token'),
    username: localStorage.getItem('cratewatch.username'),
  }),

  getters: {
    isAuthenticated: (state) => state.token !== null,
  },

  actions: {
    async login(username: string, password: string) {
      const response = await postJson<LoginResponse>('/login', { username, password })
      this.token = response.access_token
      this.username = response.username
      localStorage.setItem('cratewatch.token', response.access_token)
      localStorage.setItem('cratewatch.username', response.username)
    },

    async register(username: string, password: string) {
      await postJson<RegisterResponse>('/auth/register', { username, password })
      await this.login(username, password)
    },

    logout() {
      this.token = null
      this.username = null
      localStorage.removeItem('cratewatch.token')
      localStorage.removeItem('cratewatch.username')
    },
  },
})
