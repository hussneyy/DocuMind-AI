import { CheckCircle2, Clock, Loader2, XCircle } from 'lucide-react'
import { DocumentProcessingStatus } from '../../types/api'

const STATUS_CONFIG: Record<DocumentProcessingStatus, { label: string; className: string; icon: typeof Clock; spin?: boolean }> = {
  [DocumentProcessingStatus.Uploaded]: {
    label: 'Uploaded',
    className: 'bg-slate-100 text-slate-700 dark:bg-slate-700 dark:text-slate-300',
    icon: Clock,
  },
  [DocumentProcessingStatus.Queued]: {
    label: 'Queued',
    className: 'bg-amber-100 text-amber-700 dark:bg-amber-500/15 dark:text-amber-400',
    icon: Clock,
  },
  [DocumentProcessingStatus.Processing]: {
    label: 'Processing',
    className: 'bg-amber-100 text-amber-700 dark:bg-amber-500/15 dark:text-amber-400',
    icon: Loader2,
    spin: true,
  },
  [DocumentProcessingStatus.Indexed]: {
    label: 'Indexed',
    className: 'bg-emerald-100 text-emerald-700 dark:bg-emerald-500/15 dark:text-emerald-400',
    icon: CheckCircle2,
  },
  [DocumentProcessingStatus.Failed]: {
    label: 'Failed',
    className: 'bg-red-100 text-red-700 dark:bg-red-500/15 dark:text-red-400',
    icon: XCircle,
  },
}

export function DocumentStatusBadge({ status }: { status: DocumentProcessingStatus }) {
  const config = STATUS_CONFIG[status]
  const Icon = config.icon
  return (
    <span className={`inline-flex items-center gap-1.5 rounded-full px-2.5 py-0.5 text-xs font-medium ${config.className}`}>
      <Icon className={`h-3.5 w-3.5 ${config.spin ? 'animate-spin' : ''}`} aria-hidden="true" />
      {config.label}
    </span>
  )
}
