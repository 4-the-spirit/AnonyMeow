import * as signalR from '@microsoft/signalr'
import { BASE_URL } from '@/lib/api/client'

/**
 * Browsers can't set an Authorization header on the WebSocket/SSE handshake, so the backend
 * promotes an `?access_token=` query-string value to the bearer token for any request under
 * `/hubs` (see Common/Development/DevJwtTokenFactory usage in NotificationHub's auth wiring).
 *
 * `withCredentials: false` is required here: the SignalR client's HTTP client (used for the
 * negotiate request) defaults to `withCredentials: true`, which makes the browser require an
 * `Access-Control-Allow-Credentials: true` response header — the backend's CORS policy
 * deliberately omits that (auth is a Bearer header/query token, not cookies), so without this
 * the negotiate request fails the CORS preflight and the connection never starts.
 */
export function createNotificationHubConnection(token: string): signalR.HubConnection {
  return new signalR.HubConnectionBuilder()
    .withUrl(`${BASE_URL}/hubs/notifications?access_token=${encodeURIComponent(token)}`, {
      withCredentials: false,
    })
    .withAutomaticReconnect()
    .build()
}
