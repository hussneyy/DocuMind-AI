import { apiClient } from '../apiClient'
import type { AskResultDto, ConversationDetailDto, ConversationSummaryDto } from '../../types/api'

export interface AskParams {
  question: string
  conversationId?: string
  documentIds?: string[]
}

export async function ask(params: AskParams): Promise<AskResultDto> {
  const { data } = await apiClient.post<AskResultDto>('/chat/ask', params)
  return data
}

export async function listConversations(): Promise<ConversationSummaryDto[]> {
  const { data } = await apiClient.get<ConversationSummaryDto[]>('/chat/conversations')
  return data
}

export async function getConversation(id: string): Promise<ConversationDetailDto> {
  const { data } = await apiClient.get<ConversationDetailDto>(`/chat/conversations/${id}`)
  return data
}

export async function deleteConversation(id: string): Promise<void> {
  await apiClient.delete(`/chat/conversations/${id}`)
}
