import type { ButtonHTMLAttributes, ReactNode } from 'react'
import { Link } from 'react-router-dom'
import { Loader2 } from 'lucide-react'

type Variant = 'primary' | 'secondary' | 'ghost'
type Size = 'md' | 'lg'

const variantClasses: Record<Variant, string> = {
  primary:
    'bg-brand-600 text-white shadow-sm shadow-brand-600/20 hover:bg-brand-700 hover:shadow-md hover:shadow-brand-600/25 focus-visible:ring-brand-600',
  secondary:
    'bg-white text-slate-900 border border-slate-300 hover:border-slate-400 hover:bg-slate-50 focus-visible:ring-brand-600 dark:bg-slate-800 dark:text-white dark:border-slate-600 dark:hover:border-slate-500 dark:hover:bg-slate-700',
  ghost: 'text-slate-700 hover:bg-slate-100 focus-visible:ring-brand-600 dark:text-slate-300 dark:hover:bg-slate-800',
}

const sizeClasses: Record<Size, string> = {
  md: 'px-4 py-2 text-sm',
  lg: 'px-6 py-3 text-base',
}

const baseClasses =
  'inline-flex items-center justify-center gap-2 rounded-lg font-medium transition-all duration-150 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-offset-2 disabled:opacity-50 disabled:cursor-not-allowed active:scale-[0.98]'

interface ButtonProps extends Omit<ButtonHTMLAttributes<HTMLButtonElement>, 'children'> {
  variant?: Variant
  size?: Size
  children: ReactNode
  to?: string
  /** Shows a spinner in place of the leading icon and disables the button — pass instead of (or alongside) `disabled` while an action is in flight. */
  isLoading?: boolean
  leftIcon?: ReactNode
  rightIcon?: ReactNode
}

export function Button({
  variant = 'primary',
  size = 'md',
  children,
  className = '',
  to,
  isLoading = false,
  leftIcon,
  rightIcon,
  disabled,
  ...buttonProps
}: ButtonProps) {
  const classes = `${baseClasses} ${variantClasses[variant]} ${sizeClasses[size]} ${className}`
  const content = (
    <>
      {isLoading ? <Loader2 className="h-4 w-4 animate-spin" aria-hidden="true" /> : leftIcon}
      {children}
      {!isLoading && rightIcon}
    </>
  )

  if (to) {
    if (to.startsWith('#')) {
      return (
        <a href={to} className={classes}>
          {content}
        </a>
      )
    }
    return (
      <Link to={to} className={classes}>
        {content}
      </Link>
    )
  }

  return (
    <button className={classes} disabled={disabled || isLoading} {...buttonProps}>
      {content}
    </button>
  )
}
