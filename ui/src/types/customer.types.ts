export interface CustomerResponse {
  id: number
  customerCode: string
  fullName: string
  businessName: string | null
  email: string | null
  phonePrimary: string | null
  city: string | null
  region: string | null
  status: string
}

export interface CreateCustomerRequest {
  fullName: string
  email?: string
  phonePrimary?: string
  city?: string
  region?: string
}
