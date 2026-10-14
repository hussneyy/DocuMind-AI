import { DocumentProcessingStatus } from '../../types/api'

const STATUS_CONFIG: Record<DocumentProcessingStatus, { label: string; className: string }> = {
  [DocumentProcessingStatus.Uploaded]: { label: 'Uploaded', className: 'bg-slate-100 text-slate-700' },
  [DocumentProcessingStatus.Queued]: { label: 'Queued', className: 'bg-amber-100 text-amber-700' },
  [DocumentProcessingStatus.Processing]: { label: 'Processing', className: 'bg-amber-100 text-amber-700' },
  [DocumentProcessingStatus.Indexed]: { label: 'Indexed', className: 'bg-emerald-100 text-emerald-700' },
  [DocumentProcessingStatus.Failed]: { label: 'Failed', className: 'bg-red-100 text-red-700' },
}

export function DocumentStatusBadge({ status }: { status: DocumentProcessingStatus }) {
  const config = STATUS_CONFIG[status]
  return (
    <span className={`inline-flex items-center rounded-full px-2.5 py-0.5 text-xs font-medium ${config.className}`}>
      {config.label}
    </span>
  )
}
