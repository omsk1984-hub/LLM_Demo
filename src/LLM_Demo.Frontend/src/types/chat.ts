export interface ChatRequest {
  conversationId: string;
  message: string;
}

export interface ChatResponse {
  messages: Message[];
  iterations: number;
  duration: string; // TimeSpan as string
}

export interface Message {
  id: string;
  conversationId: string;
  role: MessageRole;
  content: string;
  timestamp: string;
}

export type MessageRole = 'System' | 'User' | 'Assistant' | 'Tool';

export interface StreamingChunk {
  content: string;
  isFinal: boolean;
  toolCallId?: string;
  error?: string;
}
