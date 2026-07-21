import { defineStore } from 'pinia'
import { organizationService } from '@/services/organization.service'
import type { OrganizationData } from '@/types/organization.types'

export const useOrganizationStore = defineStore('organization', {
  state: (): OrganizationData => ({
    id: 0,
    name: '',
    shortName: null,
    tagline: null,
    logoUrl: null,
    currency: 'KSH',
    currencySymbol: 'KSh',
    timezone: 'Africa/Nairobi',
    supportEmail: null,
    supportPhone: null,
    address: null,
    setupCompleted: false,
    createdAt: '',
    updatedAt: '',
  }),
  actions: {
    async load() {
      const data = await organizationService.get()
      if (data) this.$patch(data)
    },
  },
})
