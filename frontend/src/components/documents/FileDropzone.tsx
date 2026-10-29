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
        isDragOver ? 'scale-[1.01] border-brand-500 bg-brand-50' : 'border-slate-300 bg-slate-50 hover:border-slate-400'
      } ${disabled ? 'opacity-60' : ''}`}
    >
      <span
        className={`flex h-12 w-12 items-center justify-center rounded-full transition-colors ${
          isDragOver ? 'bg-brand-100 text-brand-600' : 'bg-slate-100 text-slate-400'
        }`}
      >
        <UploadCloud className="h-6 w-6" aria-hidden="true" />
      </span>
      <p className="mt-1 text-sm text-slate-600">
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
