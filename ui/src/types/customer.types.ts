export interface CustomerSummary {
  id: number
  userId: number
  customerCode: string
  fullName: string
  businessName: string | null
  customerType: string
  email: string | null
  phone: string
  city: string | null
  region: string | null
  status: string
  createdAt: string
}

export interface CustomerSubscription {
  id: number
  username: string
  planName: string
  status: string
  currentPeriodEnd: string | null
}

export interface CustomerDetail {
  id: number
  userId: number
  customerCode: string
  fullName: string
  businessName: string | null
  customerType: string
  email: string | null
  phone: string
  serviceAddress: string | null
  city: string | null
  region: string | null
  gpsLat: number | null
  gpsLng: number | null
  status: string
  notes: string | null
  createdAt: string
  updatedAt: string
  subscriptions: CustomerSubscription[]
}

export interface PaginatedCustomers {
  items: CustomerSummary[]
  totalCount: number
  page: number
  pageSize: number
  totalPages: number
}
