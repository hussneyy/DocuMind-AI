import axios from 'axios'
import { useEffect, useState } from 'react'
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
      <Link to="/documents" className="text-sm text-brand-600 hover:text-brand-700">
        &larr; Back to documents
      </Link>
      <h1 className="mt-1 text-2xl">{doc?.originalFileName ?? 'Document'}</h1>

      {loadError && (
        <div className="mt-4">
          <Alert variant="error">{loadError}</Alert>
        </div>
      )}

      {!loadError && (
        <div className="mt-6 grid grid-cols-1 gap-6 lg:grid-cols-[2fr_1fr]">
          <div className="flex flex-col gap-2">
            <div className="flex items-center gap-2">
              <Button variant="secondary" size="md" disabled={page <= 1} onClick={() => goToPage(page - 1)}>
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
              >
                Next
              </Button>
            </div>
            <iframe
              key={`${id}-${page}`}
              title="Document source"
              src={`${documentsApi.getFileUrl(id)}#page=${page}`}
              className="h-[75vh] w-full rounded-xl border border-slate-200"
            />
          </div>

          <div>
            <div className="flex gap-2">
              {SUMMARY_TABS.map((tab) => (
                <button
                  key={tab.value}
                  type="button"
                  onClick={() => setSummaryTab(tab.value)}
                  className={`rounded-full px-3 py-1 text-xs font-medium transition-colors ${
                    summaryTab === tab.value ? 'bg-brand-600 text-white' : 'bg-slate-100 text-slate-600 hover:bg-slate-200'
                  }`}
                >
                  {tab.label}
                </button>
              ))}
            </div>

            <div className="mt-3 rounded-xl border border-slate-200 bg-white p-4">
              {summaryError && <Alert variant="error">{summaryError}</Alert>}

              {!summaryError && isLoadingSummary && (
                <div className="flex flex-col gap-2" aria-busy="true" aria-live="polite">
                  <div className="h-3 w-full animate-pulse rounded bg-slate-100" />
                  <div className="h-3 w-5/6 animate-pulse rounded bg-slate-100" />
                  <div className="h-3 w-2/3 animate-pulse rounded bg-slate-100" />
                </div>
              )}

              {!summaryError && !isLoadingSummary && summary && (
                <>
                  <p className="whitespace-pre-wrap text-sm text-slate-700">{summary.content}</p>
                  <div className="mt-3 flex items-center justify-between gap-2 border-t border-slate-100 pt-3 text-xs text-slate-400">
                    <span>
                      Generated by {summary.generatedByModel} from {summary.sourceChunkCount} passage
                      {summary.sourceChunkCount === 1 ? '' : 's'}
                    </span>
                    <Button variant="ghost" size="md" disabled={isRegenerating} onClick={handleRegenerate}>
                      {isRegenerating ? 'Regenerating...' : 'Regenerate'}
                    </Button>
                  </div>
                </>
              )}
            </div>
          </div>
        </div>
      )}
    </main>
  )
}
