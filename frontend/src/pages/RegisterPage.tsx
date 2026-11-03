import axios from 'axios'
import { useRef, useState } from 'react'
import type { FormEvent } from 'react'
import { ChevronDown, Sparkles, UserRound } from 'lucide-react'
import { Link, useNavigate } from 'react-router-dom'
import { Alert } from '../components/ui/Alert'
import { Button } from '../components/ui/Button'
import { TextField } from '../components/ui/TextField'
import { useAuth } from '../context/AuthContext'
import { extractErrorMessages } from '../lib/apiClient'

export function RegisterPage() {
  const { register, uploadAvatar } = useAuth()
  const navigate = useNavigate()

  const [displayName, setDisplayName] = useState('')
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [errors, setErrors] = useState<string[]>([])
  const [isSubmitting, setIsSubmitting] = useState(false)

  const [showMoreDetails, setShowMoreDetails] = useState(false)
  const [bio, setBio] = useState('')
  const [location, setLocation] = useState('')
  const [phoneNumber, setPhoneNumber] = useState('')
  const [avatarFile, setAvatarFile] = useState<File | null>(null)
  const [avatarPreviewUrl, setAvatarPreviewUrl] = useState<string | null>(null)
  const fileInputRef = useRef<HTMLInputElement>(null)

  function handleAvatarSelected(file: File) {
    setAvatarFile(file)
    setAvatarPreviewUrl((previous) => {
      if (previous) URL.revokeObjectURL(previous)
      return URL.createObjectURL(file)
    })
  }

  async function handleSubmit(event: FormEvent) {
    event.preventDefault()
    setErrors([])
    setIsSubmitting(true)
    try {
      await register({
        email,
        password,
        displayName,
        bio: bio.trim(),
        location: location.trim(),
        phoneNumber: phoneNumber.trim(),
      })
      if (avatarFile) {
        // Registration already signed the account in, so this follow-up upload is authorized.
        // A failure here shouldn't block onboarding — the photo can always be added later.
        await uploadAvatar(avatarFile).catch(() => {})
      }
      navigate('/documents')
    } catch (error) {
      setErrors(axios.isAxiosError(error) ? extractErrorMessages(error) : ['Registration failed.'])
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
        <h1 className="mt-4 text-2xl">Create your account</h1>
        <p className="mt-2 text-sm text-slate-600 dark:text-slate-400">
          Already have an account?{' '}
          <Link to="/login" className="font-medium text-brand-600 hover:text-brand-700 dark:text-brand-400 dark:hover:text-brand-300">
            Log in
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
            label="Full name"
            type="text"
            autoComplete="name"
            required
            value={displayName}
            onChange={(e) => setDisplayName(e.target.value)}
          />
          <TextField
            label="Email"
            type="email"
            autoComplete="email"
            required
            value={email}
            onChange={(e) => setEmail(e.target.value)}
          />
          <TextField
            label="Password"
            type="password"
            autoComplete="new-password"
            required
            minLength={8}
            value={password}
            onChange={(e) => setPassword(e.target.value)}
          />
          <p className="text-xs text-slate-500 dark:text-slate-400">At least 8 characters.</p>

          <button
            type="button"
            onClick={() => setShowMoreDetails((v) => !v)}
            aria-expanded={showMoreDetails}
            className="flex items-center justify-between rounded-lg border border-dashed border-slate-300 px-3 py-2 text-left text-sm font-medium text-slate-600 transition-colors hover:border-brand-300 hover:text-brand-700 dark:border-slate-600 dark:text-slate-400 dark:hover:border-brand-500/50 dark:hover:text-brand-300"
          >
            Add more details (optional)
            <ChevronDown className={`h-4 w-4 transition-transform ${showMoreDetails ? 'rotate-180' : ''}`} aria-hidden="true" />
          </button>

          {showMoreDetails && (
            <div className="animate-fade-in flex flex-col gap-4 rounded-lg bg-slate-50 p-4 dark:bg-slate-900/50">
              <div className="flex items-center gap-3">
                {avatarPreviewUrl ? (
                  <img src={avatarPreviewUrl} alt="" className="h-14 w-14 shrink-0 rounded-full object-cover" />
                ) : (
                  <span className="flex h-14 w-14 shrink-0 items-center justify-center rounded-full bg-slate-200 text-slate-400 dark:bg-slate-700 dark:text-slate-500">
                    <UserRound className="h-6 w-6" aria-hidden="true" />
                  </span>
                )}
                <div>
                  <Button type="button" variant="secondary" size="md" onClick={() => fileInputRef.current?.click()}>
                    {avatarFile ? 'Change photo' : 'Add a photo'}
                  </Button>
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

              <div className="flex flex-col gap-1.5">
                <label htmlFor="register-bio" className="text-sm font-medium text-slate-700 dark:text-slate-300">
                  Bio
                </label>
                <textarea
                  id="register-bio"
                  rows={2}
                  maxLength={280}
                  value={bio}
                  onChange={(e) => setBio(e.target.value)}
                  placeholder="A short bio about yourself"
                  className="w-full resize-none rounded-lg border border-slate-300 px-3 py-2 text-sm text-slate-900 shadow-sm transition-colors placeholder:text-slate-400 focus:border-brand-600 focus:outline-none focus:ring-2 focus:ring-brand-600 dark:border-slate-600 dark:bg-slate-800 dark:text-white dark:placeholder:text-slate-500"
                />
              </div>

              <TextField
                label="Location"
                type="text"
                maxLength={100}
                placeholder="City, Country"
                value={location}
                onChange={(e) => setLocation(e.target.value)}
              />

              <TextField
                label="Phone number"
                type="tel"
                maxLength={30}
                placeholder="+1 555 123 4567"
                value={phoneNumber}
                onChange={(e) => setPhoneNumber(e.target.value)}
              />
            </div>
          )}

          <Button type="submit" size="lg" isLoading={isSubmitting} className="mt-2">
            {isSubmitting ? 'Creating account...' : 'Create account'}
          </Button>
        </form>
      </div>
    </main>
  )
}
