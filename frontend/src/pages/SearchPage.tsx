import axios from 'axios'
import { useEffect, useState } from 'react'
import type { FormEvent } from 'react'
import { Link } from 'react-router-dom'
import { Alert } from '../components/ui/Alert'
import { Button } from '../components/ui/Button'
import { extractErrorMessages } from '../lib/apiClient'
import * as documentsApi from '../lib/api/documents'
import * as searchApi from '../lib/api/search'
import { DocumentProcessingStatus, SearchMode } from '../types/api'
import type { DocumentDto, SearchResultItem } from '../types/api'

const MODE_OPTIONS: { label: string; value: SearchMode; description: string }[] = [
  { label: 'Hybrid', value: SearchMode.Hybrid, description: 'Combines semantic and keyword matching (recommended)' },
  { label: 'Semantic', value: SearchMode.Semantic, description: 'Matches by meaning, even without shared words' },
  { label: 'Keyword', value: SearchMode.Keyword, description: 'Matches exact words and phrases' },
]

export function SearchPage() {
  const [query, setQuery] = useState('')
  const [mode, setMode] = useState<SearchMode>(SearchMode.Hybrid)
  const [documents, setDocuments] = useState<DocumentDto[]>([])
  const [selectedDocumentIds, setSelectedDocumentIds] = useState<Set<string>>(new Set())
  const [showScopePicker, setShowScopePicker] = useState(false)

  const [results, setResults] = useState<SearchResultItem[] | null>(null)
  const [isSearching, setIsSearching] = useState(false)
  const [errors, setErrors] = useState<string[]>([])
  const [hasSearched, setHasSearched] = useState(false)

  useEffect(() => {
    documentsApi
      .listDocuments({ page: 1, pageSize: 100, status: DocumentProcessingStatus.Indexed, sortBy: 'name', sortDesc: false })
      .then((result) => setDocuments(result.items))
      .catch(() => setDocuments([]))
  }, [])

  function toggleDocument(id: string) {
    setSelectedDocumentIds((prev) => {
      const next = new Set(prev)
      if (next.has(id)) next.delete(id)
      else next.add(id)
      return next
    })
  }

  async function handleSubmit(event: FormEvent) {
    event.preventDefault()
    if (!query.trim()) return

    setIsSearching(true)
    setErrors([])
    setHasSearched(true)
    try {
      const items = await searchApi.search({
        query,
        mode,
        documentIds: selectedDocumentIds.size > 0 ? [...selectedDocumentIds] : undefined,
      })
      setResults(items)
    } catch (error) {
      setResults(null)
      setErrors(axios.isAxiosError(error) ? extractErrorMessages(error) : ['Search failed.'])
    } finally {
      setIsSearching(false)
    }
  }

  return (
    <main className="mx-auto max-w-4xl px-4 py-10 sm:px-6 lg:px-8">
      <h1 className="text-2xl">Search your documents</h1>
      <p className="mt-1 text-sm text-slate-600">
        Ask a question or search for a term across your indexed documents.
      </p>

      <form onSubmit={handleSubmit} className="mt-6 flex flex-col gap-4">
        <div className="flex flex-col gap-2 sm:flex-row">
          <input
            type="search"
            value={query}
            onChange={(e) => setQuery(e.target.value)}
            placeholder="e.g. What is the exam registration deadline?"
            className="w-full rounded-lg border border-slate-300 px-4 py-2.5 text-sm focus:border-brand-600 focus:outline-none focus:ring-2 focus:ring-brand-600"
            aria-label="Search query"
          />
          <Button type="submit" size="lg" disabled={isSearching || !query.trim()}>
            {isSearching ? 'Searching...' : 'Search'}
          </Button>
        </div>

        <div className="flex flex-wrap items-center gap-2">
          {MODE_OPTIONS.map((option) => (
            <button
              key={option.value}
              type="button"
              title={option.description}
              onClick={() => setMode(option.value)}
              className={`rounded-full px-3 py-1 text-xs font-medium transition-colors ${
                mode === option.value
                  ? 'bg-brand-600 text-white'
                  : 'bg-slate-100 text-slate-600 hover:bg-slate-200'
              }`}
            >
              {option.label}
            </button>
          ))}

          <span className="mx-1 h-4 w-px bg-slate-200" aria-hidden="true" />

          <button
            type="button"
            onClick={() => setShowScopePicker((v) => !v)}
            className="text-xs font-medium text-brand-600 hover:text-brand-700"
          >
            {selectedDocumentIds.size === 0
              ? 'Searching all documents'
              : `Searching ${selectedDocumentIds.size} selected document${selectedDocumentIds.size === 1 ? '' : 's'}`}
            {' '}
            &middot; {showScopePicker ? 'Hide' : 'Change'}
          </button>
        </div>

        {showScopePicker && (
          <div className="rounded-lg border border-slate-200 p-3">
            {documents.length === 0 ? (
              <p className="text-sm text-slate-500">No indexed documents yet.</p>
            ) : (
              <ul className="flex max-h-48 flex-col gap-1 overflow-y-auto">
                {documents.map((doc) => (
                  <li key={doc.id}>
                    <label className="flex items-center gap-2 rounded px-2 py-1 text-sm hover:bg-slate-50">
                      <input
                        type="checkbox"
                        checked={selectedDocumentIds.has(doc.id)}
                        onChange={() => toggleDocument(doc.id)}
                        className="rounded border-slate-300"
                      />
                      {doc.originalFileName}
                    </label>
                  </li>
                ))}
              </ul>
            )}
          </div>
        )}
      </form>

      <div className="mt-8">
        {errors.length > 0 && (
          <Alert variant="error">
            <ul className="list-inside list-disc">
              {errors.map((error) => (
                <li key={error}>{error}</li>
              ))}
            </ul>
          </Alert>
        )}

        {!errors.length && isSearching && (
          <ul className="flex flex-col gap-3" aria-busy="true" aria-live="polite">
            {[...Array(3)].map((_, i) => (
              <li key={i} className="h-24 animate-pulse rounded-xl bg-slate-100" />
            ))}
          </ul>
        )}

        {!errors.length && !isSearching && hasSearched && results !== null && results.length === 0 && (
          <div className="rounded-xl border border-dashed border-slate-300 py-12 text-center">
            <p className="text-slate-600">
              No reliable answer found in your documents for this query. Try rephrasing, or switch search modes.
            </p>
          </div>
        )}

        {!errors.length && !isSearching && results && results.length > 0 && (
          <ul className="flex flex-col gap-3">
            {results.map((result) => (
              <li key={result.chunkId} className="rounded-xl border border-slate-200 bg-white p-4 shadow-sm">
                <div className="flex items-center justify-between gap-2">
                  <span className="font-medium text-slate-900">{result.documentFileName}</span>
                  <span className="shrink-0 rounded-full bg-slate-100 px-2.5 py-0.5 text-xs font-medium text-slate-600">
                    {result.pageStart === result.pageEnd ? `Page ${result.pageStart}` : `Pages ${result.pageStart}-${result.pageEnd}`}
                  </span>
                </div>
                <p className="mt-2 text-sm text-slate-700">{highlightQuery(result.excerpt, query)}</p>
                <Link
                  to={`/documents/${result.documentId}/view?page=${result.pageStart}`}
                  className="mt-2 inline-block text-xs font-medium text-brand-600 hover:text-brand-700"
                >
                  View source &rarr;
                </Link>
              </li>
            ))}
          </ul>
        )}

        {!hasSearched && (
          <div className="rounded-xl border border-dashed border-slate-300 py-12 text-center">
            <p className="text-slate-600">Enter a question or search term above to get started.</p>
          </div>
        )}
      </div>
    </main>
  )
}

function highlightQuery(text: string, query: string) {
  const words = query.split(/\s+/).filter((w) => w.length > 2)
  if (words.length === 0) return text

  const pattern = new RegExp(`(${words.map(escapeRegExp).join('|')})`, 'gi')
  const parts = text.split(pattern)

  return parts.map((part, i) =>
    words.some((w) => w.toLowerCase() === part.toLowerCase()) ? (
      <mark key={i} className="rounded bg-amber-100 px-0.5 text-inherit">
        {part}
      </mark>
    ) : (
      <span key={i}>{part}</span>
    ),
  )
}

function escapeRegExp(value: string) {
  return value.replace(/[.*+?^${}()|[\]\\]/g, '\\$&')
}
