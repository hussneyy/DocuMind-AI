import type { ReactNode } from 'react'

export function SectionLabel({ children }: { children: ReactNode }) {
  return (
    <p className="inline-flex items-center gap-2 text-xs font-semibold uppercase tracking-wider text-brand-600 dark:text-brand-400">
      <span className="h-1.5 w-1.5 rounded-full bg-brand-500 dark:bg-brand-400" aria-hidden="true" />
      {children}
    </p>
  )
}
