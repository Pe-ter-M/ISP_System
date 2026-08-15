import { computed } from 'vue'
import { useSettingsStore } from '@/stores/settings.store'
import { useOrganizationStore } from '@/stores/organization.store'

/**
 * Central source of company/brand info used across the site.
 *
 * Precedence: settings (per-ISP config) > org profile > built-in defaults.
 * `company_name` is the main website name; `company_short_name` is a shortform
 * (e.g. "PIP" for "Phantom Internet Providers").
 */

/** A company/office location: either JSON { address, lat, lng } or plain text */
export interface CompanyLocation {
  address: string
  lat: number | null
  lng: number | null
}

/** Parse a stored location value (JSON from the map picker, or a plain string) */
function parseLocation(raw: string | null | undefined): CompanyLocation | null {
  if (!raw) return null
  const t = raw.trim()
  if (!t) return null
  if (t.startsWith('{')) {
    try {
      const p = JSON.parse(t) as Record<string, unknown>
      if (p && typeof p.address === 'string') {
        return {
          address: p.address,
          lat: typeof p.lat === 'number' ? p.lat : null,
          lng: typeof p.lng === 'number' ? p.lng : null,
        }
      }
    } catch {
      // not JSON — fall through and treat as plain text
    }
  }
  return { address: t, lat: null, lng: null }
}

export function useCompanyInfo() {
  const settings = useSettingsStore()
  const org = useOrganizationStore()

  /** Main website / company name */
  const companyName = computed(() =>
    settings.value('company_name') ?? org.name ?? 'PhantomNet',
  )

  /** Shortform name (used for the logo initial / short label) */
  const companyShortName = computed(() =>
    settings.value('company_short_name') ?? org.shortName ?? companyName.value,
  )

  /** First letter of the short name, for the logo badge */
  const initial = computed(() => companyShortName.value?.charAt(0) || 'P')

  const phone = computed(() =>
    settings.value('company_phone') ?? org.supportPhone ?? '+254 700 000 000',
  )

  const email = computed(() =>
    settings.value('company_email') ?? org.supportEmail ?? 'support@phantomnet.co.ke',
  )

  /** Office location from the office_address setting (includes coordinates) */
  const office = computed(() => parseLocation(settings.value('office_address')))
  /** Fallback company address from company_address or the org profile */
  const companyAddr = computed(() =>
    parseLocation(settings.value('company_address')) ?? (org.address ? { address: org.address, lat: null, lng: null } : null),
  )

  /** Display address: office > company address > org profile > default */
  const address = computed(() =>
    office.value?.address || companyAddr.value?.address || 'Nairobi, Kenya',
  )

  /** Office coordinates for maps (e.g. office-to-client routing) — null when unset */
  const officeLocation = computed<{ lat: number; lng: number } | null>(() => {
    if (office.value?.lat != null && office.value?.lng != null) {
      return { lat: office.value.lat, lng: office.value.lng }
    }
    return null
  })

  /** Currency code, e.g. "KES" — used before amounts across the site */
  const currency = computed(() => settings.value('currency') ?? 'KSh')

  return {
    companyName, companyShortName, initial, phone, email,
    address, office, officeLocation, currency,
  }
}
