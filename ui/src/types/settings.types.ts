export interface SettingItem {
  key: string
  value: string
  description: string | null
  isEncrypted: boolean
  updatedAt: string
}

export interface CreateSettingPayload {
  key: string
  value: string
  description?: string | null
}

export interface UpdateSettingPayload {
  value: string
  description?: string | null
}
