import { apiClient } from '../apiClient'
import type { TagDto } from '../../types/api'

export async function listTags(): Promise<TagDto[]> {
  const { data } = await apiClient.get<TagDto[]>('/tags')
  return data
}

export async function createTag(name: string): Promise<TagDto> {
  const { data } = await apiClient.post<TagDto>('/tags', { name })
  return data
}

export async function deleteTag(id: string): Promise<void> {
  await apiClient.delete(`/tags/${id}`)
}
