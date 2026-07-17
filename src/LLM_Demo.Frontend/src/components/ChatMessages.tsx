import { useEffect, useRef } from 'react';
import type { Message, StreamingChunk } from '../types/chat';
import MessageBubble from './MessageBubble';

interface ChatMessagesProps {
  messages: Message[];
  streamingChunks: StreamingChunk[];
  isLoading: boolean;
}

export default function ChatMessages({
  messages,
  streamingChunks,
  isLoading,
}: ChatMessagesProps) {
  const bottomRef = useRef<HTMLDivElement>(null);

  // Авто-скролл к последнему сообщению
  useEffect(() => {
    bottomRef.current?.scrollIntoView({ behavior: 'smooth' });
  }, [messages, streamingChunks]);

  const streamingContent = streamingChunks
    .filter((ch) => !ch.isFinal)
    .map((ch) => ch.content)
    .join('');

  if (messages.length === 0 && !streamingContent && !isLoading) {
    return (
      <div className="flex-1 flex items-center justify-center text-gray-400">
        <div className="text-center">
          <div className="text-4xl mb-4">💬</div>
          <p className="text-lg">Выберите агента и начните диалог</p>
        </div>
      </div>
    );
  }

  return (
    <div className="flex-1 overflow-y-auto px-4 py-6 space-y-4">
      {messages.map((msg) => (
        <MessageBubble key={msg.id} message={msg} />
      ))}

      {streamingContent && (
        <MessageBubble
          message={{ role: 'Assistant', content: streamingContent }}
        />
      )}

      {isLoading && !streamingContent && (
        <div className="flex items-center gap-2 text-gray-400 ml-1">
          <div className="flex gap-1">
            <span className="w-2 h-2 bg-gray-400 rounded-full animate-bounce" style={{ animationDelay: '0ms' }} />
            <span className="w-2 h-2 bg-gray-400 rounded-full animate-bounce" style={{ animationDelay: '150ms' }} />
            <span className="w-2 h-2 bg-gray-400 rounded-full animate-bounce" style={{ animationDelay: '300ms' }} />
          </div>
          <span className="text-sm">Печатает...</span>
        </div>
      )}

      <div ref={bottomRef} />
    </div>
  );
}
