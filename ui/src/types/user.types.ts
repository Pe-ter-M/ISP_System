export interface UserResponse {
  id: number
  email: string
  fullName: string
  phone: string | null
  roleId: number
  roleName: string
  isActive: boolean
  createdAt: string
}

export interface CreateUserRequest {
  email: string
  password: string
  fullName: string
  phone: string | null
  roleId: number
}
