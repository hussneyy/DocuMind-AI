import { createContext, useCallback, useContext, useEffect, useState } from 'react'
import type { ReactNode } from 'react'
import { translations } from '../i18n/translations'
import type { Language } from '../i18n/translations'

const STORAGE_KEY = 'documind-language'
const SUPPORTED: Language[] = ['en', 'es', 'fr', 'de']

interface LanguageContextValue {
  language: Language
  setLanguage: (language: Language) => void
  /** Looks up a dot-path key (e.g. "auth.login.title") and substitutes any {placeholders}. */
  t: (key: string, params?: Record<string, string | number>) => string
}

const LanguageContext = createContext<LanguageContextValue | undefined>(undefined)

function resolve(dict: Record<string, unknown>, key: string): string | undefined {
  let current: unknown = dict
  for (const part of key.split('.')) {
    if (typeof current !== 'object' || current === null) return undefined
    current = (current as Record<string, unknown>)[part]
  }
  return typeof current === 'string' ? current : undefined
}

function getInitialLanguage(): Language {
  try {
    const stored = localStorage.getItem(STORAGE_KEY)
    if (stored && SUPPORTED.includes(stored as Language)) {
      return stored as Language
    }
  } catch {
    // Ignore — localStorage can be unavailable.
  }
  return 'en'
}

export function LanguageProvider({ children }: { children: ReactNode }) {
  const [language, setLanguageState] = useState<Language>(getInitialLanguage)

  useEffect(() => {
    document.documentElement.lang = language
    try {
      localStorage.setItem(STORAGE_KEY, language)
    } catch {
      // Ignore — localStorage can be unavailable.
    }
  }, [language])

  const setLanguage = useCallback((next: Language) => {
    setLanguageState(next)
  }, [])

  const t = useCallback(
    (key: string, params?: Record<string, string | number>) => {
      const value = resolve(translations[language], key) ?? resolve(translations.en, key) ?? key
      if (!params) return value
      return Object.entries(params).reduce((text, [name, replacement]) => text.replaceAll(`{${name}}`, String(replacement)), value)
    },
    [language],
  )

  return <LanguageContext.Provider value={{ language, setLanguage, t }}>{children}</LanguageContext.Provider>
}

export function useLanguage(): LanguageContextValue {
  const context = useContext(LanguageContext)
  if (!context) {
    throw new Error('useLanguage must be used within a LanguageProvider')
  }
  return context
}
