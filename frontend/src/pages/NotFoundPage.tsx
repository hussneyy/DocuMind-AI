import { CompassIcon } from 'lucide-react'
import { Button } from '../components/ui/Button'

export function NotFoundPage() {
  return (
    <main className="flex min-h-[60vh] flex-col items-center justify-center px-4 text-center">
      <span className="flex h-14 w-14 items-center justify-center rounded-full bg-brand-50 text-brand-600 dark:bg-brand-500/15 dark:text-brand-400">
        <CompassIcon className="h-7 w-7" aria-hidden="true" />
      </span>
      <p className="mt-4 text-sm font-semibold text-brand-600 dark:text-brand-400">404</p>
      <h1 className="mt-2 text-3xl">Page not found</h1>
      <p className="mt-3 max-w-md text-slate-600 dark:text-slate-400">
        This page doesn't exist yet, or has moved. DocuMind AI is under active development.
      </p>
      <Button to="/" className="mt-8">
        Back to home
      </Button>
    </main>
  )
}
