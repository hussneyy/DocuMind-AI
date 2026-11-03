import axios from 'axios'
import { useEffect, useRef, useState } from 'react'
import type { FormEvent } from 'react'
import { AlertTriangle } from 'lucide-react'
import { Alert } from '../ui/Alert'
import { Button } from '../ui/Button'
import { TextField } from '../ui/TextField'
import { extractErrorMessages } from '../../lib/apiClient'

interface DeleteAccountDialogProps {
  isOpen: boolean
  onConfirm: (password: string) => Promise<void>
  onCancel: () => void
}

export function DeleteAccountDialog({ isOpen, onConfirm, onCancel }: DeleteAccountDialogProps) {
  const [password, setPassword] = useState('')
  const [errors, setErrors] = useState<string[]>([])
  const [isBusy, setIsBusy] = useState(false)
  const dialogRef = useRef<HTMLDivElement>(null)

  useEffect(() => {
    if (!isOpen) return
    dialogRef.current?.focus()
    setPassword('')
    setErrors([])

    function handleKeyDown(event: KeyboardEvent) {
      if (event.key === 'Escape') onCancel()
    }
    document.addEventListener('keydown', handleKeyDown)
    return () => document.removeEventListener('keydown', handleKeyDown)
  }, [isOpen, onCancel])

  if (!isOpen) return null

  async function handleSubmit(event: FormEvent) {
    event.preventDefault()
    setErrors([])
    setIsBusy(true)
    try {
      await onConfirm(password)
    } catch (error) {
      setErrors(axios.isAxiosError(error) ? extractErrorMessages(error) : ['Failed to delete account.'])
      setIsBusy(false)
    }
  }

  return (
    <div className="animate-fade-in fixed inset-0 z-50 flex items-center justify-center bg-slate-900/40 px-4 backdrop-blur-sm">
      <div
        ref={dialogRef}
        role="alertdialog"
        aria-modal="true"
        aria-labelledby="delete-account-title"
        aria-describedby="delete-account-description"
        tabIndex={-1}
        className="animate-scale-in w-full max-w-sm rounded-xl bg-white p-6 shadow-xl ring-1 ring-slate-900/5 focus:outline-none dark:bg-slate-800 dark:ring-white/10"
      >
        <div className="flex items-start gap-3">
          <span className="flex h-10 w-10 shrink-0 items-center justify-center rounded-full bg-red-100 dark:bg-red-500/15">
            <AlertTriangle className="h-5 w-5 text-red-600 dark:text-red-400" aria-hidden="true" />
          </span>
          <div>
            <h2 id="delete-account-title" className="text-lg font-semibold text-slate-900">
              Delete your account?
            </h2>
            <p id="delete-account-description" className="mt-1 text-sm text-slate-600 dark:text-slate-400">
              This permanently deletes your account, documents, and conversations. This cannot be undone.
            </p>
          </div>
        </div>

        <form onSubmit={handleSubmit} className="mt-4 flex flex-col gap-3">
          {errors.length > 0 && (
            <Alert variant="error">
              <ul className="list-inside list-disc">
                {errors.map((error) => (
                  <li key={error}>{error}</li>
                ))}
              </ul>
            </Alert>
          )}

          <TextField
            label="Confirm your password"
            type="password"
            autoComplete="current-password"
            required
            value={password}
            onChange={(e) => setPassword(e.target.value)}
          />

          <div className="mt-2 flex justify-end gap-3">
            <Button type="button" variant="secondary" onClick={onCancel} disabled={isBusy}>
              Cancel
            </Button>
            <Button
              type="submit"
              variant="primary"
              isLoading={isBusy}
              className="!bg-red-600 !shadow-red-600/20 hover:!bg-red-700 focus-visible:!ring-red-600"
            >
              {isBusy ? 'Deleting...' : 'Delete account'}
            </Button>
          </div>
        </form>
      </div>
    </div>
  )
}
