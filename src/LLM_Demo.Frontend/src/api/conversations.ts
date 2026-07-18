import client from './client';
import type { Conversation } from '../types/conversation';
import type { Message } from '../types/chat';

export async function getAllConversations(): Promise<Conversation[]> {
  const response = await client.get<Conversation[]>('/conversations');
  return response.data;
}

export async function createConversation(): Promise<Conversation> {
	const response = await client.post<Conversation>('/conversations');
	return response.data;
}

export async function getConversationMessages(conversationId: string): Promise<Message[]> {
	const response = await client.get<Message[]>(`/conversations/${conversationId}/messages`);
	return response.data;
}
