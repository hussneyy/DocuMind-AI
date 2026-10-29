import axios from 'axios'
import { useEffect, useState } from 'react'
import { ArrowLeft, ChevronLeft, ChevronRight, FileText, RefreshCw, Sparkles } from 'lucide-react'
import { Link, useParams, useSearchParams } from 'react-router-dom'
import { Alert } from '../components/ui/Alert'
import { Button } from '../components/ui/Button'
import { extractErrorMessages } from '../lib/apiClient'
import * as documentsApi from '../lib/api/documents'
import { SummaryType } from '../types/api'
import type { DocumentDto, SummaryDto } from '../types/api'

const SUMMARY_TABS: { label: string; value: SummaryType }[] = [
  { label: 'Short', value: SummaryType.Short },
  { label: 'Detailed', value: SummaryType.Detailed },
  { label: 'Key Points', value: SummaryType.KeyPoints },
]

export function DocumentViewerPage() {
  const { id } = useParams<{ id: string }>()
  const [searchParams, setSearchParams] = useSearchParams()
  const page = Math.max(1, Number(searchParams.get('page') ?? '1') || 1)

  const [doc, setDoc] = useState<DocumentDto | null>(null)
  const [loadError, setLoadError] = useState<string | null>(null)

  const [summaryTab, setSummaryTab] = useState<SummaryType>(SummaryType.Short)
  const [summary, setSummary] = useState<SummaryDto | null>(null)
  const [isLoadingSummary, setIsLoadingSummary] = useState(false)
  const [summaryError, setSummaryError] = useState<string | null>(null)
  const [isRegenerating, setIsRegenerating] = useState(false)

  useEffect(() => {
    if (!id) return
    documentsApi
      .getDocument(id)
      .then(setDoc)
      .catch(() => setLoadError('This document could not be found, or is no longer yours to view.'))
  }, [id])

  useEffect(() => {
    if (!id) return
    setIsLoadingSummary(true)
    setSummaryError(null)
    documentsApi
      .getSummary(id, summaryTab)
      .then(setSummary)
      .catch((error) => {
        setSummary(null)
        setSummaryError(axios.isAxiosError(error) ? extractErrorMessages(error)[0] : 'Failed to load summary.')
      })
      .finally(() => setIsLoadingSummary(false))
  }, [id, summaryTab])

  async function handleRegenerate() {
    if (!id) return
    setIsRegenerating(true)
    setSummaryError(null)
    try {
      setSummary(await documentsApi.getSummary(id, summaryTab, true))
    } catch (error) {
      setSummaryError(axios.isAxiosError(error) ? extractErrorMessages(error)[0] : 'Failed to regenerate summary.')
    } finally {
      setIsRegenerating(false)
    }
  }

  function goToPage(next: number) {
    const maxPage = doc?.pageCount ?? undefined
    const clamped = Math.max(1, maxPage ? Math.min(next, maxPage) : next)
    setSearchParams({ page: String(clamped) })
  }

  if (!id) return null

  return (
    <main className="mx-auto max-w-6xl px-4 py-8 sm:px-6 lg:px-8">
      <Link to="/documents" className="inline-flex items-center gap-1 text-sm text-brand-600 transition-colors hover:text-brand-700">
        <ArrowLeft className="h-3.5 w-3.5" aria-hidden="true" />
        Back to documents
      </Link>
      <h1 className="mt-2 flex items-center gap-2 text-2xl">
        <FileText className="h-5 w-5 shrink-0 text-slate-400" aria-hidden="true" />
        <span className="truncate">{doc?.originalFileName ?? 'Document'}</span>
      </h1>

      {loadError && (
        <div className="mt-4">
          <Alert variant="error">{loadError}</Alert>
        </div>
      )}

      {!loadError && (
        <div className="animate-fade-in mt-6 grid grid-cols-1 gap-6 lg:grid-cols-[2fr_1fr]">
          <div className="flex flex-col gap-2">
            <div className="flex items-center gap-2">
              <Button
                variant="secondary"
                size="md"
                disabled={page <= 1}
                onClick={() => goToPage(page - 1)}
                leftIcon={<ChevronLeft className="h-4 w-4" aria-hidden="true" />}
              >
                Prev
              </Button>
              <span className="text-sm text-slate-600">
                Page {page}
                {doc?.pageCount ? ` of ${doc.pageCount}` : ''}
              </span>
              <Button
                variant="secondary"
                size="md"
                disabled={doc?.pageCount ? page >= doc.pageCount : false}
                onClick={() => goToPage(page + 1)}
                rightIcon={<ChevronRight className="h-4 w-4" aria-hidden="true" />}
              >
                Next
              </Button>
            </div>
            <iframe
              key={`${id}-${page}`}
              title="Document source"
              src={`${documentsApi.getFileUrl(id)}#page=${page}`}
              className="h-[75vh] w-full rounded-xl border border-slate-200 shadow-sm"
            />
          </div>

          <div>
            <div className="flex gap-2">
              {SUMMARY_TABS.map((tab) => (
                <button
                  key={tab.value}
                  type="button"
                  onClick={() => setSummaryTab(tab.value)}
                  className={`rounded-full px-3 py-1 text-xs font-medium transition-all ${
                    summaryTab === tab.value ? 'bg-brand-600 text-white shadow-sm shadow-brand-600/20' : 'bg-slate-100 text-slate-600 hover:bg-slate-200'
                  }`}
                >
                  {tab.label}
                </button>
              ))}
            </div>

            <div className="mt-3 rounded-xl border border-slate-200 bg-white p-4 shadow-sm">
              {summaryError && <Alert variant="error">{summaryError}</Alert>}

              {!summaryError && isLoadingSummary && (
                <div className="flex flex-col gap-2" aria-busy="true" aria-live="polite">
                  <div className="h-3 w-full animate-pulse rounded bg-slate-100" />
                  <div className="h-3 w-5/6 animate-pulse rounded bg-slate-100" />
                  <div className="h-3 w-2/3 animate-pulse rounded bg-slate-100" />
                </div>
              )}

              {!summaryError && !isLoadingSummary && summary && (
                <div className="animate-fade-in">
                  <p className="flex items-center gap-1.5 text-xs font-medium text-brand-600">
                    <Sparkles className="h-3.5 w-3.5" aria-hidden="true" />
                    AI summary
                  </p>
                  <p className="mt-2 whitespace-pre-wrap text-sm text-slate-700">{summary.content}</p>
                  <div className="mt-3 flex items-center justify-between gap-2 border-t border-slate-100 pt-3 text-xs text-slate-500">
                    <span>
                      Generated by {summary.generatedByModel} from {summary.sourceChunkCount} passage
                      {summary.sourceChunkCount === 1 ? '' : 's'}
                    </span>
                    <Button
                      variant="ghost"
                      size="md"
                      isLoading={isRegenerating}
                      onClick={handleRegenerate}
                      leftIcon={!isRegenerating ? <RefreshCw className="h-3.5 w-3.5" aria-hidden="true" /> : undefined}
                    >
                      {isRegenerating ? 'Regenerating...' : 'Regenerate'}
                    </Button>
                  </div>
                </div>
              )}
            </div>
          </div>
        </div>
      )}
    </main>
  )
}
