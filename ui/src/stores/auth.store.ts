import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import api from '@/services/api'
import type { LoginResponse } from '@/types/auth.types'

const USER_KEY = 'auth_user'

export const useAuthStore = defineStore('auth', () => {
  const token = ref(localStorage.getItem('auth_token') || '')

  const raw = localStorage.getItem(USER_KEY)
  const user = ref<LoginResponse | null>(raw ? JSON.parse(raw) : null)

  const isAuthenticated = computed(() => !!token.value)
  const userPermissions = computed(() => user.value?.permissions ?? [])
  const userRole = computed(() => user.value?.role ?? '')
  const userName = computed(() => user.value?.fullName ?? '')
  const userEmail = computed(() => user.value?.email ?? '')

  async function login(email: string, password: string) {
    const res = await api.post('/auth/login', { email, password })
    const data = res.data as LoginResponse

    token.value = data.token
    user.value = data

    localStorage.setItem('auth_token', data.token)
    localStorage.setItem(USER_KEY, JSON.stringify(data))
  }

  function logout() {
    token.value = ''
    user.value = null
    localStorage.removeItem('auth_token')
    localStorage.removeItem(USER_KEY)
  }

  /** Verify the stored token is still valid by calling /auth/me.
   *  The Axios interceptor already unwraps ApiResponse.data so we get
   *  { userId, email, fullName, role, permissions } directly. */
  async function restoreSession() {
    const storedToken = localStorage.getItem('auth_token')
    if (!storedToken) {
      token.value = ''
      user.value = null
      return false
    }

    token.value = storedToken

    try {
      const res = await api.get('/auth/me')
      const profile = res.data as { userId: number; email: string; fullName: string; role: string; permissions: string[] }

      user.value = {
        token: storedToken,
        userId: profile.userId,
        email: profile.email,
        fullName: profile.fullName,
        role: profile.role,
        permissions: profile.permissions,
      }

      localStorage.setItem(USER_KEY, JSON.stringify(user.value))
      return true
    } catch {
      logout()
      return false
    }
  }

  return {
    token, user,
    isAuthenticated, userPermissions, userRole, userName, userEmail,
    login, logout, restoreSession,
  }
})
