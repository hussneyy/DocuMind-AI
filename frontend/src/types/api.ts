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

export interface CitationDto {
  chunkId: string
  documentId: string
  documentFileName: string
  pageStart: number
  pageEnd: number
  excerpt: string
}

export type MessageRole = 'user' | 'assistant'

export interface MessageDto {
  id: string
  role: MessageRole
  content: string
  createdAt: string
  citations: CitationDto[]
}

export interface AskResultDto {
  conversationId: string
  title: string
  userMessage: MessageDto
  assistantMessage: MessageDto
}

export interface ConversationSummaryDto {
  id: string
  title: string
  createdAt: string
  updatedAt: string
}

export interface ConversationDetailDto {
  id: string
  title: string
  createdAt: string
  updatedAt: string
  messages: MessageDto[]
}

export const SummaryType = {
  Short: 'Short',
  Detailed: 'Detailed',
  KeyPoints: 'KeyPoints',
} as const

export type SummaryType = (typeof SummaryType)[keyof typeof SummaryType]

export interface SummaryDto {
  summaryType: SummaryType
  content: string
  sourceChunkCount: number
  generatedByModel: string
  generatedAt: string
}
