import { apiClient } from '../apiClient'
import type { CurrentUser, ForgotPasswordResult, UpdateProfileInput } from '../../types/api'

export async function register(email: string, password: string, displayName: string): Promise<CurrentUser> {
  const { data } = await apiClient.post<CurrentUser>('/auth/register', { email, password, displayName })
  return data
}

export async function login(email: string, password: string): Promise<CurrentUser> {
  const { data } = await apiClient.post<CurrentUser>('/auth/login', { email, password })
  return data
}

export async function logout(): Promise<void> {
  await apiClient.post('/auth/logout')
}

export async function getCurrentUser(): Promise<CurrentUser | null> {
  try {
    const { data } = await apiClient.get<CurrentUser>('/auth/me')
    return data
  } catch {
    return null
  }
}

export async function updateProfile(input: UpdateProfileInput): Promise<CurrentUser> {
  const { data } = await apiClient.patch<CurrentUser>('/auth/me', input)
  return data
}

export async function uploadAvatar(file: File): Promise<CurrentUser> {
  const formData = new FormData()
  formData.append('file', file)
  const { data } = await apiClient.post<CurrentUser>('/auth/me/avatar', formData, {
    headers: { 'Content-Type': 'multipart/form-data' },
  })
  return data
}

export async function removeAvatar(): Promise<CurrentUser> {
  const { data } = await apiClient.delete<CurrentUser>('/auth/me/avatar')
  return data
}

export async function forgotPassword(email: string): Promise<ForgotPasswordResult> {
  const { data } = await apiClient.post<ForgotPasswordResult>('/auth/forgot-password', { email })
  return data
}

export async function resetPassword(email: string, token: string, newPassword: string): Promise<void> {
  await apiClient.post('/auth/reset-password', { email, token, newPassword })
}

/** Resolves the same base URL axios is configured with, for use as a plain <img> src (not an axios call). */
export function getAvatarUrl(cacheBust?: number): string {
  const base = apiClient.defaults.baseURL ?? '/api'
  const url = `${base.replace(/\/+$/, '')}/auth/me/avatar`
  return cacheBust ? `${url}?v=${cacheBust}` : url
}
