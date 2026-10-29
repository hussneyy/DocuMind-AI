import { useId, useRef, useState } from 'react'
import type { DragEvent } from 'react'
import { UploadCloud } from 'lucide-react'

interface FileDropzoneProps {
  onFileSelected: (file: File) => void
  disabled?: boolean
}

export function FileDropzone({ onFileSelected, disabled }: FileDropzoneProps) {
  const inputId = useId()
  const inputRef = useRef<HTMLInputElement>(null)
  const [isDragOver, setIsDragOver] = useState(false)

  function handleDrop(event: DragEvent<HTMLDivElement>) {
    event.preventDefault()
    setIsDragOver(false)
    if (disabled) return
    const file = event.dataTransfer.files[0]
    if (file) onFileSelected(file)
  }

  return (
    <div
      onDragOver={(e) => {
        e.preventDefault()
        if (!disabled) setIsDragOver(true)
      }}
      onDragLeave={() => setIsDragOver(false)}
      onDrop={handleDrop}
      className={`flex flex-col items-center justify-center gap-2 rounded-xl border-2 border-dashed px-6 py-10 text-center transition-all duration-150 ${
        isDragOver
          ? 'scale-[1.01] border-brand-500 bg-brand-50 dark:bg-brand-500/10'
          : 'border-slate-300 bg-slate-50 hover:border-slate-400 dark:border-slate-600 dark:bg-slate-800/50 dark:hover:border-slate-500'
      } ${disabled ? 'opacity-60' : ''}`}
    >
      <span
        className={`flex h-12 w-12 items-center justify-center rounded-full transition-colors ${
          isDragOver ? 'bg-brand-100 text-brand-600 dark:bg-brand-500/20 dark:text-brand-400' : 'bg-slate-100 text-slate-400 dark:bg-slate-700 dark:text-slate-500'
        }`}
      >
        <UploadCloud className="h-6 w-6" aria-hidden="true" />
      </span>
      <p className="mt-1 text-sm text-slate-600 dark:text-slate-400">
        Drag and drop a PDF here, or{' '}
        <label htmlFor={inputId} className="cursor-pointer font-medium text-brand-600 hover:text-brand-700 dark:text-brand-400 dark:hover:text-brand-300">
          browse
        </label>
      </p>
      <p className="text-xs text-slate-500 dark:text-slate-400">PDF files only, up to 25 MB</p>
      <input
        ref={inputRef}
        id={inputId}
        type="file"
        accept="application/pdf,.pdf"
        className="sr-only"
        disabled={disabled}
        onChange={(e) => {
          const file = e.target.files?.[0]
          if (file) onFileSelected(file)
          e.target.value = ''
        }}
      />
    </div>
  )
}
