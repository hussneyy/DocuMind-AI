import type { ButtonHTMLAttributes, ReactNode } from 'react'
import { Link } from 'react-router-dom'

type Variant = 'primary' | 'secondary' | 'ghost'
type Size = 'md' | 'lg'

const variantClasses: Record<Variant, string> = {
  primary:
    'bg-brand-600 text-white hover:bg-brand-700 focus-visible:ring-brand-600 shadow-sm',
  secondary:
    'bg-white text-slate-900 border border-slate-300 hover:bg-slate-50 focus-visible:ring-brand-600',
  ghost: 'text-slate-700 hover:bg-slate-100 focus-visible:ring-brand-600',
}

const sizeClasses: Record<Size, string> = {
  md: 'px-4 py-2 text-sm',
  lg: 'px-6 py-3 text-base',
}

const baseClasses =
  'inline-flex items-center justify-center gap-2 rounded-lg font-medium transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-offset-2 disabled:opacity-50 disabled:cursor-not-allowed'

interface ButtonProps extends Omit<ButtonHTMLAttributes<HTMLButtonElement>, 'children'> {
  variant?: Variant
  size?: Size
  children: ReactNode
  to?: string
}

export function Button({
  variant = 'primary',
  size = 'md',
  children,
  className = '',
  to,
  ...buttonProps
}: ButtonProps) {
  const classes = `${baseClasses} ${variantClasses[variant]} ${sizeClasses[size]} ${className}`

  if (to) {
    if (to.startsWith('#')) {
      return (
        <a href={to} className={classes}>
          {children}
        </a>
      )
    }
    return (
      <Link to={to} className={classes}>
        {children}
      </Link>
    )
  }

  return (
    <button className={classes} {...buttonProps}>
      {children}
    </button>
  )
}
