import { createContext, useCallback, useContext, useEffect, useState } from 'react'
import type { ReactNode } from 'react'
import * as authApi from '../lib/api/auth'
import type { CurrentUser, UpdateProfileInput } from '../types/api'

interface AuthContextValue {
  user: CurrentUser | null
  isLoading: boolean
  /** Bumped every time the avatar changes, so <img> consumers can cache-bust the URL. */
  avatarVersion: number
  login: (email: string, password: string) => Promise<void>
  register: (email: string, password: string, displayName: string) => Promise<void>
  logout: () => Promise<void>
  updateProfile: (input: UpdateProfileInput) => Promise<void>
  uploadAvatar: (file: File) => Promise<void>
  removeAvatar: () => Promise<void>
}

const AuthContext = createContext<AuthContextValue | undefined>(undefined)

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<CurrentUser | null>(null)
  const [isLoading, setIsLoading] = useState(true)
  const [avatarVersion, setAvatarVersion] = useState(0)

  useEffect(() => {
    let cancelled = false
    authApi.getCurrentUser().then((currentUser) => {
      if (!cancelled) {
        setUser(currentUser)
        setIsLoading(false)
      }
    })
    return () => {
      cancelled = true
    }
  }, [])

  const login = useCallback(async (email: string, password: string) => {
    const currentUser = await authApi.login(email, password)
    setUser(currentUser)
  }, [])

  const register = useCallback(async (email: string, password: string, displayName: string) => {
    const currentUser = await authApi.register(email, password, displayName)
    setUser(currentUser)
  }, [])

  const logout = useCallback(async () => {
    await authApi.logout()
    setUser(null)
  }, [])

  const updateProfile = useCallback(async (input: UpdateProfileInput) => {
    const currentUser = await authApi.updateProfile(input)
    setUser(currentUser)
  }, [])

  const uploadAvatar = useCallback(async (file: File) => {
    const currentUser = await authApi.uploadAvatar(file)
    setUser(currentUser)
    setAvatarVersion((v) => v + 1)
  }, [])

  const removeAvatar = useCallback(async () => {
    const currentUser = await authApi.removeAvatar()
    setUser(currentUser)
    setAvatarVersion((v) => v + 1)
  }, [])

  return (
    <AuthContext.Provider
      value={{ user, isLoading, avatarVersion, login, register, logout, updateProfile, uploadAvatar, removeAvatar }}
    >
      {children}
    </AuthContext.Provider>
  )
}

export function useAuth(): AuthContextValue {
  const context = useContext(AuthContext)
  if (!context) {
    throw new Error('useAuth must be used within an AuthProvider')
  }
  return context
}
