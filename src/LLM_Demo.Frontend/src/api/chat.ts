import client from './client';
import type { ChatRequest, ChatResponse } from '../types/chat';

export async function sendChatMessage(
  agentId: string,
  data: ChatRequest
): Promise<ChatResponse> {
  const response = await client.post<ChatResponse>(`/chat/${agentId}`, data);
  return response.data;
}

/**
 * Возвращает URL для SSE-подключения к стримингу чата.
 * Token передаётся как query-параметр, т.к. EventSource API
 * не поддерживает кастомные HTTP-заголовки.
 */
export function getChatStreamUrl(agentId: string, conversationId: string, token: string): string {
  const base = `${client.defaults.baseURL}/chat/${agentId}/stream`;
  return `${base}?conversationId=${encodeURIComponent(conversationId)}&token=${encodeURIComponent(token)}`;
}
