import { useEffect, useRef, useState } from 'react'
import type { FormEvent } from 'react'
import { Plus, Trash2 } from 'lucide-react'
import type { TagDto } from '../../types/api'

interface TagPickerProps {
  allTags: TagDto[]
  attachedTagIds: Set<string>
  onToggle: (tagId: string) => void
  onCreateAndAttach: (name: string) => void
  onDeleteTag: (tag: TagDto) => void
}

export function TagPicker({ allTags, attachedTagIds, onToggle, onCreateAndAttach, onDeleteTag }: TagPickerProps) {
  const [isOpen, setIsOpen] = useState(false)
  const [newTagName, setNewTagName] = useState('')
  const containerRef = useRef<HTMLDivElement>(null)

  useEffect(() => {
    if (!isOpen) return
    function handleClickOutside(event: MouseEvent) {
      if (containerRef.current && !containerRef.current.contains(event.target as Node)) {
        setIsOpen(false)
      }
    }
    function handleKeyDown(event: KeyboardEvent) {
      if (event.key === 'Escape') setIsOpen(false)
    }
    document.addEventListener('mousedown', handleClickOutside)
    document.addEventListener('keydown', handleKeyDown)
    return () => {
      document.removeEventListener('mousedown', handleClickOutside)
      document.removeEventListener('keydown', handleKeyDown)
    }
  }, [isOpen])

  function handleCreateSubmit(event: FormEvent) {
    event.preventDefault()
    const trimmed = newTagName.trim()
    if (!trimmed) return
    onCreateAndAttach(trimmed)
    setNewTagName('')
  }

  return (
    <div ref={containerRef} className="relative inline-block">
      <button
        type="button"
        onClick={() => setIsOpen((v) => !v)}
        aria-expanded={isOpen}
        aria-label="Manage tags"
        className="flex h-6 w-6 items-center justify-center rounded-full border border-dashed border-slate-300 text-slate-400 transition-colors hover:border-brand-400 hover:text-brand-600 dark:border-slate-600 dark:text-slate-500 dark:hover:border-brand-500 dark:hover:text-brand-400"
      >
        <Plus className="h-3.5 w-3.5" aria-hidden="true" />
      </button>

      {isOpen && (
        <div className="animate-scale-in absolute left-0 top-full z-10 mt-2 w-56 origin-top-left rounded-xl border border-slate-200 bg-white p-2 shadow-lg shadow-slate-200/60 dark:border-slate-700 dark:bg-slate-800 dark:shadow-black/30">
          {allTags.length === 0 && (
            <p className="px-2 py-1 text-xs text-slate-500 dark:text-slate-400">No tags yet. Create one below.</p>
          )}
          {allTags.length > 0 && (
            <ul className="flex max-h-48 flex-col gap-0.5 overflow-y-auto">
              {allTags.map((tag) => (
                <li key={tag.id} className="flex items-center gap-1">
                  <label className="flex min-w-0 flex-1 items-center gap-2 rounded-lg px-2 py-1.5 text-sm text-slate-700 hover:bg-slate-50 dark:text-slate-300 dark:hover:bg-slate-700">
                    <input
                      type="checkbox"
                      checked={attachedTagIds.has(tag.id)}
                      onChange={() => onToggle(tag.id)}
                      className="rounded border-slate-300 dark:border-slate-600 dark:bg-slate-800"
                    />
                    <span className="truncate">{tag.name}</span>
                  </label>
                  <button
                    type="button"
                    aria-label={`Delete tag ${tag.name}`}
                    onClick={() => onDeleteTag(tag)}
                    className="shrink-0 rounded p-1.5 text-slate-400 transition-colors hover:bg-red-50 hover:text-red-600 dark:text-slate-500 dark:hover:bg-red-500/10 dark:hover:text-red-400"
                  >
                    <Trash2 className="h-3.5 w-3.5" aria-hidden="true" />
                  </button>
                </li>
              ))}
            </ul>
          )}

          <form onSubmit={handleCreateSubmit} className="mt-1.5 flex gap-1 border-t border-slate-100 pt-1.5 dark:border-slate-700">
            <input
              type="text"
              value={newTagName}
              onChange={(e) => setNewTagName(e.target.value)}
              placeholder="New tag..."
              maxLength={50}
              className="w-full rounded-lg border border-slate-300 px-2 py-1 text-xs shadow-sm focus:border-brand-600 focus:outline-none focus:ring-1 focus:ring-brand-600 dark:border-slate-600 dark:bg-slate-800 dark:text-white"
            />
            <button
              type="submit"
              disabled={!newTagName.trim()}
              className="shrink-0 rounded-lg bg-brand-600 px-2 py-1 text-xs font-medium text-white transition-colors hover:bg-brand-700 disabled:opacity-50"
            >
              Add
            </button>
          </form>
        </div>
      )}
    </div>
  )
}
