import axios from 'axios'
import { useRef, useState } from 'react'
import type { FormEvent } from 'react'
import { Mail, Moon, Sun, UserRound } from 'lucide-react'
import { Alert } from '../components/ui/Alert'
import { Avatar } from '../components/ui/Avatar'
import { Button } from '../components/ui/Button'
import { TextField } from '../components/ui/TextField'
import { useAuth } from '../context/AuthContext'
import { useTheme } from '../context/ThemeContext'
import { extractErrorMessages } from '../lib/apiClient'
import { formatDate } from '../lib/format'

export function AccountPage() {
  const { user, avatarVersion, updateProfile, uploadAvatar, removeAvatar } = useAuth()
  const { theme, toggleTheme } = useTheme()

  const [displayName, setDisplayName] = useState(user?.displayName ?? '')
  const [bio, setBio] = useState(user?.bio ?? '')
  const [location, setLocation] = useState(user?.location ?? '')
  const [phoneNumber, setPhoneNumber] = useState(user?.phoneNumber ?? '')
  const [errors, setErrors] = useState<string[]>([])
  const [successMessage, setSuccessMessage] = useState<string | null>(null)
  const [isSaving, setIsSaving] = useState(false)

  const [avatarErrors, setAvatarErrors] = useState<string[]>([])
  const [isUpdatingAvatar, setIsUpdatingAvatar] = useState(false)
  const fileInputRef = useRef<HTMLInputElement>(null)

  if (!user) return null

  const isUnchanged =
    displayName.trim() === user.displayName &&
    bio.trim() === (user.bio ?? '') &&
    location.trim() === (user.location ?? '') &&
    phoneNumber.trim() === (user.phoneNumber ?? '')

  async function handleSubmit(event: FormEvent) {
    event.preventDefault()
    setErrors([])
    setSuccessMessage(null)
    setIsSaving(true)
    try {
      await updateProfile({
        displayName: displayName.trim(),
        bio: bio.trim(),
        location: location.trim(),
        phoneNumber: phoneNumber.trim(),
      })
      setSuccessMessage('Your profile has been updated.')
    } catch (error) {
      setErrors(axios.isAxiosError(error) ? extractErrorMessages(error) : ['Failed to update profile.'])
    } finally {
      setIsSaving(false)
    }
  }

  async function handleAvatarSelected(file: File) {
    setAvatarErrors([])
    setIsUpdatingAvatar(true)
    try {
      await uploadAvatar(file)
    } catch (error) {
      setAvatarErrors(axios.isAxiosError(error) ? extractErrorMessages(error) : ['Failed to upload photo.'])
    } finally {
      setIsUpdatingAvatar(false)
    }
  }

  async function handleRemoveAvatar() {
    setAvatarErrors([])
    setIsUpdatingAvatar(true)
    try {
      await removeAvatar()
    } catch (error) {
      setAvatarErrors(axios.isAxiosError(error) ? extractErrorMessages(error) : ['Failed to remove photo.'])
    } finally {
      setIsUpdatingAvatar(false)
    }
  }

  return (
    <main className="mx-auto max-w-2xl px-4 py-10 sm:px-6 lg:px-8">
      <div className="flex flex-col gap-1">
        <h1 className="text-2xl">Account</h1>
        <p className="text-sm text-slate-600 dark:text-slate-400">Manage your profile details.</p>
      </div>

      <div className="animate-fade-in-up mt-8 rounded-2xl border border-slate-200 bg-white p-6 shadow-sm dark:border-slate-700 dark:bg-slate-800">
        <div className="flex items-center gap-4">
          <Avatar user={user} avatarVersion={avatarVersion} size="lg" />
          <div className="flex flex-col gap-2">
            <div className="flex flex-wrap gap-2">
              <Button
                type="button"
                variant="secondary"
                size="md"
                isLoading={isUpdatingAvatar}
                onClick={() => fileInputRef.current?.click()}
              >
                {user.hasAvatar ? 'Change photo' : 'Upload photo'}
              </Button>
              {user.hasAvatar && (
                <Button type="button" variant="ghost" size="md" disabled={isUpdatingAvatar} onClick={handleRemoveAvatar}>
                  Remove
                </Button>
              )}
            </div>
            <p className="text-xs text-slate-500 dark:text-slate-400">PNG, JPEG, or WEBP. Up to 2 MB.</p>
            <input
              ref={fileInputRef}
              type="file"
              accept="image/png,image/jpeg,image/webp"
              aria-label="Upload profile photo"
              className="sr-only"
              onChange={(e) => {
                const file = e.target.files?.[0]
                if (file) handleAvatarSelected(file)
                e.target.value = ''
              }}
            />
          </div>
        </div>
        {avatarErrors.length > 0 && (
          <div className="mt-4">
            <Alert variant="error">
              <ul className="list-inside list-disc">
                {avatarErrors.map((error) => (
                  <li key={error}>{error}</li>
                ))}
              </ul>
            </Alert>
          </div>
        )}

        <div className="mt-6 flex flex-col gap-4 border-t border-slate-100 pt-6 dark:border-slate-700">
          <div className="flex items-center gap-3">
            <span className="flex h-9 w-9 shrink-0 items-center justify-center rounded-full bg-slate-100 text-slate-400 dark:bg-slate-700 dark:text-slate-500">
              <Mail className="h-4 w-4" aria-hidden="true" />
            </span>
            <div className="min-w-0">
              <p className="text-xs font-medium uppercase tracking-wide text-slate-500 dark:text-slate-400">Email</p>
              <p className="truncate text-sm text-slate-900 dark:text-white">{user.email}</p>
            </div>
          </div>
          <div className="flex items-center gap-3">
            <span className="flex h-9 w-9 shrink-0 items-center justify-center rounded-full bg-slate-100 text-slate-400 dark:bg-slate-700 dark:text-slate-500">
              <UserRound className="h-4 w-4" aria-hidden="true" />
            </span>
            <div className="min-w-0">
              <p className="text-xs font-medium uppercase tracking-wide text-slate-500 dark:text-slate-400">Member since</p>
              <p className="truncate text-sm text-slate-900 dark:text-white">{formatDate(user.createdAt)}</p>
            </div>
          </div>
        </div>

        <form onSubmit={handleSubmit} className="mt-6 flex flex-col gap-4 border-t border-slate-100 pt-6 dark:border-slate-700">
          {errors.length > 0 && (
            <Alert variant="error">
              <ul className="list-inside list-disc">
                {errors.map((error) => (
                  <li key={error}>{error}</li>
                ))}
              </ul>
            </Alert>
          )}
          {successMessage && <Alert variant="success">{successMessage}</Alert>}

          <TextField
            label="Display name"
            type="text"
            required
            maxLength={100}
            value={displayName}
            onChange={(e) => {
              setSuccessMessage(null)
              setDisplayName(e.target.value)
            }}
          />

          <div className="flex flex-col gap-1.5">
            <label htmlFor="bio" className="text-sm font-medium text-slate-700 dark:text-slate-300">
              Bio
            </label>
            <textarea
              id="bio"
              rows={3}
              maxLength={280}
              value={bio}
              onChange={(e) => {
                setSuccessMessage(null)
                setBio(e.target.value)
              }}
              placeholder="A short bio about yourself"
              className="w-full resize-none rounded-lg border border-slate-300 px-3 py-2 text-sm text-slate-900 shadow-sm transition-colors placeholder:text-slate-400 focus:border-brand-600 focus:outline-none focus:ring-2 focus:ring-brand-600 dark:border-slate-600 dark:bg-slate-800 dark:text-white dark:placeholder:text-slate-500"
            />
            <p className="text-right text-xs text-slate-500 dark:text-slate-400">{bio.length}/280</p>
          </div>

          <TextField
            label="Location"
            type="text"
            maxLength={100}
            placeholder="City, Country"
            value={location}
            onChange={(e) => {
              setSuccessMessage(null)
              setLocation(e.target.value)
            }}
          />

          <TextField
            label="Phone number"
            type="tel"
            maxLength={30}
            placeholder="+1 555 123 4567"
            value={phoneNumber}
            onChange={(e) => {
              setSuccessMessage(null)
              setPhoneNumber(e.target.value)
            }}
          />

          <Button type="submit" size="md" isLoading={isSaving} disabled={isUnchanged} className="self-start">
            {isSaving ? 'Saving...' : 'Save changes'}
          </Button>
        </form>
      </div>

      <div className="animate-fade-in-up mt-6 rounded-2xl border border-slate-200 bg-white p-6 shadow-sm dark:border-slate-700 dark:bg-slate-800">
        <h2 className="text-lg">Preferences</h2>
        <div className="mt-4 flex items-center justify-between gap-4">
          <div className="flex items-center gap-3">
            <span className="flex h-9 w-9 shrink-0 items-center justify-center rounded-full bg-slate-100 text-slate-400 dark:bg-slate-700 dark:text-slate-500">
              {theme === 'dark' ? <Moon className="h-4 w-4" aria-hidden="true" /> : <Sun className="h-4 w-4" aria-hidden="true" />}
            </span>
            <div>
              <p className="text-sm font-medium text-slate-900 dark:text-white">Dark mode</p>
              <p className="text-xs text-slate-500 dark:text-slate-400">
                {theme === 'dark' ? 'Currently on.' : 'Currently off.'}
              </p>
            </div>
          </div>
          <button
            type="button"
            role="switch"
            aria-checked={theme === 'dark'}
            aria-label="Toggle dark mode"
            onClick={toggleTheme}
            className={`relative inline-flex h-6 w-11 shrink-0 items-center rounded-full transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-600 focus-visible:ring-offset-2 dark:focus-visible:ring-offset-slate-800 ${
              theme === 'dark' ? 'bg-brand-600' : 'bg-slate-300 dark:bg-slate-600'
            }`}
          >
            <span
              className={`inline-block h-4 w-4 transform rounded-full bg-white shadow transition-transform ${
                theme === 'dark' ? 'translate-x-6' : 'translate-x-1'
              }`}
            />
          </button>
        </div>
      </div>
    </main>
  )
}
