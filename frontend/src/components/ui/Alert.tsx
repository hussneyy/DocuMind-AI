import type { ReactNode } from 'react'
import { AlertTriangle, CheckCircle2, Info } from 'lucide-react'

interface AlertProps {
  variant?: 'error' | 'success' | 'info'
  children: ReactNode
}

const variantConfig = {
  error: {
    classes: 'bg-red-50 text-red-700 border-red-200 dark:bg-red-500/10 dark:text-red-400 dark:border-red-500/30',
    icon: AlertTriangle,
    iconClass: 'text-red-500 dark:text-red-400',
  },
  success: {
    classes:
      'bg-emerald-50 text-emerald-700 border-emerald-200 dark:bg-emerald-500/10 dark:text-emerald-400 dark:border-emerald-500/30',
    icon: CheckCircle2,
    iconClass: 'text-emerald-500 dark:text-emerald-400',
  },
  info: {
    classes: 'bg-brand-50 text-brand-700 border-brand-200 dark:bg-brand-500/10 dark:text-brand-300 dark:border-brand-500/30',
    icon: Info,
    iconClass: 'text-brand-500 dark:text-brand-400',
  },
}

export function Alert({ variant = 'info', children }: AlertProps) {
  const { classes, icon: Icon, iconClass } = variantConfig[variant]

  return (
    <div role="alert" className={`animate-fade-in flex gap-2.5 rounded-lg border px-4 py-3 text-sm ${classes}`}>
      <Icon className={`mt-0.5 h-4 w-4 shrink-0 ${iconClass}`} aria-hidden="true" />
      <div className="min-w-0 flex-1">{children}</div>
    </div>
  )
}
