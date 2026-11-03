import { useState } from 'react'
import { getAvatarUrl } from '../../lib/api/auth'
import type { CurrentUser } from '../../types/api'

const SIZE_CLASSES = {
  sm: 'h-8 w-8 text-xs',
  md: 'h-11 w-11 text-sm',
  lg: 'h-20 w-20 text-xl',
} as const

function initialsOf(name: string): string {
  const parts = name.trim().split(/\s+/)
  const first = parts[0]?.[0] ?? ''
  const last = parts.length > 1 ? parts[parts.length - 1][0] : ''
  return (first + last).toUpperCase() || '?'
}

export function Avatar({
  user,
  avatarVersion,
  size = 'md',
  className = '',
}: {
  user: CurrentUser
  avatarVersion: number
  size?: 'sm' | 'md' | 'lg'
  className?: string
}) {
  const [imgFailed, setImgFailed] = useState(false)
  const sizeClasses = SIZE_CLASSES[size]

  if (user.hasAvatar && !imgFailed) {
    return (
      <img
        src={getAvatarUrl(avatarVersion)}
        alt=""
        onError={() => setImgFailed(true)}
        className={`shrink-0 rounded-full object-cover ${sizeClasses} ${className}`}
      />
    )
  }

  return (
    <span
      className={`flex shrink-0 items-center justify-center rounded-full bg-brand-100 font-semibold text-brand-700 dark:bg-brand-500/20 dark:text-brand-300 ${sizeClasses} ${className}`}
    >
      {initialsOf(user.displayName)}
    </span>
  )
}
