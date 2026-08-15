import api from './api'
import type { SettingItem, CreateSettingPayload, UpdateSettingPayload } from '@/types/settings.types'

/** Admin: list all settings (requires settings.view) */
export async function getSettings(): Promise<SettingItem[]> {
  const res = await api.get('/settings')
  return res.data as SettingItem[]
}

/** Public: fetch all non-encrypted settings (no auth) — powers the frontend settings store */
export async function getPublicSettings(): Promise<SettingItem[]> {
  const res = await api.get('/settings/public')
  return res.data as SettingItem[]
}

/** Public: fetch a single setting by key (no auth) */
export async function getSetting(key: string): Promise<SettingItem | null> {
  const res = await api.get(`/settings/${key}`)
  return res.data as SettingItem | null
}

/** Create a new setting (requires settings.update) */
export async function createSetting(payload: CreateSettingPayload): Promise<SettingItem> {
  const res = await api.post('/settings', payload)
  return res.data as SettingItem
}

/** Update a setting's value/description (requires settings.update) */
export async function updateSetting(key: string, payload: UpdateSettingPayload): Promise<SettingItem> {
  const res = await api.put(`/settings/${key}`, payload)
  return res.data as SettingItem
}

/** Delete a setting (requires settings.update) */
export async function deleteSetting(key: string): Promise<void> {
  await api.delete(`/settings/${key}`)
}
