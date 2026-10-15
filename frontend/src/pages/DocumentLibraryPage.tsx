import axios from 'axios'
import { useCallback, useEffect, useState } from 'react'
import { Alert } from '../components/ui/Alert'
import { Button } from '../components/ui/Button'
import { ConfirmDialog } from '../components/ui/ConfirmDialog'
import { DocumentStatusBadge } from '../components/documents/DocumentStatusBadge'
import { FileDropzone } from '../components/documents/FileDropzone'
import { extractErrorMessages } from '../lib/apiClient'
import * as documentsApi from '../lib/api/documents'
import { formatDate, formatFileSize } from '../lib/format'
import { DocumentProcessingStatus, type DocumentDto, type PagedResult } from '../types/api'

const PAGE_SIZE = 10

// "Uploaded" isn't included: documents move straight to Queued once the upload completes, so
// that status is never actually observed and would be a dead filter option.
const STATUS_FILTER_OPTIONS: { label: string; value: DocumentProcessingStatus | '' }[] = [
  { label: 'All statuses', value: '' },
  { label: 'Queued', value: DocumentProcessingStatus.Queued },
  { label: 'Processing', value: DocumentProcessingStatus.Processing },
  { label: 'Indexed', value: DocumentProcessingStatus.Indexed },
  { label: 'Failed', value: DocumentProcessingStatus.Failed },
]

export function DocumentLibraryPage() {
  const [result, setResult] = useState<PagedResult<DocumentDto> | null>(null)
  const [isLoading, setIsLoading] = useState(true)
  const [loadError, setLoadError] = useState<string | null>(null)

  const [search, setSearch] = useState('')
  const [statusFilter, setStatusFilter] = useState<DocumentProcessingStatus | ''>('')
  const [sortBy, setSortBy] = useState<'uploadedAt' | 'name'>('uploadedAt')
  const [sortDesc, setSortDesc] = useState(true)
  const [page, setPage] = useState(1)

  const [isUploading, setIsUploading] = useState(false)
  const [uploadErrors, setUploadErrors] = useState<string[]>([])

  const [deleteTarget, setDeleteTarget] = useState<DocumentDto | null>(null)
  const [isDeleting, setIsDeleting] = useState(false)

  const [retryingId, setRetryingId] = useState<string | null>(null)

  const fetchDocuments = useCallback(async () => {
    setIsLoading(true)
    setLoadError(null)
    try {
      const data = await documentsApi.listDocuments({
        page,
        pageSize: PAGE_SIZE,
        search: search || undefined,
        status: statusFilter === '' ? undefined : statusFilter,
        sortBy,
        sortDesc,
      })
      setResult(data)
    } catch (error) {
      setLoadError(axios.isAxiosError(error) ? extractErrorMessages(error)[0] : 'Failed to load documents.')
    } finally {
      setIsLoading(false)
    }
  }, [page, search, statusFilter, sortBy, sortDesc])

  useEffect(() => {
    fetchDocuments()
  }, [fetchDocuments])

  // While any document on this page is still being indexed, keep polling so status updates
  // (Queued -> Processing -> Indexed/Failed) show up without the user refreshing manually.
  useEffect(() => {
    const hasInFlightDocument = (result?.items ?? []).some(
      (doc) =>
        doc.processingStatus === DocumentProcessingStatus.Queued ||
        doc.processingStatus === DocumentProcessingStatus.Processing,
    )
    if (!hasInFlightDocument) return

    const timeoutId = setTimeout(fetchDocuments, 2000)
    return () => clearTimeout(timeoutId)
  }, [result, fetchDocuments])

  async function handleFileSelected(file: File) {
    setUploadErrors([])
    setIsUploading(true)
    try {
      await documentsApi.uploadDocument(file)
      setPage(1)
      await fetchDocuments()
    } catch (error) {
      setUploadErrors(axios.isAxiosError(error) ? extractErrorMessages(error) : ['Upload failed.'])
    } finally {
      setIsUploading(false)
    }
  }

  async function handleRetry(doc: DocumentDto) {
    setRetryingId(doc.id)
    try {
      await documentsApi.retryDocument(doc.id)
      await fetchDocuments()
    } catch (error) {
      setLoadError(axios.isAxiosError(error) ? extractErrorMessages(error)[0] : 'Failed to retry document.')
    } finally {
      setRetryingId(null)
    }
  }

  async function handleConfirmDelete() {
    if (!deleteTarget) return
    setIsDeleting(true)
    try {
      await documentsApi.deleteDocument(deleteTarget.id)
      setDeleteTarget(null)
      await fetchDocuments()
    } catch (error) {
      setLoadError(axios.isAxiosError(error) ? extractErrorMessages(error)[0] : 'Failed to delete document.')
    } finally {
      setIsDeleting(false)
    }
  }

  const documents = result?.items ?? []
  const totalPages = result?.totalPages ?? 1

  return (
    <main className="mx-auto max-w-5xl px-4 py-10 sm:px-6 lg:px-8">
      <div className="flex flex-col gap-1">
        <h1 className="text-2xl">My Documents</h1>
        <p className="text-sm text-slate-600">Upload PDFs to build your searchable library.</p>
      </div>

      <div className="mt-6">
        <FileDropzone onFileSelected={handleFileSelected} disabled={isUploading} />
        {isUploading && <p className="mt-2 text-sm text-slate-500">Uploading...</p>}
        {uploadErrors.length > 0 && (
          <div className="mt-3">
            <Alert variant="error">
              <ul className="list-inside list-disc">
                {uploadErrors.map((error) => (
                  <li key={error}>{error}</li>
                ))}
              </ul>
            </Alert>
          </div>
        )}
      </div>

      <div className="mt-8 flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
        <input
          type="search"
          placeholder="Search by file name..."
          value={search}
          onChange={(e) => {
            setPage(1)
            setSearch(e.target.value)
          }}
          className="w-full rounded-lg border border-slate-300 px-3 py-2 text-sm focus:border-brand-600 focus:outline-none focus:ring-2 focus:ring-brand-600 sm:max-w-xs"
          aria-label="Search documents by file name"
        />

        <div className="flex gap-2">
          <select
            value={statusFilter}
            onChange={(e) => {
              setPage(1)
              setStatusFilter(e.target.value === '' ? '' : (Number(e.target.value) as DocumentProcessingStatus))
            }}
            className="rounded-lg border border-slate-300 px-3 py-2 text-sm focus:border-brand-600 focus:outline-none focus:ring-2 focus:ring-brand-600"
            aria-label="Filter by processing status"
          >
            {STATUS_FILTER_OPTIONS.map((option) => (
              <option key={option.label} value={option.value}>
                {option.label}
              </option>
            ))}
          </select>

          <select
            value={`${sortBy}:${sortDesc}`}
            onChange={(e) => {
              const [field, desc] = e.target.value.split(':')
              setSortBy(field as 'uploadedAt' | 'name')
              setSortDesc(desc === 'true')
            }}
            className="rounded-lg border border-slate-300 px-3 py-2 text-sm focus:border-brand-600 focus:outline-none focus:ring-2 focus:ring-brand-600"
            aria-label="Sort documents"
          >
            <option value="uploadedAt:true">Newest first</option>
            <option value="uploadedAt:false">Oldest first</option>
            <option value="name:false">Name (A-Z)</option>
            <option value="name:true">Name (Z-A)</option>
          </select>
        </div>
      </div>

      <div className="mt-4">
        {loadError && (
          <Alert variant="error">
            <div className="flex items-center justify-between gap-4">
              <span>{loadError}</span>
              <Button variant="secondary" size="md" onClick={fetchDocuments}>
                Retry
              </Button>
            </div>
          </Alert>
        )}

        {!loadError && isLoading && (
          <ul className="mt-2 flex flex-col gap-2" aria-busy="true" aria-live="polite">
            {[...Array(3)].map((_, i) => (
              <li key={i} className="h-16 animate-pulse rounded-xl bg-slate-100" />
            ))}
          </ul>
        )}

        {!loadError && !isLoading && documents.length === 0 && (
          <div className="mt-6 rounded-xl border border-dashed border-slate-300 py-12 text-center">
            <p className="text-slate-600">
              {search || statusFilter !== '' ? 'No documents match your filters.' : 'No documents yet. Upload your first PDF above.'}
            </p>
          </div>
        )}

        {!loadError && !isLoading && documents.length > 0 && (
          <div className="mt-2 overflow-x-auto rounded-xl border border-slate-200">
            <table className="min-w-full divide-y divide-slate-200 text-sm">
              <thead className="bg-slate-50 text-left text-xs font-medium uppercase tracking-wide text-slate-500">
                <tr>
                  <th scope="col" className="px-4 py-3">Name</th>
                  <th scope="col" className="px-4 py-3">Status</th>
                  <th scope="col" className="px-4 py-3">Pages</th>
                  <th scope="col" className="px-4 py-3">Size</th>
                  <th scope="col" className="px-4 py-3">Uploaded</th>
                  <th scope="col" className="px-4 py-3">
                    <span className="sr-only">Actions</span>
                  </th>
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-100 bg-white">
                {documents.map((doc) => (
                  <tr key={doc.id}>
                    <td className="px-4 py-3 font-medium text-slate-900">{doc.originalFileName}</td>
                    <td className="px-4 py-3">
                      <DocumentStatusBadge status={doc.processingStatus} />
                      {doc.processingStatus === DocumentProcessingStatus.Failed && doc.processingError && (
                        <p className="mt-1 text-xs text-red-600">{doc.processingError}</p>
                      )}
                    </td>
                    <td className="px-4 py-3 text-slate-600">{doc.pageCount ?? '—'}</td>
                    <td className="px-4 py-3 text-slate-600">{formatFileSize(doc.fileSizeBytes)}</td>
                    <td className="px-4 py-3 text-slate-600">{formatDate(doc.uploadedAt)}</td>
                    <td className="px-4 py-3 text-right">
                      <div className="flex justify-end gap-2">
                        {doc.processingStatus === DocumentProcessingStatus.Failed && (
                          <Button
                            variant="ghost"
                            size="md"
                            disabled={retryingId === doc.id}
                            onClick={() => handleRetry(doc)}
                          >
                            {retryingId === doc.id ? 'Retrying...' : 'Retry'}
                          </Button>
                        )}
                        <Button variant="ghost" size="md" onClick={() => setDeleteTarget(doc)}>
                          Delete
                        </Button>
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}

        {!loadError && !isLoading && totalPages > 1 && (
          <div className="mt-4 flex items-center justify-between">
            <Button variant="secondary" size="md" disabled={page <= 1} onClick={() => setPage((p) => p - 1)}>
              Previous
            </Button>
            <span className="text-sm text-slate-500">
              Page {page} of {totalPages}
            </span>
            <Button variant="secondary" size="md" disabled={page >= totalPages} onClick={() => setPage((p) => p + 1)}>
              Next
            </Button>
          </div>
        )}
      </div>

      <ConfirmDialog
        isOpen={deleteTarget !== null}
        title="Delete document?"
        description={`"${deleteTarget?.originalFileName}" will be permanently deleted. This cannot be undone.`}
        confirmLabel="Delete"
        isBusy={isDeleting}
        onConfirm={handleConfirmDelete}
        onCancel={() => setDeleteTarget(null)}
      />
    </main>
  )
}
