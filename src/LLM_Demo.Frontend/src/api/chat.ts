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
 * В production возвращает полный URL, в dev — через Vite proxy.
 */
export function getChatStreamUrl(agentId: string, conversationId: string): string {
  return `${client.defaults.baseURL}/chat/${agentId}/stream?conversationId=${conversationId}`;
}
