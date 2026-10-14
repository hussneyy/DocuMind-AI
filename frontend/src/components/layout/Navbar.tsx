import { Link, useNavigate } from 'react-router-dom'
import { useAuth } from '../../context/AuthContext'
import { Button } from '../ui/Button'

export function Navbar() {
  const { user, logout } = useAuth()
  const navigate = useNavigate()

  async function handleLogout() {
    await logout()
    navigate('/')
  }

  return (
    <header className="sticky top-0 z-40 border-b border-slate-200 bg-white/90 backdrop-blur">
      <nav
        aria-label="Primary"
        className="mx-auto flex max-w-6xl items-center justify-between px-4 py-4 sm:px-6 lg:px-8"
      >
        <Link to="/" className="flex items-center gap-2 text-lg font-semibold text-slate-900">
          <span className="flex h-8 w-8 items-center justify-center rounded-lg bg-brand-600 text-white">
            <svg viewBox="0 0 24 24" fill="none" className="h-5 w-5" aria-hidden="true">
              <path
                d="M7 4h7l4 4v12a1 1 0 0 1-1 1H7a1 1 0 0 1-1-1V5a1 1 0 0 1 1-1Z"
                stroke="currentColor"
                strokeWidth="1.6"
              />
              <path d="M9 10h6M9 13h6M9 16h3" stroke="currentColor" strokeWidth="1.6" strokeLinecap="round" />
            </svg>
          </span>
          DocuMind AI
        </Link>

        {!user && (
          <div className="hidden items-center gap-8 md:flex">
            <a href="/#features" className="text-sm font-medium text-slate-600 hover:text-slate-900">
              Features
            </a>
            <a href="/#how-it-works" className="text-sm font-medium text-slate-600 hover:text-slate-900">
              How it works
            </a>
            <a href="/#privacy" className="text-sm font-medium text-slate-600 hover:text-slate-900">
              Privacy
            </a>
          </div>
        )}

        <div className="flex items-center gap-3">
          {user ? (
            <>
              <Link to="/documents" className="text-sm font-medium text-slate-600 hover:text-slate-900">
                My Documents
              </Link>
              <span className="hidden text-sm text-slate-500 sm:inline">{user.displayName}</span>
              <Button variant="ghost" size="md" onClick={handleLogout}>
                Log out
              </Button>
            </>
          ) : (
            <>
              <Button to="/login" variant="ghost" size="md">
                Log in
              </Button>
              <Button to="/register" variant="primary" size="md">
                Start Searching
              </Button>
            </>
          )}
        </div>
      </nav>
    </header>
  )
}
