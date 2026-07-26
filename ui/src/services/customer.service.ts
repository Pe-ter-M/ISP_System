import api from './api'
import type { PaginatedCustomers, CustomerDetail } from '@/types/customer.types'

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
}

export async function getCustomers(
  page = 1,
  pageSize = 10,
  search?: string,
  sortBy?: string,
  sortDesc = false,
): Promise<PaginatedCustomers> {
  const params: Record<string, string | number | boolean> = { page, pageSize }
  if (search) params.search = search
  if (sortBy) params.sortBy = sortBy
  if (sortDesc) params.sortDesc = true

  const res = await api.get('/customers', { params })
  return res.data as PaginatedCustomers
}

export async function getCustomerById(id: number): Promise<CustomerDetail> {
  const res = await api.get(`/customers/${id}`)
  return res.data as CustomerDetail
}

export async function createCustomer(payload: CreateCustomerPayload): Promise<CustomerDetail> {
  const res = await api.post('/customers', payload)
  return res.data as CustomerDetail
}
