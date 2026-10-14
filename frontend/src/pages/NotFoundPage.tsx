import { Button } from '../components/ui/Button'

export function NotFoundPage() {
  return (
    <main className="flex min-h-[60vh] flex-col items-center justify-center px-4 text-center">
      <p className="text-sm font-semibold text-brand-600">404</p>
      <h1 className="mt-2 text-3xl">Page not found</h1>
      <p className="mt-3 max-w-md text-slate-600">
        This page doesn't exist yet, or has moved. DocuMind AI is under active development.
      </p>
      <Button to="/" className="mt-8">
        Back to home
      </Button>
    </main>
  )
}
