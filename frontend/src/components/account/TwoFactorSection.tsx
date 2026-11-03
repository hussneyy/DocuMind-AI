import axios from 'axios'
import { useState } from 'react'
import type { FormEvent } from 'react'
import QRCode from 'qrcode'
import { CheckCircle2, ShieldCheck, ShieldOff } from 'lucide-react'
import { Alert } from '../ui/Alert'
import { Button } from '../ui/Button'
import { TextField } from '../ui/TextField'
import { useAuth } from '../../context/AuthContext'
import { extractErrorMessages } from '../../lib/apiClient'
import * as authApi from '../../lib/api/auth'
import type { CurrentUser } from '../../types/api'

type WizardStep = 'closed' | 'scan' | 'recoveryCodes'

export function TwoFactorSection({ user }: { user: CurrentUser }) {
  const { refreshUser, disableTwoFactor } = useAuth()

  const [step, setStep] = useState<WizardStep>('closed')
  const [sharedKey, setSharedKey] = useState('')
  const [qrDataUrl, setQrDataUrl] = useState('')
  const [code, setCode] = useState('')
  const [recoveryCodes, setRecoveryCodes] = useState<string[]>([])
  const [errors, setErrors] = useState<string[]>([])
  const [isBusy, setIsBusy] = useState(false)

  const [isDisabling, setIsDisabling] = useState(false)
  const [disablePassword, setDisablePassword] = useState('')

  async function handleStartSetup() {
    setErrors([])
    setIsBusy(true)
    try {
      const setup = await authApi.setupTwoFactor()
      setSharedKey(setup.sharedKey)
      setQrDataUrl(await QRCode.toDataURL(setup.authenticatorUri))
      setStep('scan')
    } catch (error) {
      setErrors(axios.isAxiosError(error) ? extractErrorMessages(error) : ['Failed to start two-factor setup.'])
    } finally {
      setIsBusy(false)
    }
  }

  async function handleVerify(event: FormEvent) {
    event.preventDefault()
    setErrors([])
    setIsBusy(true)
    try {
      const codes = await authApi.enableTwoFactor(code.trim())
      setRecoveryCodes(codes)
      setStep('recoveryCodes')
      setCode('')
    } catch (error) {
      setErrors(axios.isAxiosError(error) ? extractErrorMessages(error) : ['Invalid verification code.'])
    } finally {
      setIsBusy(false)
    }
  }

  async function handleFinishSetup() {
    await refreshUser()
    setStep('closed')
    setRecoveryCodes([])
  }

  function handleCancelSetup() {
    setStep('closed')
    setSharedKey('')
    setQrDataUrl('')
    setCode('')
    setErrors([])
  }

  async function handleDisable(event: FormEvent) {
    event.preventDefault()
    setErrors([])
    setIsBusy(true)
    try {
      await disableTwoFactor(disablePassword)
      setIsDisabling(false)
      setDisablePassword('')
    } catch (error) {
      setErrors(axios.isAxiosError(error) ? extractErrorMessages(error) : ['Failed to disable two-factor authentication.'])
    } finally {
      setIsBusy(false)
    }
  }

  return (
    <div className="animate-fade-in-up mt-6 rounded-2xl border border-slate-200 bg-white p-6 shadow-sm dark:border-slate-700 dark:bg-slate-800">
      <h2 className="text-lg">Security</h2>

      {step === 'closed' && (
        <>
          <div className="mt-4 flex items-center justify-between gap-4">
            <div className="flex items-center gap-3">
              <span
                className={`flex h-9 w-9 shrink-0 items-center justify-center rounded-full ${
                  user.twoFactorEnabled
                    ? 'bg-emerald-100 text-emerald-600 dark:bg-emerald-500/15 dark:text-emerald-400'
                    : 'bg-slate-100 text-slate-400 dark:bg-slate-700 dark:text-slate-500'
                }`}
              >
                {user.twoFactorEnabled ? <ShieldCheck className="h-4 w-4" aria-hidden="true" /> : <ShieldOff className="h-4 w-4" aria-hidden="true" />}
              </span>
              <div>
                <p className="text-sm font-medium text-slate-900 dark:text-white">Two-factor authentication</p>
                <p className="text-xs text-slate-500 dark:text-slate-400">
                  {user.twoFactorEnabled ? 'Enabled — an authenticator code is required at login.' : 'Not enabled.'}
                </p>
              </div>
            </div>
            {user.twoFactorEnabled ? (
              <Button type="button" variant="secondary" size="md" onClick={() => setIsDisabling((v) => !v)}>
                Disable
              </Button>
            ) : (
              <Button type="button" variant="secondary" size="md" isLoading={isBusy} onClick={handleStartSetup}>
                Enable
              </Button>
            )}
          </div>

          {errors.length > 0 && (
            <div className="mt-4">
              <Alert variant="error">
                <ul className="list-inside list-disc">
                  {errors.map((error) => (
                    <li key={error}>{error}</li>
                  ))}
                </ul>
              </Alert>
            </div>
          )}

          {isDisabling && (
            <form onSubmit={handleDisable} className="mt-4 flex flex-col gap-3 border-t border-slate-100 pt-4 dark:border-slate-700">
              <TextField
                label="Confirm your password to disable two-factor authentication"
                type="password"
                autoComplete="current-password"
                required
                value={disablePassword}
                onChange={(e) => setDisablePassword(e.target.value)}
              />
              <div className="flex gap-2">
                <Button type="submit" variant="primary" size="md" isLoading={isBusy} className="!bg-red-600 hover:!bg-red-700">
                  {isBusy ? 'Disabling...' : 'Disable two-factor authentication'}
                </Button>
                <Button type="button" variant="ghost" size="md" onClick={() => setIsDisabling(false)}>
                  Cancel
                </Button>
              </div>
            </form>
          )}
        </>
      )}

      {step === 'scan' && (
        <div className="mt-4 flex flex-col gap-4">
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
            Scan this QR code with an authenticator app (Google Authenticator, Authy, 1Password, etc.), or enter the key manually.
          </p>
          <img src={qrDataUrl} alt="Two-factor setup QR code" className="h-44 w-44 self-center rounded-lg border border-slate-200 dark:border-slate-600" />
          <div className="break-all rounded-lg border border-slate-200 bg-slate-50 p-3 text-center font-mono text-xs text-slate-700 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-300">
            {sharedKey}
          </div>

          <form onSubmit={handleVerify} className="flex flex-col gap-3">
            <TextField
              label="Enter the 6-digit code from your app"
              type="text"
              autoComplete="one-time-code"
              required
              value={code}
              onChange={(e) => setCode(e.target.value)}
            />
            <div className="flex gap-2">
              <Button type="submit" size="md" isLoading={isBusy}>
                {isBusy ? 'Verifying...' : 'Verify & enable'}
              </Button>
              <Button type="button" variant="ghost" size="md" onClick={handleCancelSetup}>
                Cancel
              </Button>
            </div>
          </form>
        </div>
      )}

      {step === 'recoveryCodes' && (
        <div className="mt-4 flex flex-col gap-4">
          <Alert variant="success">Two-factor authentication is now enabled.</Alert>
          <div>
            <p className="text-sm font-medium text-slate-900 dark:text-white">Save your recovery codes</p>
            <p className="mt-1 text-xs text-slate-500 dark:text-slate-400">
              Each code can be used once to sign in if you lose access to your authenticator app. They're shown only now.
            </p>
          </div>
          <ul className="grid grid-cols-2 gap-2 rounded-lg border border-slate-200 bg-slate-50 p-3 font-mono text-xs text-slate-700 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-300">
            {recoveryCodes.map((rc) => (
              <li key={rc} className="flex items-center gap-1.5">
                <CheckCircle2 className="h-3 w-3 shrink-0 text-emerald-500" aria-hidden="true" />
                {rc}
              </li>
            ))}
          </ul>
          <Button type="button" size="md" onClick={handleFinishSetup} className="self-start">
            Done
          </Button>
        </div>
      )}
    </div>
  )
}
