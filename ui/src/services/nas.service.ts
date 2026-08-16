import api from './api'
import type { NasClient, PagedNas, CreateNasPayload, UpdateNasPayload } from '@/types/nas.types'

export interface NasQuery {
  page?: number
  pageSize?: number
  search?: string
  sortBy?: string
  sortDesc?: boolean
  type?: string
}

export async function getNas(query: NasQuery = {}): Promise<PagedNas> {
  const res = await api.get('/nas', {
    params: {
      page: query.page ?? 1,
      pageSize: query.pageSize ?? 10,
      search: query.search || undefined,
      sortBy: query.sortBy || undefined,
      sortDesc: query.sortDesc || undefined,
      type: query.type || undefined,
    },
  })
  return res.data as PagedNas
}

export async function getNasById(id: number): Promise<NasClient> {
  const res = await api.get(`/nas/${id}`)
  return res.data as NasClient
}

export async function createNas(payload: CreateNasPayload): Promise<NasClient> {
  const res = await api.post('/nas', payload)
  return res.data as NasClient
}

export async function updateNas(id: number, payload: UpdateNasPayload): Promise<NasClient> {
  const res = await api.put(`/nas/${id}`, payload)
  return res.data as NasClient
}

export async function deleteNas(id: number): Promise<void> {
  await api.delete(`/nas/${id}`)
}
