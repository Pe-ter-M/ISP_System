export interface StaffSummary {
  id: number
  userId: number
  staffCode: string
  fullName: string
  email: string | null
  phone: string
  roleId: number
  roleName: string
  salaryCents: number
  employmentType: string
  dateJoined: string | null
  notes: string | null
  status: string
  isActive: boolean
  createdAt: string
  updatedAt: string
}

export interface PaginatedStaff {
  items: StaffSummary[]
  totalCount: number
  page: number
  pageSize: number
  totalPages: number
}

export interface StaffStats {
  total: number
  active: number
  inactive: number
  totalMonthlySalaryCents: number
}

export interface CreateStaffPayload {
  email: string
  password: string
  fullName: string
  phone: string
  roleId: number
  salaryCents: number
  employmentType: string
  dateJoined?: string | null
  notes: string | null
}

export interface UpdateStaffPayload {
  fullName: string
  email: string
  phone: string
  roleId: number
  salaryCents: number
  employmentType: string
  dateJoined?: string | null
  notes: string | null
  status: string
}

export interface DeleteStaffResult {
  hardDeleted: boolean
  message: string
}
