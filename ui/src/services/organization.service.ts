import api from './api'
import type { OrganizationData, UpdateOrganizationPayload } from '@/types/organization.types'

export const organizationService = {
  async get(): Promise<OrganizationData> {
    const res = await api.get<OrganizationData>('/organization')
    return res.data
  },
  async update(data: UpdateOrganizationPayload): Promise<OrganizationData> {
    const res = await api.put<OrganizationData>('/organization', data)
    return res.data
  },
}
