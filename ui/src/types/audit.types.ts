export type AuditActorType = 'staff' | 'customer' | 'system' | 'anonymous' | 'unknown'

export type AuditAction = 'create' | 'update' | 'delete' | 'login_success' | 'login_failure'

export interface AuditActorInfo {
  type: AuditActorType
  fullName: string | null
  role: string | null
  email: string | null
  phone: string | null
  staffCode: string | null
}

export interface AuditChange {
  field: string
  oldValue: string | null
  newValue: string | null
}

export interface AuditLogListItem {
  id: number
  entityType: string
  entityId: number | null
  action: AuditAction
  summary: string
  ipAddress: string | null
  createdAt: string
  actor: AuditActorInfo | null
}

export interface AuditLogDetail {
  id: number
  entityType: string
  entityId: number | null
  entityLabel: string
  action: AuditAction
  summary: string
  ipAddress: string | null
  httpMethod: string | null
  httpPath: string | null
  createdAt: string
  actor: AuditActorInfo | null
  changes: AuditChange[]
}

export interface PagedAuditLogs {
  items: AuditLogListItem[]
  totalCount: number
  page: number
  pageSize: number
  totalPages: number
}

export interface AuditQuery {
  page?: number
  pageSize?: number
  entityType?: string
  action?: string
  search?: string
  sortBy?: string
  sortDesc?: boolean
}
