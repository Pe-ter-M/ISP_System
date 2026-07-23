import api from './api'
import type { PaginatedUsers, UserDetail } from '@/types/user.types'

export interface CreateUserPayload {
  email: string
  password: string
  fullName: string
  phone: string | null
  roleId: number
}

export async function getUsers(
  page = 1,
  pageSize = 10,
  search?: string,
  sortBy?: string,
  sortDesc = false,
): Promise<PaginatedUsers> {
  const params: Record<string, string | number | boolean> = { page, pageSize }
  if (search) params.search = search
  if (sortBy) params.sortBy = sortBy
  if (sortDesc) params.sortDesc = true

  const res = await api.get('/users', { params })
  return res.data as PaginatedUsers
}

export async function getUserById(id: number): Promise<UserDetail> {
  const res = await api.get(`/users/${id}`)
  return res.data as UserDetail
}

export async function createUser(payload: CreateUserPayload): Promise<UserDetail> {
  const res = await api.post('/users', payload)
  return res.data as UserDetail
}
