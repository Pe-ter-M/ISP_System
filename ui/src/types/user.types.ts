export interface Permission {
  id: number
  code: string
  group: string
  description: string
}

export interface PermissionOverride {
  code: string
  isGranted: boolean
}

export interface UserDetail {
  id: number
  email: string
  fullName: string
  phone: string | null
  roleId: number
  roleName: string
  isActive: boolean
  createdAt: string
  updatedAt?: string
  permissions?: string[]
  permissionOverrides?: PermissionOverride[]
}

export interface PaginatedUsers {
  items: UserDetail[]
  totalCount: number
  page: number
  pageSize: number
  totalPages: number
}
