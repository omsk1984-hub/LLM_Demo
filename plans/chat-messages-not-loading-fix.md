# План исправления: сообщения не загружаются при выборе беседы

## Диагностика проблемы

**Корень:** При выборе беседы в [`ChatPage.tsx`](src/LLM_Demo.Frontend/src/pages/ChatPage.tsx:99-104) функция `handleSelectConversation` очищает список сообщений (`setMessages([])`) и останавливает SSE, но **НЕ вызывает API** для загрузки существующих сообщений из БД.

API на бэкенде существует:
- [`GET /api/conversations/{id}/messages`](src/LLM_Demo.Api/Extensions/EndpointExtensions.cs:50-51) зарегистрирован и работает
- [`ConversationEndpoints.GetMessages()`](src/LLM_Demo.Api/Endpoints/ConversationEndpoints.cs:49-53) делает запрос к `ConversationRepository.GetMessagesAsync(conversationId)`

Однако на фронтенде **отсутствует** функция `getConversationMessages()` в [`conversations.ts`](src/LLM_Demo.Frontend/src/api/conversations.ts).

## План исправления

### Шаг 1: Добавить API-функцию `getConversationMessages`

**Файл:** [`src/LLM_Demo.Frontend/src/api/conversations.ts`](src/LLM_Demo.Frontend/src/api/conversations.ts)

Добавить функцию, которая запрашивает сообщения по ID беседы:

```typescript
export async function getConversationMessages(conversationId: string): Promise<Message[]> {
  const response = await client.get<Message[]>(`/conversations/${conversationId}/messages`);
  return response.data;
}
```

Требуется импорт типа `Message` из [`'../types/chat'`](src/LLM_Demo.Frontend/src/types/chat.ts).

### Шаг 2: Обновить `handleSelectConversation` в ChatPage

**Файл:** [`src/LLM_Demo.Frontend/src/pages/ChatPage.tsx`](src/LLM_Demo.Frontend/src/pages/ChatPage.tsx)

В функции `handleSelectConversation` (строка 99) добавить вызов `getConversationMessages` после очистки состояния:

```typescript
async function handleSelectConversation(conversationId: string) {
    setSelectedConversationId(conversationId);
    setMessages([]);
    clearChunks();
    stopStream();

    // Загружаем существующие сообщения для выбранной беседы
    try {
      const history = await getConversationMessages(conversationId);
      setMessages(history);
    } catch {
      // Если загрузка не удалась — просто показываем пустой чат
    }
}
```

Требуется импорт `getConversationMessages` из `'../api/conversations'`.

### Шаг 3 (опционально): Аналогично для `handleCreateConversation`

При создании новой беседы сообщений ещё нет, поэтому там изменений не требуется.

---

## Схема потока данных (до и после)

```
ДО исправления:

Пользователь выбирает беседу
       │
       ▼
handleSelectConversation()
       │
       ├── setSelectedConversationId(id)
       ├── setMessages([])        ← очистка
       ├── clearChunks()
       └── stopStream()
       │
       ▼
Сообщения НЕ загружаются ← БАГ
Чат пустой


ПОСЛЕ исправления:

Пользователь выбирает беседу
       │
       ▼
handleSelectConversation()
       │
       ├── setSelectedConversationId(id)
       ├── setMessages([])
       ├── clearChunks()
       ├── stopStream()
       └── await getConversationMessages(id) ← НОВЫЙ ВЫЗОВ API
                │
                ▼
       setMessages(history) ← сообщения из БД
       │
       ▼
Чат показывает историю сообщений ✅
```

## Проверка

1. Открыть приложение, залогиниться
2. Создать новую беседу, отправить несколько сообщений
3. Переключиться на другую беседу и вернуться обратно
4. Сообщения должны загружаться и отображаться корректно
