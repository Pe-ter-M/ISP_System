import api from './api'
import type { PlanSummary, PlanDetail } from '@/types/plan.types'

export async function getPlans(): Promise<PlanSummary[]> {
  const res = await api.get('/plans')
  return res.data as PlanSummary[]
}

export async function getPlanDetail(id: number): Promise<PlanDetail> {
  const res = await api.get(`/plans/${id}`)
  return res.data as PlanDetail
}
