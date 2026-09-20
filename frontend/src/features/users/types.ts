export type FriendListVisibility = 'Everyone' | 'FriendsOnly' | 'NoOne'

export interface UserResponse {
  username: string
  displayName: string | null
  avatarSeed: string | null
  karma: number
  friendListVisibility: FriendListVisibility
  createdAtUtc: string
  isPlatformAdmin: boolean
}

export interface CompleteProfileRequest {
  username: string
  displayName: string
  avatarSeed: string
}

export interface UpdateProfileRequest {
  displayName?: string
  avatarSeed?: string
  friendListVisibility?: FriendListVisibility
}

export interface UsernameAvailabilityResponse {
  isAvailable: boolean
}
