export interface CurrentUser {
  id: string
  email: string
  displayName: string
  createdAt: string
}

export const DocumentProcessingStatus = {
  Uploaded: 0,
  Queued: 1,
  Processing: 2,
  Indexed: 3,
  Failed: 4,
} as const

export type DocumentProcessingStatus = (typeof DocumentProcessingStatus)[keyof typeof DocumentProcessingStatus]

export interface DocumentDto {
  id: string
  originalFileName: string
  contentType: string
  fileSizeBytes: number
  pageCount: number | null
  processingStatus: DocumentProcessingStatus
  processingError: string | null
  uploadedAt: string
  indexedAt: string | null
}

export interface PagedResult<T> {
  items: T[]
  page: number
  pageSize: number
  totalCount: number
  totalPages: number
}

export const SearchMode = {
  Semantic: 'Semantic',
  Keyword: 'Keyword',
  Hybrid: 'Hybrid',
} as const

export type SearchMode = (typeof SearchMode)[keyof typeof SearchMode]

export interface SearchResultItem {
  chunkId: string
  documentId: string
  documentFileName: string
  excerpt: string
  pageStart: number
  pageEnd: number
  score: number
}
