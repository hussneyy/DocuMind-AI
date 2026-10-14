import { apiClient } from '../apiClient'
import type { CurrentUser } from '../../types/api'

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
