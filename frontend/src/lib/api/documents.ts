import { apiClient } from '../apiClient'
import type { DocumentDto, DocumentProcessingStatus, PagedResult } from '../../types/api'

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

export async function getDocument(id: string): Promise<DocumentDto> {
  const { data } = await apiClient.get<DocumentDto>(`/documents/${id}`)
  return data
}
