import { useState, useRef, useCallback, useEffect } from 'react';
import type { StreamingChunk } from '../types/chat';
import { getChatStreamUrl } from '../api/chat';

interface UseChatSSEOptions {
  agentId: string | null;
  conversationId: string | null;
  token: string | null;
}

interface UseChatSSEResult {
  chunks: StreamingChunk[];
  isConnected: boolean;
  error: string | null;
  startStream: () => void;
  stopStream: () => void;
  clearChunks: () => void;
}

export function useChatSSE({
  agentId,
  conversationId,
  token,
}: UseChatSSEOptions): UseChatSSEResult {
  const [chunks, setChunks] = useState<StreamingChunk[]>([]);
  const [isConnected, setIsConnected] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const eventSourceRef = useRef<EventSource | null>(null);

  const stopStream = useCallback(() => {
    if (eventSourceRef.current) {
      eventSourceRef.current.close();
      eventSourceRef.current = null;
      setIsConnected(false);
    }
  }, []);

  const startStream = useCallback(() => {
    if (!agentId || !conversationId || !token) return;

    // Закрываем предыдущее подключение, если есть
    stopStream();

    setError(null);
    setChunks([]);

    const url = getChatStreamUrl(agentId, conversationId);
    const eventSource = new EventSource(url, {
      withCredentials: false,
    });

    eventSourceRef.current = eventSource;

    eventSource.addEventListener('chunk', (event) => {
      try {
        const chunk: StreamingChunk = JSON.parse(event.data);
        setChunks((prev) => [...prev, chunk]);

        if (chunk.isFinal) {
          eventSource.close();
          eventSourceRef.current = null;
          setIsConnected(false);
        }
      } catch {
        // ignore parse errors
      }
    });

    eventSource.addEventListener('complete', () => {
      eventSource.close();
      eventSourceRef.current = null;
      setIsConnected(false);
    });

    eventSource.addEventListener('error', (event) => {
      const message = (event as MessageEvent).data || 'SSE connection error';
      setError(message);
      eventSource.close();
      eventSourceRef.current = null;
      setIsConnected(false);
    });

    eventSource.onopen = () => {
      setIsConnected(true);
    };

    eventSource.onerror = () => {
      setError('Connection lost');
      setIsConnected(false);
      eventSource.close();
      eventSourceRef.current = null;
    };
  }, [agentId, conversationId, token, stopStream]);

  const clearChunks = useCallback(() => {
    setChunks([]);
    setError(null);
  }, []);

  // Очистка при размонтировании
  useEffect(() => {
    return () => {
      if (eventSourceRef.current) {
        eventSourceRef.current.close();
        eventSourceRef.current = null;
      }
    };
  }, []);

  return {
    chunks,
    isConnected,
    error,
    startStream,
    stopStream,
    clearChunks,
  };
}
