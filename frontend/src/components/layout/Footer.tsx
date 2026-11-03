import { useLanguage } from '../../context/LanguageContext'

export function Footer() {
  const { t } = useLanguage()

  return (
    <footer className="border-t border-slate-200 bg-slate-50 dark:border-slate-800 dark:bg-slate-900">
      <div className="mx-auto max-w-6xl px-4 py-10 sm:px-6 lg:px-8">
        <div className="flex flex-col items-start justify-between gap-6 md:flex-row md:items-center">
          <div>
            <p className="flex items-center gap-2 text-sm font-semibold text-slate-900 dark:text-white">
              <span className="flex h-5 w-5 items-center justify-center rounded-md bg-brand-600 text-white">
                <svg viewBox="0 0 24 24" fill="none" className="h-3 w-3" aria-hidden="true">
                  <path d="M7 4h7l4 4v12a1 1 0 0 1-1 1H7a1 1 0 0 1-1-1V5a1 1 0 0 1 1-1Z" stroke="currentColor" strokeWidth="2" />
                </svg>
              </span>
              DocuMind AI
            </p>
            <p className="mt-1 max-w-sm text-sm text-slate-500 dark:text-slate-400">{t('footer.tagline')}</p>
          </div>

          <div className="flex flex-wrap gap-x-8 gap-y-2 text-sm text-slate-500 dark:text-slate-400">
            <span className="cursor-default text-slate-500 dark:text-slate-400" title="See the project README once published">
              {t('footer.documentation')}
            </span>
            <span className="cursor-default text-slate-500 dark:text-slate-400" title="See the project README once published">
              {t('footer.sourceCode')}
            </span>
            <a href="#privacy" className="transition-colors hover:text-slate-900 dark:hover:text-white">
              {t('footer.privacyLink')}
            </a>
          </div>
        </div>

        <p className="mt-8 text-xs text-slate-500 dark:text-slate-400">
          {t('footer.copyright', { year: new Date().getFullYear() })}
        </p>
      </div>
    </footer>
  )
}
