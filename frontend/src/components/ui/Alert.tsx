import type { ReactNode } from 'react'

interface AlertProps {
  variant?: 'error' | 'success' | 'info'
  children: ReactNode
}

const variantClasses = {
  error: 'bg-red-50 text-red-700 border-red-200',
  success: 'bg-emerald-50 text-emerald-700 border-emerald-200',
  info: 'bg-brand-50 text-brand-700 border-brand-200',
}

export function Alert({ variant = 'info', children }: AlertProps) {
  return (
    <div role="alert" className={`rounded-lg border px-4 py-3 text-sm ${variantClasses[variant]}`}>
      {children}
    </div>
  )
}
