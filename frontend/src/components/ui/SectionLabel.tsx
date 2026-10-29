import type { ReactNode } from 'react'

export function SectionLabel({ children }: { children: ReactNode }) {
  return (
    <p className="inline-flex items-center gap-2 text-xs font-semibold uppercase tracking-wider text-brand-600">
      <span className="h-1.5 w-1.5 rounded-full bg-brand-500" aria-hidden="true" />
      {children}
    </p>
  )
}
