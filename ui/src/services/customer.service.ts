import api from './api'
import type { PaginatedCustomers, CustomerDetail, CustomerSummary } from '@/types/customer.types'

export interface CreateCustomerPayload {
  email: string
  password: string
  fullName: string
  phone: string
  businessName: string | null
  customerType: string
  serviceAddress: string | null
  city: string | null
  region: string | null
  gpsLat: number | null
  gpsLng: number | null
}

export interface UpdateCustomerPayload {
  fullName: string
  email: string
  phone: string
  businessName: string | null
  customerType: string
  serviceAddress: string | null
  city: string | null
  region: string | null
  gpsLat: number | null
  gpsLng: number | null
  status: string
  notes: string | null
}

export interface DeleteCustomerResult {
  hardDeleted: boolean
  message: string
}

export async function getCustomers(
  page = 1,
  pageSize = 10,
  search?: string,
  sortBy?: string,
  sortDesc = false,
  subscription?: 'all' | 'none' | 'active',
): Promise<PaginatedCustomers> {
  const params: Record<string, string | number | boolean> = { page, pageSize }
  if (search) params.search = search
  if (sortBy) params.sortBy = sortBy
  if (sortDesc) params.sortDesc = true
  if (subscription && subscription !== 'all') params.subscription = subscription

  const res = await api.get('/customers', { params })
  return res.data as PaginatedCustomers
}

export async function getCustomerById(id: number): Promise<CustomerDetail> {
  const res = await api.get(`/customers/${id}`)
  return res.data as CustomerDetail
}

export async function createCustomer(payload: CreateCustomerPayload): Promise<CustomerSummary> {
  const res = await api.post('/customers', payload)
  return res.data as CustomerSummary
}

export async function updateCustomer(id: number, payload: UpdateCustomerPayload): Promise<CustomerSummary> {
  const res = await api.put(`/customers/${id}`, payload)
  return res.data as CustomerSummary
}

export async function deleteCustomer(id: number): Promise<DeleteCustomerResult> {
  const res = await api.delete(`/customers/${id}`)
  return res.data as DeleteCustomerResult
}
