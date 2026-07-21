export interface OrganizationData {
  id: number
  name: string
  shortName: string | null
  tagline: string | null
  logoUrl: string | null
  currency: string
  currencySymbol: string
  timezone: string
  supportEmail: string | null
  supportPhone: string | null
  address: string | null
  setupCompleted: boolean
  createdAt: string
  updatedAt: string
}

export interface UpdateOrganizationPayload {
  name?: string
  shortName?: string
  tagline?: string
  currency?: string
  currencySymbol?: string
  timezone?: string
  supportEmail?: string
  supportPhone?: string
  address?: string
}
