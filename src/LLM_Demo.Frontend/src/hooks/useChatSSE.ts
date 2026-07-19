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
  // Флаг для предотвращения повторной обработки ошибки после закрытия
  const closedRef = useRef(false);

  const stopStream = useCallback(() => {
    if (eventSourceRef.current) {
      closedRef.current = true;
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
    closedRef.current = false;

    const url = getChatStreamUrl(agentId, conversationId, token);
    const eventSource = new EventSource(url, {
      withCredentials: false,
    });

    eventSourceRef.current = eventSource;

    eventSource.addEventListener('chunk', (event) => {
      try {
        const chunk: StreamingChunk = JSON.parse(event.data);
        setChunks((prev) => [...prev, chunk]);

        if (chunk.isFinal) {
          closedRef.current = true;
          eventSource.close();
          eventSourceRef.current = null;
          setIsConnected(false);
        }
      } catch {
        // ignore parse errors
      }
    });

    eventSource.addEventListener('complete', () => {
      closedRef.current = true;
      eventSource.close();
      eventSourceRef.current = null;
      setIsConnected(false);
    });

    eventSource.addEventListener('cancelled', () => {
      closedRef.current = true;
      eventSource.close();
      eventSourceRef.current = null;
      setIsConnected(false);
    });

    eventSource.addEventListener('error', (event) => {
      if (closedRef.current) return;
      closedRef.current = true;

      const message = (event as MessageEvent).data || 'Ошибка подключения';
      setError(message);
      eventSource.close();
      eventSourceRef.current = null;
      setIsConnected(false);
    });

    // onopen — единственное место для отслеживания успешного подключения
    eventSource.onopen = () => {
      if (closedRef.current) return;
      setIsConnected(true);
    };

    // onerror срабатывает при любой ошибке соединения
    // (в т.ч. когда сервер закрывает соединение после complete)
    eventSource.onerror = () => {
      if (closedRef.current) return;
      closedRef.current = true;

      setError('Соединение потеряно');
      setIsConnected(false);
      if (eventSourceRef.current) {
        eventSourceRef.current.close();
        eventSourceRef.current = null;
      }
    };
  }, [agentId, conversationId, token, stopStream]);

  const clearChunks = useCallback(() => {
    setChunks([]);
    setError(null);
  }, []);

  // Очистка при размонтировании
  useEffect(() => {
    return () => {
      closedRef.current = true;
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
