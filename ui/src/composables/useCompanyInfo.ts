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

  const address = computed(() =>
    settings.value('company_address') ?? org.address ?? 'Nairobi, Kenya',
  )

  /** Currency code, e.g. "KES" — used before amounts across the site */
  const currency = computed(() => settings.value('currency') ?? 'KSh')

  return { companyName, companyShortName, initial, phone, email, address, currency }
}
