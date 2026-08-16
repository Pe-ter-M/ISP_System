import api from './api'
import type { PaginatedStaff, StaffStats, StaffSummary, CreateStaffPayload, UpdateStaffPayload, DeleteStaffResult } from '@/types/staff.types'

export interface StaffQuery {
  page?: number
  pageSize?: number
  search?: string
  sortBy?: string
  sortDesc?: boolean
}

export async function getStaff(query: StaffQuery = {}): Promise<PaginatedStaff> {
  const res = await api.get('/staff', {
    params: {
      page: query.page ?? 1,
      pageSize: query.pageSize ?? 10,
      search: query.search || undefined,
      sortBy: query.sortBy || undefined,
      sortDesc: query.sortDesc || undefined,
    },
  })
  return res.data as PaginatedStaff
}

export async function getStaffById(id: number): Promise<StaffSummary> {
  const res = await api.get(`/staff/${id}`)
  return res.data as StaffSummary
}

export async function getStaffStats(): Promise<StaffStats> {
  const res = await api.get('/staff/stats')
  return res.data as StaffStats
}

export async function createStaff(payload: CreateStaffPayload): Promise<StaffSummary> {
  const res = await api.post('/staff', payload)
  return res.data as StaffSummary
}

export async function updateStaff(id: number, payload: UpdateStaffPayload): Promise<StaffSummary> {
  const res = await api.put(`/staff/${id}`, payload)
  return res.data as StaffSummary
}

export async function deleteStaff(id: number): Promise<DeleteStaffResult> {
  const res = await api.delete(`/staff/${id}`)
  return res.data as DeleteStaffResult
}
