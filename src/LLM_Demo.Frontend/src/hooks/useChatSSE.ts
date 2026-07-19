import { useState, useRef, useCallback, useEffect, useMemo } from 'react';
import type { StreamingChunk } from '../types/chat';
import { getChatStreamUrl } from '../api/chat';

/** Интервал сброса буфера чанков в state (ms) */
const CHUNK_FLUSH_INTERVAL_MS = 80;

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
  const closedRef = useRef(false);

  // streamingContent вычисляем из chunks через useMemo — дешёвая операция
  const streamingContent = useMemo(
    () => chunks.filter((ch) => !ch.isFinal).map((ch) => ch.content).join(''),
    [chunks],
  );

  // Буфер чанков — чтобы дёргать setChunks не на каждый токен
  const chunkBufferRef = useRef<StreamingChunk[]>([]);
  const flushTimerRef = useRef<ReturnType<typeof setTimeout> | null>(null);

  const flushChunks = useCallback(() => {
    flushTimerRef.current = null;
    if (chunkBufferRef.current.length === 0) return;
    const snapshot = chunkBufferRef.current;
    chunkBufferRef.current = [];
    setChunks((prev) => [...prev, ...snapshot]);
  }, []);

  const scheduleFlush = useCallback(() => {
    if (flushTimerRef.current) return;
    flushTimerRef.current = setTimeout(flushChunks, CHUNK_FLUSH_INTERVAL_MS);
  }, [flushChunks]);

  const forceFlush = useCallback(() => {
    if (flushTimerRef.current) {
      clearTimeout(flushTimerRef.current);
      flushTimerRef.current = null;
    }
    if (chunkBufferRef.current.length > 0) {
      const snapshot = chunkBufferRef.current;
      chunkBufferRef.current = [];
      setChunks((prev) => [...prev, ...snapshot]);
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

    stopStream();

    setError(null);
    setChunks([]);
    chunkBufferRef.current = [];
    closedRef.current = false;

    const url = getChatStreamUrl(agentId, conversationId, token);
    const eventSource = new EventSource(url, { withCredentials: false });

    eventSourceRef.current = eventSource;

    eventSource.addEventListener('chunk', (event) => {
      try {
        const chunk: StreamingChunk = JSON.parse(event.data);
        console.log('[SSE chunk received]', chunk, '| isFinal:', chunk.isFinal);
        chunkBufferRef.current.push(chunk);

        if (chunk.isFinal) {
          console.log('[SSE] isFinal=true, forceFlush + closing');
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
      console.log('[SSE] complete event received');
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

    eventSource.onopen = () => {
      if (closedRef.current) return;
      setIsConnected(true);
    };

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
