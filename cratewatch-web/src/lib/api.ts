import { useAuthStore } from '../stores/auth'
import router from '../router'

export const API_BASE = import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:8080/api/v1'

export class ApiError extends Error {
  status: number
  body: unknown

  constructor(status: number, message: string, body?: unknown) {
    super(message)
    this.status = status
    this.body = body
  }
}

async function handleResponse<T>(response: Response, path: string, authenticated: boolean): Promise<T> {
  if (!response.ok) {
    if (authenticated && response.status === 401) {
      useAuthStore().logout()
      router.push('/login')
    }

    const text = await response.text()
    let body: unknown
    try {
      body = text ? JSON.parse(text) : undefined
    } catch {
      body = undefined
    }
    throw new ApiError(response.status, text || `Request to ${path} failed with ${response.status}`, body)
  }

  if (response.status === 204) {
    return undefined as unknown as T
  }

  return (await response.json()) as T
}

async function sendJson<T>(method: 'POST' | 'PUT', path: string, body: unknown, token?: string): Promise<T> {
  const headers: Record<string, string> = { 'Content-Type': 'application/json' }
  if (token) {
    headers.Authorization = `Bearer ${token}`
  }

  const response = await fetch(`${API_BASE}${path}`, {
    method,
    headers,
    body: JSON.stringify(body),
  })

  return handleResponse<T>(response, path, Boolean(token))
}

export function postJson<T>(path: string, body: unknown, token?: string): Promise<T> {
  return sendJson<T>('POST', path, body, token)
}

export function putJson<T>(path: string, body: unknown, token?: string): Promise<T> {
  return sendJson<T>('PUT', path, body, token)
}

export async function getJson<T>(path: string, token?: string): Promise<T> {
  const headers: Record<string, string> = {}
  if (token) {
    headers.Authorization = `Bearer ${token}`
  }

  const response = await fetch(`${API_BASE}${path}`, { headers })

  return handleResponse<T>(response, path, Boolean(token))
}
