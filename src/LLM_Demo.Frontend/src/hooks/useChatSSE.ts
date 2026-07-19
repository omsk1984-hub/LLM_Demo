import { useState, useRef, useCallback, useEffect } from 'react';
import type { StreamingChunk } from '../types/chat';
import { getChatStreamUrl } from '../api/chat';

/** Интервал сброса буфера чанков в state (ms). 50ms ≈ 20fps — достаточно для плавного вывода текста */
const FLUSH_INTERVAL_MS = 50;

interface UseChatSSEOptions {
  agentId: string | null;
  conversationId: string | null;
  token: string | null;
}

interface UseChatSSEResult {
  chunks: StreamingChunk[];
  streamingContent: string;
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

  // --- Throttling: буфер чанков + таймер флаша ---
  const chunkBufferRef = useRef<StreamingChunk[]>([]);
  const flushTimerRef = useRef<ReturnType<typeof setTimeout> | null>(null);

  /** Принудительный сброс буфера в state */
  const flushChunks = useCallback(() => {
    flushTimerRef.current = null;
    if (chunkBufferRef.current.length === 0) return;
    setChunks((prev) => [...prev, ...chunkBufferRef.current]);
    chunkBufferRef.current = [];
  }, []);

  /** Запланировать сброс буфера через FLUSH_INTERVAL_MS */
  const scheduleFlush = useCallback(() => {
    if (flushTimerRef.current) return;
    flushTimerRef.current = setTimeout(flushChunks, FLUSH_INTERVAL_MS);
  }, [flushChunks]);

  /** Сбросить буфер принудительно и очистить таймер */
  const forceFlush = useCallback(() => {
    if (flushTimerRef.current) {
      clearTimeout(flushTimerRef.current);
      flushTimerRef.current = null;
    }
    if (chunkBufferRef.current.length > 0) {
      setChunks((prev) => [...prev, ...chunkBufferRef.current]);
      chunkBufferRef.current = [];
    }
  }, []);

  const stopStream = useCallback(() => {
    if (eventSourceRef.current) {
      closedRef.current = true;
      eventSourceRef.current.close();
      eventSourceRef.current = null;
      setIsConnected(false);
    }
    forceFlush();
  }, [forceFlush]);

  const startStream = useCallback(() => {
    if (!agentId || !conversationId || !token) return;

    // Закрываем предыдущее подключение, если есть
    stopStream();

    setError(null);
    setChunks([]);
    chunkBufferRef.current = [];
    closedRef.current = false;

    const url = getChatStreamUrl(agentId, conversationId, token);
    const eventSource = new EventSource(url, {
      withCredentials: false,
    });

    eventSourceRef.current = eventSource;

    eventSource.addEventListener('chunk', (event) => {
      try {
        const chunk: StreamingChunk = JSON.parse(event.data);
        chunkBufferRef.current.push(chunk);

        // Если это финальный чанк — сбрасываем буфер немедленно
        if (chunk.isFinal) {
          forceFlush();
          closedRef.current = true;
          eventSource.close();
          eventSourceRef.current = null;
          setIsConnected(false);
        } else {
          scheduleFlush();
        }
      } catch {
        // ignore parse errors
      }
    });

    eventSource.addEventListener('complete', () => {
      forceFlush();
      closedRef.current = true;
      eventSource.close();
      eventSourceRef.current = null;
      setIsConnected(false);
    });

    eventSource.addEventListener('cancelled', () => {
      forceFlush();
      closedRef.current = true;
      eventSource.close();
      eventSourceRef.current = null;
      setIsConnected(false);
    });

    eventSource.addEventListener('error', (event) => {
      if (closedRef.current) return;
      closedRef.current = true;

      forceFlush();
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

      forceFlush();
      setError('Соединение потеряно');
      setIsConnected(false);
      if (eventSourceRef.current) {
        eventSourceRef.current.close();
        eventSourceRef.current = null;
      }
    };
  }, [agentId, conversationId, token, stopStream, scheduleFlush, forceFlush]);

  const clearChunks = useCallback(() => {
    setChunks([]);
    chunkBufferRef.current = [];
    setError(null);
  }, []);

  // Актуальный стриминговый контент (учитывает как state, так и буфер)
  const streamingContent = chunks
    .concat(chunkBufferRef.current)
    .filter((ch) => !ch.isFinal)
    .map((ch) => ch.content)
    .join('');

  // Очистка при размонтировании
  useEffect(() => {
    return () => {
      closedRef.current = true;
      if (flushTimerRef.current) {
        clearTimeout(flushTimerRef.current);
        flushTimerRef.current = null;
      }
      if (eventSourceRef.current) {
        eventSourceRef.current.close();
        eventSourceRef.current = null;
      }
    };
  }, []);

  return {
    chunks,
    streamingContent,
    isConnected,
    error,
    startStream,
    stopStream,
    clearChunks,
  };
}
