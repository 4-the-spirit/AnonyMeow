export interface FlairResponse {
  id: string
  name: string
  colorHex: string
  isDefault: boolean
}

export interface CreateFlairRequest {
  name: string
  colorHex: string
}
