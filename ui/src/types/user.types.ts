export interface UserDetail {
  id: number
  email: string
  fullName: string
  phone: string | null
  roleId: number
  roleName: string
  isActive: boolean
  createdAt: string
}

export interface PaginatedUsers {
  items: UserDetail[]
  totalCount: number
  page: number
  pageSize: number
  totalPages: number
}
