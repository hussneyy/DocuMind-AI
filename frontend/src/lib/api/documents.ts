import { apiClient } from '../apiClient'
import type { DocumentDto, DocumentProcessingStatus, PagedResult, SummaryDto, SummaryType } from '../../types/api'

export interface ListDocumentsParams {
  page?: number
  pageSize?: number
  search?: string
  status?: DocumentProcessingStatus
  sortBy?: 'uploadedAt' | 'name'
  sortDesc?: boolean
}

export async function listDocuments(params: ListDocumentsParams): Promise<PagedResult<DocumentDto>> {
  const { data } = await apiClient.get<PagedResult<DocumentDto>>('/documents', { params })
  return data
}

export async function uploadDocument(file: File): Promise<DocumentDto> {
  const formData = new FormData()
  formData.append('file', file)
  const { data } = await apiClient.post<DocumentDto>('/documents', formData, {
    headers: { 'Content-Type': 'multipart/form-data' },
  })
  return data
}

export async function deleteDocument(id: string): Promise<void> {
  await apiClient.delete(`/documents/${id}`)
}

export async function retryDocument(id: string): Promise<DocumentDto> {
  const { data } = await apiClient.post<DocumentDto>(`/documents/${id}/retry`)
  return data
}

export async function getDocument(id: string): Promise<DocumentDto> {
  const { data } = await apiClient.get<DocumentDto>(`/documents/${id}`)
  return data
}

export async function getSummary(id: string, type: SummaryType, regenerate = false): Promise<SummaryDto> {
  const { data } = await apiClient.get<SummaryDto>(`/documents/${id}/summary`, { params: { type, regenerate } })
  return data
}

/** Resolves the same base URL axios is configured with, for use as a plain <iframe>/<a> src (not an axios call). */
export function getFileUrl(id: string): string {
  const base = apiClient.defaults.baseURL ?? '/api'
  return `${base.replace(/\/+$/, '')}/documents/${id}/file`
}
