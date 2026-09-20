export type FriendshipStatus = 'Pending' | 'Accepted' | 'Declined'

export interface FriendRequestResponse {
  requesterUsername: string
  requesterDisplayName: string | null
  addresseeUsername: string
  addresseeDisplayName: string | null
  status: FriendshipStatus
  createdAtUtc: string
}

export interface FriendRequestsResponse {
  incoming: FriendRequestResponse[]
  outgoing: FriendRequestResponse[]
}

export interface FriendResponse {
  username: string
  displayName: string | null
  avatarSeed: string | null
  friendsSinceUtc: string
}
