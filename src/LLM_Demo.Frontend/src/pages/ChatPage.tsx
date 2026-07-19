import { useState, useCallback, useEffect, useRef } from 'react';
import { useAuth } from '../context/AuthContext';
import { useChatSSE } from '../hooks/useChatSSE';
import { sendChatMessage } from '../api/chat';
import { getConversationMessages } from '../api/conversations';
import AgentSelector from '../components/AgentSelector';
import ConversationList from '../components/ConversationList';
import ChatMessages from '../components/ChatMessages';
import ChatInput from '../components/ChatInput';
import type { Conversation } from '../types/conversation';
import type { Message } from '../types/chat';

export default function ChatPage() {
  const { token } = useAuth();

  const [selectedAgentId, setSelectedAgentId] = useState<string | null>(null);
  const [selectedConversationId, setSelectedConversationId] = useState<string | null>(null);
  const [messages, setMessages] = useState<Message[]>([]);
  const [isLoading, setIsLoading] = useState(false);
  const hasStreamStarted = useRef(false);

  const {
    chunks,
    isConnected,
    error: sseError,
    startStream,
    stopStream,
    clearChunks,
  } = useChatSSE({
    agentId: selectedAgentId,
    conversationId: selectedConversationId,
    token,
  });

  // Обработка завершения SSE — добавляем полное сообщение ассистента
  useEffect(() => {
    const finalChunk = chunks.find((ch) => ch.isFinal);
    if (finalChunk && hasStreamStarted.current) {
      const fullContent = chunks
        .filter((ch) => !ch.isFinal)
        .map((ch) => ch.content)
        .join('');

      if (fullContent) {
        setMessages((prev) => [
          ...prev,
          {
            id: crypto.randomUUID(),
            conversationId: selectedConversationId!,
            role: 'Assistant',
            content: fullContent,
            timestamp: new Date().toISOString(),
          },
        ]);
      }

      hasStreamStarted.current = false;
      clearChunks();
      setIsLoading(false);
    }
  }, [chunks, selectedConversationId, clearChunks]);

  // Сбрасываем isLoading при ошибке SSE
  useEffect(() => {
    if (sseError && hasStreamStarted.current) {
      hasStreamStarted.current = false;
      setIsLoading(false);
    }
  }, [sseError]);

  const handleSend = useCallback(
    async (text: string) => {
      if (!selectedAgentId || !selectedConversationId) return;

      // Добавляем сообщение пользователя
      const userMessage: Message = {
        id: crypto.randomUUID(),
        conversationId: selectedConversationId,
        role: 'User',
        content: text,
        timestamp: new Date().toISOString(),
      };

      setMessages((prev) => [...prev, userMessage]);
      setIsLoading(true);

      // Отправляем сообщение через REST
      try {
        await sendChatMessage(selectedAgentId, {
          conversationId: selectedConversationId,
          message: text,
        });
      } catch {
        // Даже если REST упал, SSE всё равно может работать
      }

      // Запускаем SSE-стриминг
      hasStreamStarted.current = true;
      startStream();
    },
    [selectedAgentId, selectedConversationId, startStream]
  );

  function handleSelectAgent(agentId: string) {
    setSelectedAgentId(agentId);
  }

  async function handleSelectConversation(conversationId: string) {
    setSelectedConversationId(conversationId);
    setMessages([]);
    clearChunks();
    stopStream();

    try {
      const history = await getConversationMessages(conversationId);
      setMessages(history);
    } catch {
      // если загрузка не удалась — показываем пустой чат
    }
  }

  function handleCreateConversation(conversation: Conversation) {
    setSelectedConversationId(conversation.id);
    setMessages([]);
    clearChunks();
  }

  return (
    <div className="h-[calc(100vh-4rem)] flex">
      {/* Left sidebar */}
      <div className="w-72 border-r border-gray-200 bg-white flex flex-col gap-4 p-4 overflow-y-auto">
        <AgentSelector
          selectedAgentId={selectedAgentId}
          onSelect={handleSelectAgent}
        />
        <ConversationList
          selectedConversationId={selectedConversationId}
          onSelect={handleSelectConversation}
          onCreate={handleCreateConversation}
        />
      </div>

      {/* Chat area */}
      <div className="flex-1 flex flex-col">
        {!selectedConversationId ? (
          <div className="flex-1 flex items-center justify-center text-gray-400">
            <div className="text-center">
              <div className="text-6xl mb-4">💬</div>
              <p className="text-xl">Выберите агента и беседу</p>
              <p className="text-sm mt-2">
                Или создайте новую беседу, чтобы начать
              </p>
            </div>
          </div>
        ) : (
          <>
            <ChatMessages
              messages={messages}
              streamingChunks={chunks}
              isLoading={isLoading}
            />

            {sseError && (
              <div className="bg-red-50 border-t border-red-200 px-4 py-2 text-sm text-red-600">
                SSE: {sseError}
              </div>
            )}

            <ChatInput
              onSend={handleSend}
              disabled={isLoading || isConnected || !selectedAgentId}
              placeholder={
                !selectedAgentId
                  ? 'Сначала выберите агента'
                  : 'Напишите сообщение...'
              }
            />
          </>
        )}
      </div>
    </div>
  );
}
