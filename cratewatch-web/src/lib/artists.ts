import { getJson, postJson, putJson } from './api'

export interface ArtistSearchResult {
  musicbrainzId: string
  name: string
  disambiguation: string | null
  country: string | null
}

export interface ArtistResponse {
  id: string
  name: string
  musicbrainzId: string
  coverArtUrl: string | null
}

export type ReleaseState = 'NONE' | 'WISHLIST' | 'OWNED' | 'DONT_WANT'

export interface ReleaseResponse {
  id: string
  title: string
  releaseType: string
  state: ReleaseState
  coverArtUrl: string | null
  firstReleaseDate: string | null
  artistId: string
  artistName: string | null
}

export function searchArtists(query: string, token: string) {
  return getJson<ArtistSearchResult[]>(`/artists/search?query=${encodeURIComponent(query)}`, token)
}

export function listArtists(token: string) {
  return getJson<ArtistResponse[]>('/artists', token)
}

export function createArtist(name: string, musicbrainzId: string, token: string) {
  return postJson<ArtistResponse>('/artists', { name, musicbrainzId }, token)
}

export function getArtist(id: string, token: string) {
  return getJson<ArtistResponse>(`/artists/${id}`, token)
}

export function listReleases(artistId: string, token: string) {
  return getJson<ReleaseResponse[]>(`/artists/${artistId}/releases`, token)
}

export function setReleaseState(releaseId: string, state: ReleaseState, token: string) {
  return putJson<void>(`/releases/${releaseId}/state`, { state }, token)
}

export function listReleasesByState(state: 'OWNED' | 'WISHLIST', token: string) {
  return getJson<ReleaseResponse[]>(`/releases?state=${state}`, token)
}
