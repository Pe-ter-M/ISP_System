export type SubscriptionStatus = 'active' | 'suspended' | 'expired'

export interface SubscriptionSummary {
  id: number
  customerId: number
  packageId: number
  username: string
  status: SubscriptionStatus
  currentPeriodStart: string
  currentPeriodEnd: string
  autoRenew: boolean
  paidAmountCents: number
  paymentReference: string | null
  paymentStatus: string
  paymentMethod: string | null
  paymentCompletedAt: string | null
  customerFullName: string
  customerCode: string
  planName: string
}

export interface PagedSubscriptions {
  items: SubscriptionSummary[]
  totalCount: number
  page: number
  pageSize: number
  totalPages: number
}

export interface SubscriptionStats {
  total: number
  active: number
  suspended: number
  expired: number
}

export interface PaymentMethodOption {
  value: string
  label: string
}

export interface Payment {
  id: number
  subscriptionId: number
  amountCents: number
  currency: string
  paymentMethod: string
  status: string
  referenceNumber: string | null
  phoneNumber: string | null
  errorMessage: string | null
  createdAt: string
  completedAt: string | null
}

export interface CreateSubscriptionPayload {
  customerId: number
  packageId: number
  autoRenew: boolean
  paymentMethod: string
  phoneNumber: string
  referenceNotes?: string | null
}

export interface UpdateSubscriptionPayload {
  status?: SubscriptionStatus
  currentPeriodEnd?: string
  autoRenew?: boolean
  packageId?: number
}
