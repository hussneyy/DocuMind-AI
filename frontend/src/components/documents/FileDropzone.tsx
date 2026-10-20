import { useId, useRef, useState } from 'react'
import type { DragEvent } from 'react'

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
      className={`flex flex-col items-center justify-center gap-2 rounded-xl border-2 border-dashed px-6 py-10 text-center transition-colors ${
        isDragOver ? 'border-brand-500 bg-brand-50' : 'border-slate-300 bg-slate-50'
      } ${disabled ? 'opacity-60' : ''}`}
    >
      <svg viewBox="0 0 24 24" fill="none" className="h-8 w-8 text-slate-400" aria-hidden="true">
        <path
          d="M12 16V4m0 0 4 4m-4-4-4 4M4 16v3a1 1 0 0 0 1 1h14a1 1 0 0 0 1-1v-3"
          stroke="currentColor"
          strokeWidth="1.6"
          strokeLinecap="round"
          strokeLinejoin="round"
        />
      </svg>
      <p className="text-sm text-slate-600">
        Drag and drop a PDF here, or{' '}
        <label htmlFor={inputId} className="cursor-pointer font-medium text-brand-600 hover:text-brand-700">
          browse
        </label>
      </p>
      <p className="text-xs text-slate-500">PDF files only, up to 25 MB</p>
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
