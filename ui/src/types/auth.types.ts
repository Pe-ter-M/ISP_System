export interface LoginRequest {
  email: string
  password: string
}

export interface LoginResponse {
  token: string
  userId: number
  email: string
  fullName: string
  role: string
  permissions: string[]
}

export interface AuthUser {
  userId: number
  email: string
  fullName: string
  role: string
  permissions: string[]
}
