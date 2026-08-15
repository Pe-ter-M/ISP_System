import api from './api'
import type {
  PagedSubscriptions,
  SubscriptionStats,
  SubscriptionSummary,
  Payment,
  PaymentMethodOption,
  CreateSubscriptionPayload,
  UpdateSubscriptionPayload,
} from '@/types/subscription.types'

export interface SubscriptionsQuery {
  page?: number
  pageSize?: number
  search?: string
  status?: 'all' | 'active' | 'suspended' | 'expired'
  sortBy?: string
  sortDesc?: boolean
}

/** Paged subscription list with search/filter/sort (server-side) */
export async function getSubscriptions(query: SubscriptionsQuery = {}): Promise<PagedSubscriptions> {
  const res = await api.get('/subscriptions', {
    params: {
      page: query.page ?? 1,
      pageSize: query.pageSize ?? 10,
      search: query.search || undefined,
      status: query.status === 'all' || !query.status ? undefined : query.status,
      sortBy: query.sortBy || undefined,
      sortDesc: query.sortDesc || undefined,
    },
  })
  return res.data as PagedSubscriptions
}

/** Single subscription — GET also lazily expires past-due subscriptions and syncs RADIUS reject rules */
export async function getSubscriptionById(id: number): Promise<SubscriptionSummary> {
  const res = await api.get(`/subscriptions/${id}`)
  return res.data as SubscriptionSummary
}

export async function getSubscriptionStats(): Promise<SubscriptionStats> {
  const res = await api.get('/subscriptions/stats')
  return res.data as SubscriptionStats
}

export async function createSubscription(payload: CreateSubscriptionPayload): Promise<SubscriptionSummary> {
  const res = await api.post('/subscriptions', payload)
  return res.data as SubscriptionSummary
}

export async function updateSubscription(id: number, payload: UpdateSubscriptionPayload): Promise<SubscriptionSummary> {
  const res = await api.patch(`/subscriptions/${id}`, payload)
  return res.data as SubscriptionSummary
}

/** Suspend / resume a subscription — governed by subscription.suspend permission. */
export async function updateSubscriptionStatus(id: number, status: 'active' | 'suspended' | 'expired'): Promise<SubscriptionSummary> {
  const res = await api.patch(`/subscriptions/${id}/status`, { status })
  return res.data as SubscriptionSummary
}

export async function deleteSubscription(id: number): Promise<void> {
  await api.delete(`/subscriptions/${id}`)
}

/** Registered payment gateways (from the API) */
export async function getPaymentMethods(): Promise<PaymentMethodOption[]> {
  const res = await api.get('/payments/methods')
  return res.data as PaymentMethodOption[]
}

/** Payment history for a subscription */
export async function getPayments(subscriptionId: number): Promise<Payment[]> {
  const res = await api.get('/payments', { params: { subscriptionId } })
  return res.data as Payment[]
}
