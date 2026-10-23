import { deleteJson, getJson, postJson } from './api'
import type { ReleaseResponse } from './artists'

export interface ShopResponse {
  id: string
  name: string
  searchUrlTemplate: string
}

export interface ScanResult {
  checked: number
  found: number
  failed: number
}

export interface ShopOffer {
  shopId: string
  shopName: string
  url: string
  checkedAt: string
}

export interface AvailableRelease {
  release: ReleaseResponse
  offers: ShopOffer[]
}

export function listShops(token: string) {
  return getJson<ShopResponse[]>('/shops', token)
}

export function createShop(
  name: string,
  searchUrlTemplate: string,
  exampleArtist: string,
  exampleTitle: string,
  token: string,
) {
  return postJson<ShopResponse>('/shops', { name, searchUrlTemplate, exampleArtist, exampleTitle }, token)
}

export function deleteShop(id: string, token: string) {
  return deleteJson(`/shops/${id}`, token)
}

export function scanShops(token: string) {
  return postJson<ScanResult>('/shops/scan', {}, token)
}

export function listAvailability(token: string) {
  return getJson<AvailableRelease[]>('/shops/availability', token)
}
