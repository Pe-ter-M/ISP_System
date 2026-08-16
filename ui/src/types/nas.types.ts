export interface NasClient {
  id: number
  nasname: string
  shortname: string
  type: string
  ports: number | null
  server: string
  community: string | null
  description: string | null
}

export interface PagedNas {
  items: NasClient[]
  totalCount: number
  page: number
  pageSize: number
  totalPages: number
}

export interface CreateNasPayload {
  nasname: string
  shortname: string
  type: string
  ports: number | null
  secret: string
  server: string | null
  community: string | null
  description: string | null
}

/** Secret is optional on update — blank keeps the existing shared secret */
export interface UpdateNasPayload {
  nasname: string
  shortname: string
  type: string
  ports: number | null
  secret?: string
  server: string | null
  community: string | null
  description: string | null
}
