import { useEffect, useRef, useState } from 'react'
import { LogOut, Settings } from 'lucide-react'
import { Link } from 'react-router-dom'
import { Avatar } from '../ui/Avatar'
import type { CurrentUser } from '../../types/api'

export function ProfileMenu({
  user,
  avatarVersion,
  onLogout,
}: {
  user: CurrentUser
  avatarVersion: number
  onLogout: () => void
}) {
  const [isOpen, setIsOpen] = useState(false)
  const containerRef = useRef<HTMLDivElement>(null)

  useEffect(() => {
    if (!isOpen) return

    function handleClickOutside(event: MouseEvent) {
      if (containerRef.current && !containerRef.current.contains(event.target as Node)) {
        setIsOpen(false)
      }
    }
    function handleKeyDown(event: KeyboardEvent) {
      if (event.key === 'Escape') setIsOpen(false)
    }
    document.addEventListener('mousedown', handleClickOutside)
    document.addEventListener('keydown', handleKeyDown)
    return () => {
      document.removeEventListener('mousedown', handleClickOutside)
      document.removeEventListener('keydown', handleKeyDown)
    }
  }, [isOpen])

  return (
    <div ref={containerRef} className="relative">
      <button
        type="button"
        onClick={() => setIsOpen((v) => !v)}
        aria-haspopup="true"
        aria-expanded={isOpen}
        className="flex items-center gap-2 rounded-full py-1 pl-1 pr-2 transition-colors hover:bg-slate-100 dark:hover:bg-slate-800"
      >
        <Avatar user={user} avatarVersion={avatarVersion} size="sm" />
        <span className="hidden max-w-[10rem] truncate text-sm font-medium text-slate-700 dark:text-slate-300 lg:inline">
          {user.displayName}
        </span>
      </button>

      {isOpen && (
        <div
          role="menu"
          className="animate-scale-in absolute right-0 top-full mt-2 w-56 origin-top-right rounded-xl border border-slate-200 bg-white p-1.5 shadow-lg shadow-slate-200/60 dark:border-slate-700 dark:bg-slate-800 dark:shadow-black/30"
        >
          <div className="px-3 py-2">
            <p className="truncate text-sm font-medium text-slate-900 dark:text-white">{user.displayName}</p>
            <p className="truncate text-xs text-slate-500 dark:text-slate-400">{user.email}</p>
          </div>
          <div className="my-1 border-t border-slate-100 dark:border-slate-700" />
          <Link
            to="/account"
            role="menuitem"
            onClick={() => setIsOpen(false)}
            className="flex w-full items-center gap-2 rounded-lg px-3 py-2 text-left text-sm text-slate-700 transition-colors hover:bg-slate-50 dark:text-slate-300 dark:hover:bg-slate-700"
          >
            <Settings className="h-4 w-4 text-slate-400 dark:text-slate-500" aria-hidden="true" />
            Account settings
          </Link>
          <button
            type="button"
            role="menuitem"
            onClick={() => {
              setIsOpen(false)
              onLogout()
            }}
            className="flex w-full items-center gap-2 rounded-lg px-3 py-2 text-left text-sm text-slate-700 transition-colors hover:bg-slate-50 dark:text-slate-300 dark:hover:bg-slate-700"
          >
            <LogOut className="h-4 w-4 text-slate-400 dark:text-slate-500" aria-hidden="true" />
            Log out
          </button>
        </div>
      )}
    </div>
  )
}
