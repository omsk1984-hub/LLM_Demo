import { useState, useEffect } from 'react';
import { getAllConversations, createConversation } from '../api/conversations';
import type { Conversation } from '../types/conversation';

interface ConversationListProps {
  selectedConversationId: string | null;
  onSelect: (conversationId: string) => void;
  onCreate: (conversation: Conversation) => void;
}

export default function ConversationList({
  selectedConversationId,
  onSelect,
  onCreate,
}: ConversationListProps) {
  const [conversations, setConversations] = useState<Conversation[]>([]);
  const [loading, setLoading] = useState(true);
  const [creating, setCreating] = useState(false);

  async function loadConversations() {
    try {
      setLoading(true);
      const data = await getAllConversations();
      setConversations(data);
    } catch {
      // ignore
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    loadConversations();
  }, []);

  async function handleCreate() {
    try {
      setCreating(true);
      const conv = await createConversation();
      setConversations((prev) => [conv, ...prev]);
      onCreate(conv);
    } catch {
      // ignore
    } finally {
      setCreating(false);
    }
  }

  return (
    <div className="card">
      <div className="flex items-center justify-between mb-3">
        <h2 className="text-sm font-semibold text-gray-500 uppercase tracking-wider">
          Беседы
        </h2>
        <button
          onClick={handleCreate}
          disabled={creating}
          className="text-indigo-600 hover:text-indigo-700 text-sm font-medium disabled:opacity-50"
        >
          {creating ? '...' : '+ Новая'}
        </button>
      </div>

      {loading ? (
        <div className="animate-pulse space-y-2">
          {[1, 2, 3].map((i) => (
            <div key={i} className="h-10 bg-gray-200 rounded" />
          ))}
        </div>
      ) : conversations.length === 0 ? (
        <p className="text-gray-400 text-sm text-center py-4">
          Нет бесед. Создайте новую.
        </p>
      ) : (
        <div className="space-y-1 max-h-80 overflow-y-auto">
          {conversations.map((conv) => (
            <button
              key={conv.id}
              onClick={() => onSelect(conv.id)}
              className={`w-full text-left px-3 py-2 rounded-lg text-sm transition-colors duration-150 ${
                selectedConversationId === conv.id
                  ? 'bg-indigo-50 text-indigo-700 font-medium'
                  : 'hover:bg-gray-100 text-gray-600'
              }`}
            >
              <span className="truncate block">
                {conv.title || `Беседа ${conv.createdAt ? new Date(conv.createdAt).toLocaleDateString('ru-RU') : ''}`}
              </span>
              <span className="text-xs text-gray-400">
                {new Date(conv.createdAt).toLocaleTimeString('ru-RU')}
              </span>
            </button>
          ))}
        </div>
      )}
    </div>
  );
}
