# План исправления: бесконечный цикл в `ChunkText`

## Описание проблемы

Метод `ChunkText` в [`DocumentService.cs:131`](../src/LLM_Demo.Application/RAG/DocumentService.cs:131) может войти в бесконечный цикл из-за ошибки в логике вычисления следующего `start` при обработке последнего чанка.

### Условия возникновения

- Текст длиннее `maxChars` (2000 символов при `maxTokens=500`)
- После обработки последнего чанка, когда `end == text.Length`, строка `start = end - overlapChars` отбрасывает `start` назад, и он **больше никогда** не достигает `text.Length`, что приводит к бесконечному циклу `while`.

### Пример

Для текста длиной 5000 символов:
1. Итерация 1: `start=0, end=2000 → start=1800`
2. Итерация 2: `start=1800, end=3800 → start=3600`
3. Итерация 3: `start=3600, end=5000 → start=4800` (последний чанк)
4. Итерация 4: `start=4800, end=5000 → start=4800` 🔄 **зацикливание**
5. Итерация 5: ... бесконечность

## План исправления

### Вариант A (рекомендуемый) — `break` при достижении конца текста

После формирования чанка, если `end >= text.Length`, установить `start = text.Length` и завершить цикл.

```csharp
while (start < text.Length)
{
    var end = Math.Min(start + maxChars, text.Length);

    // Поиск границы предложения (только если не последний чанк)
    if (end < text.Length)
    {
        var searchEnd = Math.Min(end + 100, text.Length);
        var lastNewline = text.LastIndexOf('\n', end, 100);
        var lastPeriod = text.LastIndexOf('.', end, 100);
        var breakAt = Math.Max(lastNewline, lastPeriod);

        if (breakAt > start)
            end = breakAt + 1;
    }

    var chunkText = text[start..end];
    chunks.Add(new Chunk(index, chunkText.Trim()));

    // Если дошли до конца — выходим
    if (end >= text.Length)
        break;

    start = end - overlapChars;
    index++;
}
```

### Вариант B — условный сдвиг `start`

Сдвигать `start` на `overlapChars` только если это не последний чанк:

```csharp
start = (end >= text.Length) ? text.Length : (end - overlapChars);
```

### Вариант C — разделить на две фазы

Сначала вычислить все позиции разбиения, потом формировать чанки — более декларативный подход, но требует больше изменений.

## Рекомендация

**Вариант A** — минимальное и понятное изменение. Добавляет `if (end >= text.Length) break;` после добавления чанка, что полностью устраняет зацикливание, сохраняя текущую логику перекрытия для всех чанков, кроме последнего.

## Файл для изменения

- [`src/LLM_Demo.Application/RAG/DocumentService.cs`](../src/LLM_Demo.Application/RAG/DocumentService.cs) — метод `ChunkText` (строка 131)
