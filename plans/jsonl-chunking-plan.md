# План: чанкование JSONL-файлов с преобразованием в человекочитаемый текст

## Описание задачи

Доработать метод `ChunkText` в [`DocumentService.cs`](../src/LLM_Demo.Application/RAG/DocumentService.cs):

1. **Обнаружить JSONL-формат** — если файл состоит из строк, где каждая строка — валидный JSON, обработать его специальным образом.
2. **Конвертировать JSON → человекочитаемый текст** — каждая JSON-строка превращается в русскоязычное описание человека.
3. **Разбить по строкам** — одна запись = один чанк, никакого перекрытия.

## Детальные изменения

### 1. Замена метода `ChunkText`

Текущий метод (строки 133–179) полностью заменяется на:

```csharp
private static List<Chunk> ChunkText(string text, int maxTokens = 500, int overlapTokens = 50)
{
    var chunks = new List<Chunk>();
    var lines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries);

    // Если хотя бы одна строка — валидный JSON → используем JSONL-чанкование
    if (lines.Length > 0 && IsJsonLine(lines[0]))
    {
        foreach (var line in lines)
        {
            if (!string.IsNullOrWhiteSpace(line))
            {
                var humanText = JsonToHumanReadable(line.Trim());
                chunks.Add(new Chunk(chunks.Count, humanText));
            }
        }
        return chunks;
    }

    // Иначе — оригинальная логика с исправлением бесконечного цикла
    const int charsPerToken = 4;
    var maxChars = maxTokens * charsPerToken;
    var overlapChars = overlapTokens * charsPerToken;

    if (text.Length <= maxChars)
    {
        chunks.Add(new Chunk(0, text));
        return chunks;
    }

    var start = 0;
    var index = 0;

    while (start < text.Length)
    {
        var end = Math.Min(start + maxChars, text.Length);

        if (end < text.Length)
        {
            var lastNewline = text.LastIndexOf('\n', end, Math.Min(100, end));
            var lastPeriod = text.LastIndexOf('.', end, Math.Min(100, end));
            var breakAt = Math.Max(lastNewline, lastPeriod);

            if (breakAt > start)
                end = breakAt + 1;
        }

        var chunkText = text[start..end];
        chunks.Add(new Chunk(index, chunkText.Trim()));

        if (end >= text.Length)
            break;

        start = end - overlapChars;
        index++;
    }

    return chunks;
}
```

### 2. Добавить вспомогательные методы

```csharp
/// <summary>
/// Проверяет, является ли строка валидным JSON-объектом.
/// </summary>
private static bool IsJsonLine(string line)
{
    line = line.Trim();
    return line.StartsWith('{') && line.EndsWith('}');
}

/// <summary>
/// Преобразует JSON-строку случайного человека в человекочитаемый текст.
/// Пример: {"name":"Алиса Воронцова","height_cm":168,"weight_kg":58,"age":29,"city":"Москва","profession":"UX-дизайнер"}
/// → "Алиса Воронцова. 29 лет. Рост 168 см, вес 58 кг. Город: Москва. Профессия: UX-дизайнер."
/// </summary>
private static string JsonToHumanReadable(string json)
{
    try
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var name = root.TryGetProperty("name", out var n) ? n.GetString() ?? "" : "";
        var age = root.TryGetProperty("age", out var a) ? a.GetInt32() : 0;
        var height = root.TryGetProperty("height_cm", out var h) ? h.GetInt32() : 0;
        var weight = root.TryGetProperty("weight_kg", out var w) ? w.GetInt32() : 0;
        var city = root.TryGetProperty("city", out var c) ? c.GetString() ?? "" : "";
        var profession = root.TryGetProperty("profession", out var p) ? p.GetString() ?? "" : "";

        var gender = DetectGender(name);
        var ageWord = GetAgeWord(age, gender);

        return $"{name}. {age} {ageWord}. Рост {height} см, вес {weight} кг. Город: {city}. Профессия: {profession}.";
    }
    catch (JsonException)
    {
        // Если не удалось распарсить — возвращаем исходный текст
        return json;
    }
}

/// <summary>
/// Определяет пол по окончанию имени.
/// </summary>
private static string DetectGender(string name)
{
    if (string.IsNullOrEmpty(name)) return "male";
    // Русские женские имена часто заканчиваются на -а, -я
    var lastChar = name[^1];
    return lastChar is 'а' or 'я' or 'А' or 'Я' ? "female" : "male";
}

/// <summary>
/// Возвращает правильное склонение слова "год" для возраста.
/// </summary>
private static string GetAgeWord(int age, string gender)
{
    var lastDigit = age % 10;
    var lastTwoDigits = age % 100;

    if (lastTwoDigits is >= 11 and <= 19)
        return "лет";

    return lastDigit switch
    {
        1 => "год",
        >= 2 and <= 4 => "года",
        _ => "лет"
    };
}
```

### 3. Удалить отладочные логи

Строки 43, 46–49 — удалить:

```csharp
_logger.LogInformation($"1111");          // строка 43 — удалить
_logger.LogInformation($"2222 - {chunks.Count}"); // строка 46 — удалить
_logger.LogInformation($"0 - {chunks[0]}");        // строка 47 — удалить
_logger.LogInformation($"1 - {chunks[1]}");        // строка 48 — удалить
_logger.LogInformation($"2 - {chunks[2]}");        // строка 49 — удалить
```

### 4. Добавить missing using

В секцию using (строка 3–5) добавить:

```csharp
using System.Text.Json;
```

## Файлы для изменения

- [`src/LLM_Demo.Application/RAG/DocumentService.cs`](../src/LLM_Demo.Application/RAG/DocumentService.cs)

## Порядок выполнения

1. Добавить `using System.Text.Json;`
2. Удалить отладочные логи (строки 43, 46–49)
3. Заменить метод `ChunkText` на новый
4. Добавить вспомогательные методы `IsJsonLine`, `JsonToHumanReadable`, `DetectGender`, `GetAgeWord`
