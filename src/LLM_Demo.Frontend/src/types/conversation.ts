export interface Conversation {
  id: string;
  title?: string;
  ownerId: string;
  status: ConversationStatus;
  createdAt: string;
}

export type ConversationStatus = 'Active' | 'Archived' | 'Completed';
