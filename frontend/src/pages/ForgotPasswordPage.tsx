import axios from 'axios'
import { useState } from 'react'
import type { FormEvent } from 'react'
import { KeyRound } from 'lucide-react'
import { Link } from 'react-router-dom'
import { Alert } from '../components/ui/Alert'
import { Button } from '../components/ui/Button'
import { TextField } from '../components/ui/TextField'
import * as authApi from '../lib/api/auth'
import { extractErrorMessages } from '../lib/apiClient'

export function ForgotPasswordPage() {
  const [email, setEmail] = useState('')
  const [errors, setErrors] = useState<string[]>([])
  const [isSubmitting, setIsSubmitting] = useState(false)
  const [resetLink, setResetLink] = useState<string | null>(null)
  const [submittedForUnknownEmail, setSubmittedForUnknownEmail] = useState(false)

  async function handleSubmit(event: FormEvent) {
    event.preventDefault()
    setErrors([])
    setResetLink(null)
    setSubmittedForUnknownEmail(false)
    setIsSubmitting(true)
    try {
      const result = await authApi.forgotPassword(email)
      if (result.accountExists && result.resetLink) {
        setResetLink(result.resetLink)
      } else {
        setSubmittedForUnknownEmail(true)
      }
    } catch (error) {
      setErrors(axios.isAxiosError(error) ? extractErrorMessages(error) : ['Something went wrong. Please try again.'])
    } finally {
      setIsSubmitting(false)
    }
  }

  return (
    <main className="relative flex min-h-[80vh] flex-col items-center justify-center overflow-hidden bg-gradient-to-b from-brand-50 to-white px-4 py-16 dark:from-slate-950 dark:to-slate-900">
      <div className="bg-dot-grid pointer-events-none absolute inset-0 dark:hidden" aria-hidden="true" />
      <div className="bg-dot-grid-dark pointer-events-none absolute inset-0 hidden dark:block" aria-hidden="true" />
      <div className="animate-fade-in-up relative w-full max-w-md rounded-2xl border border-slate-200 bg-white p-8 shadow-xl shadow-slate-200/60 dark:border-slate-700 dark:bg-slate-800 dark:shadow-black/30">
        <span className="flex h-11 w-11 items-center justify-center rounded-xl bg-brand-600 text-white shadow-sm shadow-brand-600/30">
          <KeyRound className="h-5 w-5" aria-hidden="true" />
        </span>
        <h1 className="mt-4 text-2xl">Reset your password</h1>
        <p className="mt-2 text-sm text-slate-600 dark:text-slate-400">
          Enter your account email and we'll generate a reset link.
        </p>

        {resetLink ? (
          <div className="mt-6 flex flex-col gap-4">
            <Alert variant="info">
              This project has no email service configured, so here's your reset link directly
              (in production this would be emailed instead):
            </Alert>
            <div className="break-all rounded-lg border border-slate-200 bg-slate-50 p-3 text-xs text-slate-700 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-300">
              {resetLink}
            </div>
            <Button to={resetLink.replace(/^https?:\/\/[^/]+/, '')} size="lg">
              Continue to reset password
            </Button>
          </div>
        ) : submittedForUnknownEmail ? (
          <div className="mt-6">
            <Alert variant="info">
              If an account exists for that email, a reset link has been generated for it.
            </Alert>
          </div>
        ) : (
          <form onSubmit={handleSubmit} noValidate className="mt-8 flex flex-col gap-4">
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
              label="Email"
              type="email"
              autoComplete="email"
              required
              value={email}
              onChange={(e) => setEmail(e.target.value)}
            />

            <Button type="submit" size="lg" isLoading={isSubmitting} className="mt-2">
              {isSubmitting ? 'Sending...' : 'Send reset link'}
            </Button>
          </form>
        )}

        <p className="mt-6 text-sm text-slate-600 dark:text-slate-400">
          <Link to="/login" className="font-medium text-brand-600 hover:text-brand-700 dark:text-brand-400 dark:hover:text-brand-300">
            Back to log in
          </Link>
        </p>
      </div>
    </main>
  )
}
