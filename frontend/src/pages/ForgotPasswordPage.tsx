import axios from 'axios'
import { useState } from 'react'
import type { FormEvent } from 'react'
import { KeyRound } from 'lucide-react'
import { Link } from 'react-router-dom'
import { Alert } from '../components/ui/Alert'
import { Button } from '../components/ui/Button'
import { TextField } from '../components/ui/TextField'
import { useLanguage } from '../context/LanguageContext'
import * as authApi from '../lib/api/auth'
import { extractErrorMessages } from '../lib/apiClient'

export function ForgotPasswordPage() {
  const { t } = useLanguage()
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
      setErrors(axios.isAxiosError(error) ? extractErrorMessages(error) : [t('auth.forgotPassword.genericError')])
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
        <h1 className="mt-4 text-2xl">{t('auth.forgotPassword.title')}</h1>
        <p className="mt-2 text-sm text-slate-600 dark:text-slate-400">{t('auth.forgotPassword.prompt')}</p>

        {resetLink ? (
          <div className="mt-6 flex flex-col gap-4">
            <Alert variant="info">{t('auth.forgotPassword.demoNotice')}</Alert>
            <div className="break-all rounded-lg border border-slate-200 bg-slate-50 p-3 text-xs text-slate-700 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-300">
              {resetLink}
            </div>
            <Button to={resetLink.replace(/^https?:\/\/[^/]+/, '')} size="lg">
              {t('auth.forgotPassword.continueToReset')}
            </Button>
          </div>
        ) : submittedForUnknownEmail ? (
          <div className="mt-6">
            <Alert variant="info">{t('auth.forgotPassword.unknownEmailNotice')}</Alert>
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
              label={t('auth.forgotPassword.email')}
              type="email"
              autoComplete="email"
              required
              value={email}
              onChange={(e) => setEmail(e.target.value)}
            />

            <Button type="submit" size="lg" isLoading={isSubmitting} className="mt-2">
              {isSubmitting ? t('auth.forgotPassword.sendingLink') : t('auth.forgotPassword.sendResetLink')}
            </Button>
          </form>
        )}

        <p className="mt-6 text-sm text-slate-600 dark:text-slate-400">
          <Link to="/login" className="font-medium text-brand-600 hover:text-brand-700 dark:text-brand-400 dark:hover:text-brand-300">
            {t('auth.forgotPassword.backToLogin')}
          </Link>
        </p>
      </div>
    </main>
  )
}
