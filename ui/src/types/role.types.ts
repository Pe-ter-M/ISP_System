export interface Role {
  id: number
  name: string
  isSystemRole: boolean
  description: string | null
}

export interface RolePermission {
  id: number
  code: string
  group: string
  description: string
}
