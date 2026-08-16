import api from './api'
import type { Role, RolePermission } from '@/types/role.types'
import type { Permission } from '@/types/user.types'

export async function getRoles(): Promise<Role[]> {
  const res = await api.get('/roles')
  return res.data as Role[]
}

export async function createRole(name: string, description: string | null): Promise<Role> {
  const res = await api.post('/roles', { name, description })
  return res.data as Role
}

export async function deleteRole(roleId: number): Promise<void> {
  await api.delete(`/roles/${roleId}`)
}

export async function getRolePermissions(roleId: number): Promise<RolePermission[]> {
  const res = await api.get(`/roles/${roleId}/permissions`)
  return res.data as RolePermission[]
}

export async function setRolePermissions(roleId: number, codes: string[]): Promise<void> {
  await api.put(`/roles/${roleId}/permissions`, { codes })
}

export { type Permission }
