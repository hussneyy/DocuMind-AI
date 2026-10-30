import axios from 'axios'
import { useState } from 'react'
import type { FormEvent } from 'react'
import { Mail, UserRound } from 'lucide-react'
import { Alert } from '../components/ui/Alert'
import { Button } from '../components/ui/Button'
import { TextField } from '../components/ui/TextField'
import { useAuth } from '../context/AuthContext'
import { extractErrorMessages } from '../lib/apiClient'
import { formatDate } from '../lib/format'

export function AccountPage() {
  const { user, updateDisplayName } = useAuth()
  const [displayName, setDisplayName] = useState(user?.displayName ?? '')
  const [errors, setErrors] = useState<string[]>([])
  const [successMessage, setSuccessMessage] = useState<string | null>(null)
  const [isSaving, setIsSaving] = useState(false)

  if (!user) return null

  const isUnchanged = displayName.trim() === user.displayName

  async function handleSubmit(event: FormEvent) {
    event.preventDefault()
    setErrors([])
    setSuccessMessage(null)
    setIsSaving(true)
    try {
      await updateDisplayName(displayName.trim())
      setSuccessMessage('Your display name has been updated.')
    } catch (error) {
      setErrors(axios.isAxiosError(error) ? extractErrorMessages(error) : ['Failed to update display name.'])
    } finally {
      setIsSaving(false)
    }
  }

  return (
    <main className="mx-auto max-w-2xl px-4 py-10 sm:px-6 lg:px-8">
      <div className="flex flex-col gap-1">
        <h1 className="text-2xl">Account</h1>
        <p className="text-sm text-slate-600 dark:text-slate-400">Manage your profile details.</p>
      </div>

      <div className="animate-fade-in-up mt-8 rounded-2xl border border-slate-200 bg-white p-6 shadow-sm dark:border-slate-700 dark:bg-slate-800">
        <div className="flex flex-col gap-4">
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

          <Button type="submit" size="md" isLoading={isSaving} disabled={isUnchanged || !displayName.trim()} className="self-start">
            {isSaving ? 'Saving...' : 'Save changes'}
          </Button>
        </form>
      </div>
    </main>
  )
}
