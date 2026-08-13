import api from './api'
import type { PlanSummary, PlanDetail, CreatePlanPayload, UpdatePlanPayload } from '@/types/plan.types'

// ── Public ──
export async function getPlans(): Promise<PlanSummary[]> {
  const res = await api.get('/plans')
  return res.data as PlanSummary[]
}

export async function getPlanDetail(id: number): Promise<PlanDetail> {
  const res = await api.get(`/plans/${id}`)
  return res.data as PlanDetail
}

// ── Admin ──
export interface PagedPlans {
  items: PlanSummary[]
  totalCount: number
  page: number
  pageSize: number
  totalPages: number
}

export interface PlanStats {
  totalPlans: number
  activePlans: number
  inactivePlans: number
  totalSubscribers: number
}

export interface AdminPlansQuery {
  page?: number
  pageSize?: number
  search?: string
  status?: 'all' | 'active' | 'inactive'
  sortBy?: string
  sortDesc?: boolean
}

/** List ALL plans (active + inactive) with server-side search/sort/pagination, for management */
export async function getAdminPlans(query: AdminPlansQuery = {}): Promise<PagedPlans> {
  const res = await api.get('/admin/plans', {
    params: {
      page: query.page ?? 1,
      pageSize: query.pageSize ?? 10,
      search: query.search || undefined,
      status: query.status === 'all' || !query.status ? undefined : query.status,
      sortBy: query.sortBy || undefined,
      sortDesc: query.sortDesc || undefined,
      subscribersCount: true,
    },
  })
  return res.data as PagedPlans
}

/** Aggregate counts for the stats cards (total / active / inactive / subscribers) */
export async function getPlanStats(): Promise<PlanStats> {
  const res = await api.get('/admin/plans/stats')
  return res.data as PlanStats
}

export async function createPlan(payload: CreatePlanPayload): Promise<PlanSummary> {
  const res = await api.post('/admin/plans', payload)
  return res.data as PlanSummary
}

export async function updatePlan(id: number, payload: UpdatePlanPayload): Promise<PlanSummary> {
  const res = await api.put(`/admin/plans/${id}`, payload)
  return res.data as PlanSummary
}

export async function deletePlan(id: number): Promise<void> {
  await api.delete(`/admin/plans/${id}`)
}
