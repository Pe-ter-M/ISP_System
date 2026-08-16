import { useSettingsStore } from '@/stores/settings.store'

export interface PlanSummary {
  id: number
  name: string
  description: string | null
  priceCents: number
  billingCycle: string
  bandwidthUpKbps: number | null
  bandwidthDownKbps: number | null
  maxDevices: number
  isActive: boolean
  sortOrder: number
  activeSubscribersCount: number | null
}

export interface PlanDetail {
  id: number
  name: string
  description: string | null
  priceCents: number
  billingCycle: string
  bandwidthUpKbps: number | null
  bandwidthDownKbps: number | null
  sessionTimeoutSeconds: number
  idleTimeoutSeconds: number
  maxDevices: number
  isActive: boolean
  sortOrder: number
  groupName: string
  activeSubscribersCount: number | null
}

/** Payload for creating a plan (POST /api/admin/plans) */
export interface CreatePlanPayload {
  name: string
  description: string | null
  priceCents: number
  billingCycle: string
  bandwidthUpKbps: number | null
  bandwidthDownKbps: number | null
  sessionTimeoutSeconds: number | null
  idleTimeoutSeconds: number | null
  maxDevices: number | null
  sortOrder: number | null
}

/** Payload for updating a plan (PUT /api/admin/plans/{id}) — all fields optional, omitted = keep */
export interface UpdatePlanPayload {
  name?: string
  description?: string | null
  priceCents?: number
  billingCycle?: string
  bandwidthUpKbps?: number | null
  bandwidthDownKbps?: number | null
  sessionTimeoutSeconds?: number
  idleTimeoutSeconds?: number
  maxDevices?: number
  sortOrder?: number
  isActive?: boolean
}

/** Convert price cents to display string with symbol (currency comes from settings, e.g. "KES 1,500") */
export function formatPrice(cents: number, symbol?: string): string {
  const currency = symbol ?? useSettingsStore().value('currency') ?? 'KSh'
  return `${currency} ${(cents / 100).toLocaleString()}`
}

/** Convert kbps to Mbps display (e.g. 30720 → "30 Mbps") */
export function formatSpeed(kbps: number | null): string {
  if (!kbps) return 'N/A'
  if (kbps >= 1000) return `${Math.floor(kbps / 1000)} Mbps`
  return `${kbps} Kbps`
}

/** Convert seconds to a human-readable duration string */
export function formatDuration(seconds: number): string {
  if (seconds >= 86400) {
    const days = seconds / 86400
    return `${days} day${days > 1 ? 's' : ''}`
  }
  if (seconds >= 3600) {
    const hours = Math.floor(seconds / 3600)
    const mins = Math.round((seconds % 3600) / 60)
    return mins > 0 ? `${hours}h ${mins}m` : `${hours} hours`
  }
  return `${Math.round(seconds / 60)} min`
}
