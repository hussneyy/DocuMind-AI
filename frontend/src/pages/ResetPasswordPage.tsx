import axios from 'axios'
import { useState } from 'react'
import type { FormEvent } from 'react'
import { KeyRound } from 'lucide-react'
import { Link, useSearchParams } from 'react-router-dom'
import { Alert } from '../components/ui/Alert'
import { Button } from '../components/ui/Button'
import { TextField } from '../components/ui/TextField'
import { useLanguage } from '../context/LanguageContext'
import * as authApi from '../lib/api/auth'
import { extractErrorMessages } from '../lib/apiClient'

export function ResetPasswordPage() {
  const { t } = useLanguage()
  const [searchParams] = useSearchParams()
  const email = searchParams.get('email') ?? ''
  const token = searchParams.get('token') ?? ''

  const [newPassword, setNewPassword] = useState('')
  const [confirmPassword, setConfirmPassword] = useState('')
  const [errors, setErrors] = useState<string[]>([])
  const [isSubmitting, setIsSubmitting] = useState(false)
  const [isDone, setIsDone] = useState(false)

  const missingLinkParams = !email || !token

  async function handleSubmit(event: FormEvent) {
    event.preventDefault()
    setErrors([])

    if (newPassword !== confirmPassword) {
      setErrors([t('auth.register.passwordsDoNotMatch')])
      return
    }

    setIsSubmitting(true)
    try {
      await authApi.resetPassword(email, token, newPassword)
      setIsDone(true)
    } catch (error) {
      setErrors(axios.isAxiosError(error) ? extractErrorMessages(error) : [t('auth.resetPassword.resetFailed')])
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
        <h1 className="mt-4 text-2xl">{t('auth.resetPassword.title')}</h1>

        {isDone ? (
          <div className="mt-6 flex flex-col gap-4">
            <Alert variant="success">{t('auth.resetPassword.done')}</Alert>
            <Button to="/login" size="lg">
              {t('auth.resetPassword.continueToLogin')}
            </Button>
          </div>
        ) : missingLinkParams ? (
          <div className="mt-6">
            <Alert variant="error">
              {t('auth.resetPassword.missingParamsPrefix')}{' '}
              <Link to="/forgot-password" className="font-medium underline">
                {t('auth.resetPassword.missingParamsLink')}
              </Link>{' '}
              {t('auth.resetPassword.missingParamsSuffix')}
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

            <p className="text-sm text-slate-600 dark:text-slate-400">
              {t('auth.resetPassword.resettingFor')} <span className="font-medium text-slate-900 dark:text-white">{email}</span>
            </p>

            <TextField
              label={t('auth.resetPassword.newPassword')}
              type="password"
              autoComplete="new-password"
              required
              minLength={8}
              value={newPassword}
              onChange={(e) => setNewPassword(e.target.value)}
            />
            <TextField
              label={t('auth.resetPassword.confirmNewPassword')}
              type="password"
              autoComplete="new-password"
              required
              minLength={8}
              value={confirmPassword}
              onChange={(e) => setConfirmPassword(e.target.value)}
            />
            <p className="text-xs text-slate-500 dark:text-slate-400">{t('auth.resetPassword.passwordHint')}</p>

            <Button type="submit" size="lg" isLoading={isSubmitting} className="mt-2">
              {isSubmitting ? t('auth.resetPassword.resetting') : t('auth.resetPassword.resetPassword')}
            </Button>
          </form>
        )}
      </div>
    </main>
  )
}
