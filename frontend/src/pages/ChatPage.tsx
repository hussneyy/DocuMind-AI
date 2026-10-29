import axios from 'axios'
import { useEffect, useRef, useState } from 'react'
import type { FormEvent } from 'react'
import { Link } from 'react-router-dom'
import { ArrowRight, Bot, MessageSquarePlus, SendHorizontal, Sparkles, User, X } from 'lucide-react'
import { Alert } from '../components/ui/Alert'
import { Button } from '../components/ui/Button'
import { extractErrorMessages } from '../lib/apiClient'
import * as chatApi from '../lib/api/chat'
import type { ConversationSummaryDto, MessageDto } from '../types/api'

const SUGGESTED_QUESTIONS = [
  'What is the exam registration deadline?',
  'Summarize the key policies in this document',
  'What happens if I miss a deadline?',
]

export function ChatPage() {
  const [conversations, setConversations] = useState<ConversationSummaryDto[]>([])
  const [activeConversationId, setActiveConversationId] = useState<string | null>(null)
  const [messages, setMessages] = useState<MessageDto[]>([])

  const [question, setQuestion] = useState('')
  const [isAsking, setIsAsking] = useState(false)
  const [isLoadingConversation, setIsLoadingConversation] = useState(false)
  const [errors, setErrors] = useState<string[]>([])

  const messagesContainerRef = useRef<HTMLDivElement>(null)

  useEffect(() => {
    refreshConversations()
  }, [])

  useEffect(() => {
    // Scroll only this container's own scrollbar, not scrollIntoView, which can also move an
    // ancestor (even the window) if it decides the target isn't fully visible there too.
    const container = messagesContainerRef.current
    if (container) container.scrollTop = container.scrollHeight
  }, [messages])

  async function refreshConversations() {
    try {
      const items = await chatApi.listConversations()
      setConversations(items)
    } catch {
      setConversations([])
    }
  }

  async function openConversation(id: string) {
    setErrors([])
    setIsLoadingConversation(true)
    try {
      const detail = await chatApi.getConversation(id)
      setActiveConversationId(detail.id)
      setMessages(detail.messages)
    } catch (error) {
      setErrors(axios.isAxiosError(error) ? extractErrorMessages(error) : ['Failed to load conversation.'])
    } finally {
      setIsLoadingConversation(false)
    }
  }

  function startNewChat() {
    setActiveConversationId(null)
    setMessages([])
    setErrors([])
  }

  async function handleDelete(id: string) {
    try {
      await chatApi.deleteConversation(id)
      if (id === activeConversationId) startNewChat()
      await refreshConversations()
    } catch (error) {
      setErrors(axios.isAxiosError(error) ? extractErrorMessages(error) : ['Failed to delete conversation.'])
    }
  }

  async function submitQuestion(text: string) {
    const trimmed = text.trim()
    if (!trimmed) return

    setIsAsking(true)
    setErrors([])
    setQuestion('')

    try {
      const result = await chatApi.ask({ question: trimmed, conversationId: activeConversationId ?? undefined })
      setMessages((prev) => [...prev, result.userMessage, result.assistantMessage])

      const isNewConversation = activeConversationId === null
      setActiveConversationId(result.conversationId)
      if (isNewConversation) await refreshConversations()
    } catch (error) {
      setQuestion(trimmed)
      setErrors(axios.isAxiosError(error) ? extractErrorMessages(error) : ['Failed to get an answer.'])
    } finally {
      setIsAsking(false)
    }
  }

  async function handleSubmit(event: FormEvent) {
    event.preventDefault()
    await submitQuestion(question)
  }

  return (
    <main className="mx-auto flex h-[calc(100vh-4rem)] max-w-6xl overflow-hidden px-4 sm:px-6 lg:px-8">
      <aside className="hidden min-h-0 w-64 shrink-0 flex-col border-r border-slate-200 py-6 pr-4 sm:flex">
        <Button
          variant="secondary"
          size="md"
          onClick={startNewChat}
          className="mb-4 w-full justify-start"
          leftIcon={<MessageSquarePlus className="h-4 w-4" aria-hidden="true" />}
        >
          New chat
        </Button>
        <ul className="flex flex-col gap-1 overflow-y-auto">
          {conversations.map((conversation) => (
            <li key={conversation.id} className="group flex items-center gap-1">
              <button
                type="button"
                onClick={() => openConversation(conversation.id)}
                className={`flex-1 truncate rounded-lg px-3 py-2 text-left text-sm transition-colors ${
                  conversation.id === activeConversationId ? 'bg-brand-50 text-brand-700' : 'text-slate-600 hover:bg-slate-100'
                }`}
              >
                {conversation.title}
              </button>
              <button
                type="button"
                aria-label={`Delete conversation "${conversation.title}"`}
                onClick={() => handleDelete(conversation.id)}
                className="shrink-0 rounded p-1.5 text-slate-500 opacity-0 transition-colors hover:bg-red-50 hover:text-red-600 focus-visible:opacity-100 group-hover:opacity-100 group-focus-within:opacity-100"
              >
                <X className="h-3.5 w-3.5" aria-hidden="true" />
              </button>
            </li>
          ))}
          {conversations.length === 0 && (
            <li className="mt-6 flex flex-col items-center px-3 py-2 text-center">
              <span className="flex h-9 w-9 items-center justify-center rounded-full bg-slate-100 text-slate-400">
                <MessageSquarePlus className="h-4 w-4" aria-hidden="true" />
              </span>
              <p className="mt-2 text-sm text-slate-500">No conversations yet.</p>
            </li>
          )}
        </ul>
      </aside>

      <section className="flex min-h-0 flex-1 flex-col py-6">
        <h1 className="flex items-center gap-2 text-2xl">
          <Sparkles className="h-5 w-5 text-brand-600" aria-hidden="true" />
          Ask DocuMind AI
        </h1>
        <p className="mt-1 text-sm text-slate-600">
          Ask a question across your indexed documents. Answers are grounded in your own files and cite the source.
        </p>

        <div ref={messagesContainerRef} className="mt-6 min-h-0 flex-1 overflow-y-auto">
          {errors.length > 0 && (
            <Alert variant="error">
              <ul className="list-inside list-disc">
                {errors.map((error) => (
                  <li key={error}>{error}</li>
                ))}
              </ul>
            </Alert>
          )}

          {isLoadingConversation && (
            <p className="flex items-center gap-2 text-sm text-slate-500">
              <span className="h-3 w-3 animate-spin rounded-full border-2 border-slate-300 border-t-brand-600" />
              Loading conversation...
            </p>
          )}

          {!isLoadingConversation && messages.length === 0 && (
            <div className="animate-fade-in flex flex-col items-center rounded-xl border border-dashed border-slate-300 py-14 text-center">
              <span className="flex h-12 w-12 items-center justify-center rounded-full bg-brand-50 text-brand-500">
                <Bot className="h-6 w-6" aria-hidden="true" />
              </span>
              <p className="mt-3 text-slate-600">Ask something like &ldquo;What is the exam registration deadline?&rdquo;</p>
              <div className="mt-4 flex flex-wrap justify-center gap-2 px-4">
                {SUGGESTED_QUESTIONS.map((suggestion) => (
                  <button
                    key={suggestion}
                    type="button"
                    onClick={() => submitQuestion(suggestion)}
                    className="rounded-full border border-slate-200 px-3 py-1.5 text-xs font-medium text-slate-600 transition-colors hover:border-brand-200 hover:bg-brand-50 hover:text-brand-700"
                  >
                    {suggestion}
                  </button>
                ))}
              </div>
            </div>
          )}

          <ul className="flex flex-col gap-4">
            {messages.map((message, i) => (
              <li
                key={message.id}
                className={`animate-fade-in-up flex items-start gap-2.5 ${message.role === 'user' ? 'justify-end' : 'justify-start'}`}
                style={{ animationDelay: `${Math.min(i, 4) * 40}ms` }}
              >
                {message.role === 'assistant' && (
                  <span className="mt-0.5 flex h-7 w-7 shrink-0 items-center justify-center rounded-full bg-brand-50 text-brand-600">
                    <Bot className="h-4 w-4" aria-hidden="true" />
                  </span>
                )}
                <div
                  className={`max-w-[80%] rounded-xl px-4 py-3 text-sm ${
                    message.role === 'user'
                      ? 'bg-brand-600 text-white shadow-sm shadow-brand-600/20'
                      : 'border border-slate-200 bg-white text-slate-800 shadow-sm'
                  }`}
                >
                  <p className="whitespace-pre-wrap">{message.content}</p>

                  {message.citations.length > 0 && (
                    <ul className="mt-3 flex flex-col gap-2 border-t border-slate-100 pt-3">
                      {message.citations.map((citation, i) => (
                        <li key={citation.chunkId} className="rounded-lg bg-slate-50 p-2 text-xs text-slate-600">
                          <div className="flex items-center justify-between gap-2 font-medium text-slate-700">
                            <span>
                              [{i + 1}] {citation.documentFileName}
                            </span>
                            <span className="shrink-0 rounded-full bg-slate-200 px-2 py-0.5">
                              {citation.pageStart === citation.pageEnd ? `p.${citation.pageStart}` : `p.${citation.pageStart}-${citation.pageEnd}`}
                            </span>
                          </div>
                          <p className="mt-1 text-slate-500">{citation.excerpt}</p>
                          <Link
                            to={`/documents/${citation.documentId}/view?page=${citation.pageStart}`}
                            className="mt-1 inline-flex items-center gap-1 font-medium text-brand-600 transition-colors hover:text-brand-700"
                          >
                            View source
                            <ArrowRight className="h-3 w-3" aria-hidden="true" />
                          </Link>
                        </li>
                      ))}
                    </ul>
                  )}
                </div>
                {message.role === 'user' && (
                  <span className="mt-0.5 flex h-7 w-7 shrink-0 items-center justify-center rounded-full bg-slate-100 text-slate-500">
                    <User className="h-4 w-4" aria-hidden="true" />
                  </span>
                )}
              </li>
            ))}
          </ul>

          {isAsking && (
            <div className="animate-fade-in mt-4 flex items-start gap-2.5">
              <span className="mt-0.5 flex h-7 w-7 shrink-0 items-center justify-center rounded-full bg-brand-50 text-brand-600">
                <Bot className="h-4 w-4" aria-hidden="true" />
              </span>
              <div className="rounded-xl border border-slate-200 bg-white px-4 py-3 shadow-sm">
                <span className="flex gap-1">
                  <span className="h-2 w-2 animate-bounce rounded-full bg-slate-400 [animation-delay:-0.3s]" />
                  <span className="h-2 w-2 animate-bounce rounded-full bg-slate-400 [animation-delay:-0.15s]" />
                  <span className="h-2 w-2 animate-bounce rounded-full bg-slate-400" />
                </span>
              </div>
            </div>
          )}
        </div>

        <form onSubmit={handleSubmit} className="mt-4 flex gap-2 border-t border-slate-200 pt-4">
          <input
            type="text"
            value={question}
            onChange={(e) => setQuestion(e.target.value)}
            placeholder="Ask a question about your documents..."
            className="w-full rounded-lg border border-slate-300 px-4 py-2.5 text-sm shadow-sm transition-colors hover:border-slate-400 focus:border-brand-600 focus:outline-none focus:ring-2 focus:ring-brand-600"
            aria-label="Question"
          />
          <Button
            type="submit"
            size="lg"
            isLoading={isAsking}
            disabled={!question.trim()}
            rightIcon={!isAsking ? <SendHorizontal className="h-4 w-4" aria-hidden="true" /> : undefined}
          >
            {isAsking ? 'Asking...' : 'Ask'}
          </Button>
        </form>
      </section>
    </main>
  )
}
