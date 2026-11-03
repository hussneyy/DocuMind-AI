import axios from 'axios'
import { useState } from 'react'
import type { FormEvent } from 'react'
import { KeyRound, ShieldCheck } from 'lucide-react'
import { Link, useLocation, useNavigate } from 'react-router-dom'
import { Alert } from '../components/ui/Alert'
import { Button } from '../components/ui/Button'
import { TextField } from '../components/ui/TextField'
import { useAuth } from '../context/AuthContext'
import { extractErrorMessages } from '../lib/apiClient'

export function LoginPage() {
  const { login, completeTwoFactorLogin, completeTwoFactorLoginWithRecoveryCode } = useAuth()
  const navigate = useNavigate()
  const location = useLocation()

  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [errors, setErrors] = useState<string[]>([])
  const [isSubmitting, setIsSubmitting] = useState(false)

  const [needsTwoFactor, setNeedsTwoFactor] = useState(false)
  const [useRecoveryCode, setUseRecoveryCode] = useState(false)
  const [twoFactorCode, setTwoFactorCode] = useState('')

  const redirectTo = (location.state as { from?: Location })?.from?.pathname ?? '/documents'

  async function handleSubmit(event: FormEvent) {
    event.preventDefault()
    setErrors([])
    setIsSubmitting(true)
    try {
      const result = await login(email, password)
      if (result.requiresTwoFactor) {
        setNeedsTwoFactor(true)
      } else {
        navigate(redirectTo)
      }
    } catch (error) {
      setErrors(axios.isAxiosError(error) ? extractErrorMessages(error) : ['Login failed.'])
    } finally {
      setIsSubmitting(false)
    }
  }

  async function handleTwoFactorSubmit(event: FormEvent) {
    event.preventDefault()
    setErrors([])
    setIsSubmitting(true)
    try {
      if (useRecoveryCode) {
        await completeTwoFactorLoginWithRecoveryCode(twoFactorCode.trim())
      } else {
        await completeTwoFactorLogin(twoFactorCode.trim())
      }
      navigate(redirectTo)
    } catch (error) {
      setErrors(axios.isAxiosError(error) ? extractErrorMessages(error) : ['Verification failed.'])
    } finally {
      setIsSubmitting(false)
    }
  }

  return (
    <main className="relative flex min-h-[80vh] flex-col items-center justify-center overflow-hidden bg-gradient-to-b from-brand-50 to-white px-4 py-16 dark:from-slate-950 dark:to-slate-900">
      <div className="bg-dot-grid pointer-events-none absolute inset-0 dark:hidden" aria-hidden="true" />
      <div className="bg-dot-grid-dark pointer-events-none absolute inset-0 hidden dark:block" aria-hidden="true" />
      <div className="animate-fade-in-up relative w-full max-w-md rounded-2xl border border-slate-200 bg-white p-8 shadow-xl shadow-slate-200/60 dark:border-slate-700 dark:bg-slate-800 dark:shadow-black/30">
        {needsTwoFactor ? (
          <>
            <span className="flex h-11 w-11 items-center justify-center rounded-xl bg-brand-600 text-white shadow-sm shadow-brand-600/30">
              <ShieldCheck className="h-5 w-5" aria-hidden="true" />
            </span>
            <h1 className="mt-4 text-2xl">Two-factor verification</h1>
            <p className="mt-2 text-sm text-slate-600 dark:text-slate-400">
              {useRecoveryCode
                ? 'Enter one of your recovery codes.'
                : 'Enter the 6-digit code from your authenticator app.'}
            </p>

            <form onSubmit={handleTwoFactorSubmit} noValidate className="mt-8 flex flex-col gap-4">
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
                label={useRecoveryCode ? 'Recovery code' : 'Verification code'}
                type="text"
                autoComplete="one-time-code"
                required
                value={twoFactorCode}
                onChange={(e) => setTwoFactorCode(e.target.value)}
              />

              <Button type="submit" size="lg" isLoading={isSubmitting} className="mt-2">
                {isSubmitting ? 'Verifying...' : 'Verify'}
              </Button>

              <button
                type="button"
                onClick={() => {
                  setUseRecoveryCode((v) => !v)
                  setTwoFactorCode('')
                  setErrors([])
                }}
                className="self-center text-xs font-medium text-brand-600 hover:text-brand-700 dark:text-brand-400 dark:hover:text-brand-300"
              >
                {useRecoveryCode ? 'Use an authenticator code instead' : 'Use a recovery code instead'}
              </button>
            </form>
          </>
        ) : (
          <>
            <span className="flex h-11 w-11 items-center justify-center rounded-xl bg-brand-600 text-white shadow-sm shadow-brand-600/30">
              <KeyRound className="h-5 w-5" aria-hidden="true" />
            </span>
            <h1 className="mt-4 text-2xl">Log in to DocuMind AI</h1>
            <p className="mt-2 text-sm text-slate-600 dark:text-slate-400">
              Don't have an account?{' '}
              <Link to="/register" className="font-medium text-brand-600 hover:text-brand-700 dark:text-brand-400 dark:hover:text-brand-300">
                Create one
              </Link>
            </p>

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
              <div className="flex flex-col gap-1.5">
                <TextField
                  label="Password"
                  type="password"
                  autoComplete="current-password"
                  required
                  value={password}
                  onChange={(e) => setPassword(e.target.value)}
                />
                <Link
                  to="/forgot-password"
                  className="self-end text-xs font-medium text-brand-600 hover:text-brand-700 dark:text-brand-400 dark:hover:text-brand-300"
                >
                  Forgot password?
                </Link>
              </div>

              <Button type="submit" size="lg" isLoading={isSubmitting} className="mt-2">
                {isSubmitting ? 'Logging in...' : 'Log in'}
              </Button>
            </form>
          </>
        )}
      </div>
    </main>
  )
}
