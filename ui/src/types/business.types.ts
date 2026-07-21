export interface PlanResponse {
  id: number
  name: string
  description: string | null
  priceCents: number
  bandwidthDownKbps: number | null
  bandwidthUpKbps: number | null
  isActive: boolean
}

export interface SubscriptionResponse {
  id: number
  customerId: number
  username: string
  status: string
  planName: string
  currentPeriodEnd: string
}

export interface SettingResponse {
  key: string
  value: string
  description: string | null
  updatedAt: string
}

export interface CreateSettingRequest {
  key: string
  value: string
  description?: string | null
}

export interface RoleResponse {
  id: number
  name: string
  isSystemRole: boolean
  description: string | null
}

export interface PermissionResponse {
  id: number
  code: string
  group: string
  description: string
}
