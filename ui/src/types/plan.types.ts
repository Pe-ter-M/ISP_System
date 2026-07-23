export interface PlanSummary {
  id: number
  name: string
  description: string | null
  priceCents: number
  billingCycle: string
  bandwidthUpKbps: number | null
  bandwidthDownKbps: number | null
  maxDevices: number
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
}

/** Convert price cents to display string with symbol */
export function formatPrice(cents: number, symbol = 'KSh'): string {
  return `${symbol} ${(cents / 100).toLocaleString()}`
}

/** Convert kbps to Mbps display (e.g. 30720 → "30 Mbps") */
export function formatSpeed(kbps: number | null): string {
  if (!kbps) return 'N/A'
  if (kbps >= 1000) return `${Math.floor(kbps / 1000)} Mbps`
  return `${kbps} Kbps`
}
