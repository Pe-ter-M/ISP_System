import { defineStore } from 'pinia'
import { ref } from 'vue'
import { getPublicSettings } from '@/services/settings.service'
import type { SettingItem } from '@/types/settings.types'

/**
 * Public settings cache — fetched once and kept in memory because these values
 * rarely change, but are read all over the site (contact info, business hours,
 * business days, branding). Call `refresh()` after an admin updates settings.
 */
export const useSettingsStore = defineStore('settings', () => {
  const settings = ref<Record<string, SettingItem>>({})
  const loaded = ref(false)
  const loading = ref(false)

  async function load(force = false) {
    if (loaded.value && !force) return
    loading.value = true
    try {
      const items = await getPublicSettings()
      const map: Record<string, SettingItem> = {}
      for (const s of items) map[s.key] = s
      settings.value = map
      loaded.value = true
    } finally {
      loading.value = false
    }
  }

  async function refresh() {
    await load(true)
  }

  function get(key: string): SettingItem | undefined {
    return settings.value[key]
  }

  function value(key: string): string | null {
    return settings.value[key]?.value ?? null
  }

  return { settings, loaded, loading, load, refresh, get, value }
})
