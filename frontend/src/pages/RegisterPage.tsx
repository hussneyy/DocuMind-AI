import axios from 'axios'
import { useState } from 'react'
import type { FormEvent } from 'react'
import { Sparkles } from 'lucide-react'
import { Link, useNavigate } from 'react-router-dom'
import { Alert } from '../components/ui/Alert'
import { Button } from '../components/ui/Button'
import { TextField } from '../components/ui/TextField'
import { COUNTRIES } from '../data/countries'
import { LANGUAGES } from '../i18n/translations'
import { useAuth } from '../context/AuthContext'
import { useLanguage } from '../context/LanguageContext'
import { extractErrorMessages } from '../lib/apiClient'

export function RegisterPage() {
  const { register } = useAuth()
  const { t, language, setLanguage } = useLanguage()
  const navigate = useNavigate()

  const [firstName, setFirstName] = useState('')
  const [lastName, setLastName] = useState('')
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [confirmPassword, setConfirmPassword] = useState('')
  const [country, setCountry] = useState('')
  const [phoneDigits, setPhoneDigits] = useState('')
  const [errors, setErrors] = useState<string[]>([])
  const [isSubmitting, setIsSubmitting] = useState(false)

  const dialCode = COUNTRIES.find((c) => c.code === country)?.dialCode ?? ''

  async function handleSubmit(event: FormEvent) {
    event.preventDefault()
    setErrors([])

    if (password !== confirmPassword) {
      setErrors([t('auth.register.passwordsDoNotMatch')])
      return
    }

    setIsSubmitting(true)
    try {
      const phoneNumber = `${dialCode} ${phoneDigits.trim()}`.trim()
      await register({ email, password, firstName, lastName, country, phoneNumber, preferredLanguage: language })
      navigate('/documents')
    } catch (error) {
      setErrors(axios.isAxiosError(error) ? extractErrorMessages(error) : [t('auth.register.registrationFailed')])
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
          <Sparkles className="h-5 w-5" aria-hidden="true" />
        </span>
        <h1 className="mt-4 text-2xl">{t('auth.register.title')}</h1>
        <p className="mt-2 text-sm text-slate-600 dark:text-slate-400">
          {t('auth.register.haveAccount')}{' '}
          <Link to="/login" className="font-medium text-brand-600 hover:text-brand-700 dark:text-brand-400 dark:hover:text-brand-300">
            {t('auth.register.logIn')}
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

          <div className="grid grid-cols-2 gap-3">
            <TextField
              label={t('auth.register.firstName')}
              type="text"
              autoComplete="given-name"
              required
              value={firstName}
              onChange={(e) => setFirstName(e.target.value)}
            />
            <TextField
              label={t('auth.register.lastName')}
              type="text"
              autoComplete="family-name"
              required
              value={lastName}
              onChange={(e) => setLastName(e.target.value)}
            />
          </div>

          <TextField
            label={t('auth.register.email')}
            type="email"
            autoComplete="email"
            required
            value={email}
            onChange={(e) => setEmail(e.target.value)}
          />
          <TextField
            label={t('auth.register.password')}
            type="password"
            autoComplete="new-password"
            required
            minLength={8}
            value={password}
            onChange={(e) => setPassword(e.target.value)}
          />
          <TextField
            label={t('auth.register.confirmPassword')}
            type="password"
            autoComplete="new-password"
            required
            minLength={8}
            value={confirmPassword}
            onChange={(e) => setConfirmPassword(e.target.value)}
          />
          <p className="text-xs text-slate-500 dark:text-slate-400">{t('auth.register.passwordHint')}</p>

          <div className="flex flex-col gap-1.5">
            <label htmlFor="register-country" className="text-sm font-medium text-slate-700 dark:text-slate-300">
              {t('auth.register.country')}
            </label>
            <select
              id="register-country"
              required
              value={country}
              onChange={(e) => setCountry(e.target.value)}
              className="w-full rounded-lg border border-slate-300 px-3 py-2 text-sm text-slate-900 shadow-sm transition-colors focus:border-brand-600 focus:outline-none focus:ring-2 focus:ring-brand-600 dark:border-slate-600 dark:bg-slate-800 dark:text-white"
            >
              <option value="" disabled>
                {t('auth.register.selectCountry')}
              </option>
              {COUNTRIES.map((c) => (
                <option key={c.code} value={c.code}>
                  {c.dialCode} {c.name}
                </option>
              ))}
            </select>
          </div>

          <div className="flex flex-col gap-1.5">
            <label htmlFor="register-phone" className="text-sm font-medium text-slate-700 dark:text-slate-300">
              {t('auth.register.phoneNumber')}
            </label>
            <div className="flex gap-2">
              <span
                id="register-phone-dial-code"
                className="flex shrink-0 items-center justify-center rounded-lg border border-slate-300 bg-slate-50 px-3 text-sm text-slate-600 dark:border-slate-600 dark:bg-slate-700 dark:text-slate-300"
              >
                {dialCode || '+--'}
              </span>
              <input
                id="register-phone"
                type="tel"
                autoComplete="tel-national"
                required
                aria-describedby="register-phone-dial-code"
                value={phoneDigits}
                onChange={(e) => setPhoneDigits(e.target.value)}
                placeholder="7911 123456"
                className="w-full rounded-lg border border-slate-300 px-3 py-2 text-sm text-slate-900 shadow-sm transition-colors placeholder:text-slate-400 focus:border-brand-600 focus:outline-none focus:ring-2 focus:ring-brand-600 dark:border-slate-600 dark:bg-slate-800 dark:text-white dark:placeholder:text-slate-500"
              />
            </div>
          </div>

          <div className="flex flex-col gap-1.5">
            <label htmlFor="register-language" className="text-sm font-medium text-slate-700 dark:text-slate-300">
              {t('auth.register.language')}
            </label>
            <select
              id="register-language"
              required
              value={language}
              onChange={(e) => setLanguage(e.target.value as typeof language)}
              className="w-full rounded-lg border border-slate-300 px-3 py-2 text-sm text-slate-900 shadow-sm transition-colors focus:border-brand-600 focus:outline-none focus:ring-2 focus:ring-brand-600 dark:border-slate-600 dark:bg-slate-800 dark:text-white"
            >
              {LANGUAGES.map((l) => (
                <option key={l.code} value={l.code}>
                  {l.label}
                </option>
              ))}
            </select>
          </div>

          <Button type="submit" size="lg" isLoading={isSubmitting} className="mt-2">
            {isSubmitting ? t('auth.register.creatingAccount') : t('auth.register.createAccount')}
          </Button>
        </form>
      </div>
    </main>
  )
}
