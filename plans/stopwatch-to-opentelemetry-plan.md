# План: Замена Stopwatch на OpenTelemetry в MAFAgentLoop

## 1. Анализ текущего использования Stopwatch

В файле [`MAFAgentLoop.cs:40`](src/LLM_Demo.Application/AgentLoop/MAFAgentLoop.cs:40) используется `System.Diagnostics.Stopwatch` для измерения полного времени выполнения цикла агента:

```csharp
var stopwatch = System.Diagnostics.Stopwatch.StartNew();
```

### Где используется:

| Место | Строка | Назначение |
|-------|--------|------------|
| Старт | 40 | Начало замера |
| Успешное завершение | 66-75 | `stopwatch.Stop()` → `AgentLoopResult.Success(..., stopwatch.Elapsed)` |
| Ошибка инструмента | 86-88 | `stopwatch.Stop()` → `AgentLoopResult.Failure(...)` |
| Достигнут лимит итераций | 92-103 | `stopwatch.Stop()` → `AgentLoopResult.Success(..., stopwatch.Elapsed)` |

### Куда попадает Duration:

```
MAFAgentLoop.ExecuteAsync()
  → AgentLoopResult.Duration (TimeSpan)   [src/LLM_Demo.Domain/Agents/AgentLoopResult.cs:12]
    → ChatResponse.Duration (TimeSpan)     [src/LLM_Demo.Api/Models/Responses/ChatResponse.cs:8]
      → JSON → Frontend (как строка)       [src/LLM_Demo.Frontend/src/types/chat.ts:9]
```

## 2. Текущее состояние OpenTelemetry в проекте

**OpenTelemetry отсутствует.** Ни один `.csproj` не содержит пакетов OpenTelemetry:

- [`src/LLM_Demo.Application/LLM_Demo.Application.csproj`](src/LLM_Demo.Application/LLM_Demo.Application.csproj) — только `Microsoft.Extensions.AI.Abstractions`, `DI`, `Logging.Abstractions`
- [`src/LLM_Demo.Infrastructure/LLM_Demo.Infrastructure.csproj`](src/LLM_Demo.Infrastructure/LLM_Demo.Infrastructure.csproj) — EF Core, Quartz, OpenAI, JWT, BCrypt
- [`src/LLM_Demo.Api/LLM_Demo.Api.csproj`](src/LLM_Demo.Api/LLM_Demo.Api.csproj) — JWT, Swagger, OpenAPI
- [`Program.cs`](src/LLM_Demo.Api/Program.cs:1) — нет `AddOpenTelemetry()`, нет конфигурации экспорта

## 3. Ответ на вопрос: можно ли заменить Stopwatch на OpenTelemetry?

### ❌ Полностью заменить — НЕТ

`Stopwatch.Elapsed` записывается в [`AgentLoopResult.Duration`](src/LLM_Demo.Domain/Agents/AgentLoopResult.cs:12), который является **частью доменного контракта** и возвращается клиенту через [`ChatResponse`](src/LLM_Demo.Api/Models/Responses/ChatResponse.cs:5).

OpenTelemetry — это инструмент **обсервабильности** (мониторинг, трейсинг, метрики), а не механизм возврата данных в API-ответе.

### ✅ Дополнить OpenTelemetry — ДА, рекомендуется

OpenTelemetry **добавляет ценность** сверх Stopwatch:

| Возможность | Stopwatch | OpenTelemetry |
|-------------|-----------|---------------|
| Длительность в API-ответе | ✅ | ❌ (не для этого) |
| Детальные трейсы (каждый LLM-вызов) | ❌ | ✅ Activity |
| Метрики (p99 latency, throughput) | ❌ | ✅ Meter/Counter |
| Атрибуты (agentId, iterations) | ❌ | ✅ Tags |
| Экспорт в Jaeger/Prometheus | ❌ | ✅ |
| Корреляция с логами | ❌ | ✅ TraceId в логах |

### Рекомендуемая архитектура: гибридный подход

```
┌─────────────────────────────────────────────────────┐
│                   MAFAgentLoop                        │
│                                                       │
│  var stopwatch = Stopwatch.StartNew();  ← остаётся   │
│  using var activity = _activitySource                 │
│      .StartActivity("AgentLoop.Execute");  ← новый    │
│                                                       │
│  activity?.SetTag("agent.id", agent.Id);              │
│  activity?.SetTag("iterations", iterations);          │
│                                                       │
│  return AgentLoopResult.Success(...,                  │
│      stopwatch.Elapsed);  ← длительность в ответе     │
│                                                       │
│  // OpenTelemetry сам запишет activity.Duration        │
└─────────────────────────────────────────────────────┘
```

## 4. Предлагаемый план реализации

### Шаг 1: Добавить пакеты OpenTelemetry

В [`src/LLM_Demo.Api/LLM_Demo.Api.csproj`](src/LLM_Demo.Api/LLM_Demo.Api.csproj) добавить:

```
dotnet add package OpenTelemetry.Extensions.Hosting
dotnet add package OpenTelemetry.Exporter.Console     // для dev
dotnet add package OpenTelemetry.Exporter.OpenTelemetryProtocol  // для prod (OTLP)
```

### Шаг 2: Настроить OpenTelemetry в Program.cs

В [`src/LLM_Demo.Api/Program.cs`](src/LLM_Demo.Api/Program.cs) добавить `AddOpenTelemetry()` с:
- `AddAspNetCoreInstrumentation()` — HTTP-запросы
- `AddSource("LLM_Demo.AgentLoop")` — наши кастомные Activity

### Шаг 3: Создать ActivitySource в Application слое

Добавить статический класс в [`src/LLM_Demo.Application/`](src/LLM_Demo.Application/):

```csharp
namespace LLM_Demo.Application.Diagnostics;

public static class DiagnosticConfig
{
    public const string ServiceName = "LLM_Demo.AgentLoop";
    public static readonly ActivitySource ActivitySource = new(ServiceName);
}
```

### Шаг 4: Обернуть AgentLoop в Activity

В [`MAFAgentLoop.cs:40`](src/LLM_Demo.Application/AgentLoop/MAFAgentLoop.cs:40) добавить Activity поверх существующего Stopwatch:

- Activity стартует вместе со Stopwatch
- В Activity записываются атрибуты: `agent.id`, `conversation.id`, `iterations`, `is_success`
- Stopwatch остаётся для `AgentLoopResult.Duration`

### Шаг 5: (Опционально) Заменить простой Stopwatch на диагностический

Если `Duration` в API-ответе не критичен, можно:
- Убрать `Duration` из [`ChatResponse`](src/LLM_Demo.Api/Models/Responses/ChatResponse.cs:5) и [`AgentLoopResult`](src/LLM_Demo.Domain/Agents/AgentLoopResult.cs:6)
- Полностью полагаться на OpenTelemetry для метрик времени выполнения

## 5. Заключение

| Аспект | Вывод |
|--------|-------|
| Можно ли полностью заменить? | ❌ Нет, `Duration` — часть API-контракта |
| Стоит ли добавить OpenTelemetry? | ✅ Да, для observability |
| Stopwatch убрать? | ❌ Нет, оставить для API-ответа |
| Архитектура | Гибрид: Stopwatch + OpenTelemetry Activity |
