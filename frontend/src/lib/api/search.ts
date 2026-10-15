import { apiClient } from '../apiClient'
import type { SearchMode, SearchResultItem } from '../../types/api'

export interface SearchParams {
  query: string
  mode: SearchMode
  documentIds?: string[]
  topK?: number
}

export async function search(params: SearchParams): Promise<SearchResultItem[]> {
  const { data } = await apiClient.post<SearchResultItem[]>('/search', params)
  return data
}
