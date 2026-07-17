# 🧠 LLM_Demo — Multi-Agent Framework на .NET 8

**LLM_Demo** — это демонстрационный проект Multi-Agent Framework (MAF) на C# 12 / .NET 8, построенный поверх `IChatClient` (Microsoft.Extensions.AI) с интеграцией **Orleans** для распределённых агентов и **Quartz.NET** для шедулинга.

---

## 🚀 Быстрый старт

### 1. Запустить PostgreSQL через Docker

```bash
docker compose up -d
```

### 2. Запустить API

```bash
dotnet run --project src/LLM_Demo.Api
```

Swagger UI будет доступен по адресу: `http://localhost:5000/swagger`

### 3. Получить JWT токен

```bash
curl -X POST http://localhost:5000/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"demo@test.com","password":"demo123"}'
```

### 4. Использовать API

```bash
curl http://localhost:5000/api/agents \
  -H "Authorization: Bearer <token>"
```

---

## 🏗️ Архитектура

```
┌─────────────────────────────────────────────────────────────┐
│                    LLM_Demo.Api (Minimal API)               │
│  JWT Auth · SSE Streaming · Swagger · Ownership Middleware  │
└─────────────────────┬───────────────────────────────────────┘
                      │
┌─────────────────────▼───────────────────────────────────────┐
│                LLM_Demo.Application (Use Cases)              │
│  MAFAgentLoop · ToolMiddlewarePipeline · SubAgentOrchestrator│
└─────────────────────┬───────────────────────────────────────┘
                      │
┌─────────────────────▼───────────────────────────────────────┐
│               LLM_Demo.Infrastructure (Services)             │
│  EF Core + PostgreSQL · JWT · Quartz.NET · ConnectorProvider│
│  Tools: SendSafety · Calculator · FileSystem · WebSearch    │
└─────────────────────┬───────────────────────────────────────┘
                      │
┌─────────────────────▼───────────────────────────────────────┐
│              LLM_Demo.Agents (Orleans Grains)               │
│  AgentGrain · ConversationGrain · SubAgentGrain             │
└─────────────────────┬───────────────────────────────────────┘
                      │
┌─────────────────────▼───────────────────────────────────────┐
│                 LLM_Demo.Domain (Core)                       │
│  Agent · Message · Conversation · Tool · Middleware Interfaces│
└─────────────────────────────────────────────────────────────┘
```

---

## 🧩 Ключевые компоненты

### Agent Loop (MAF поверх IChatClient)

```csharp
// Итеративный цикл: LLM → ToolCall → MiddlewarePipeline → LLM
var loop = new MAFAgentLoop(chatClient, toolExecutor, logger);
var result = await loop.ExecuteAsync(conversation, agent);
```

### Tool Middleware Pipeline

Цепочка middleware в порядке выполнения:

1. **LoggingMiddleware** — логирование всех вызовов инструментов
2. **FilteringMiddleware** — проверка разрешённых инструментов для агента
3. **SafetyMiddleware** — фильтрация опасных паттернов (send-safety)
4. **StreamingMiddleware** — трансляция чанков подписчикам SSE

### SSE Streaming

```
GET /api/chat/{agentId}/stream?conversationId=xxx
→ event: chunk  data: {"content":"Hello","isFinal":false}
→ event: chunk  data: {"content":" world","isFinal":false}
→ event: complete  data: {"isFinal":true}
```

### Connectors

```csharp
// IConnectorProvider — единый интерфейс для разных LLM
provider.GetClient("openai");    // OpenAI API
provider.GetClient("ollama");    // Локальная Ollama
provider.GetClient("azure");     // Azure OpenAI
```

---

## 📋 API Endpoints

### Аутентификация

| Method | Path | Описание |
|--------|------|----------|
| POST | `/api/auth/register` | Регистрация |
| POST | `/api/auth/login` | Вход, получение JWT |

### Агенты

| Method | Path | Описание |
|--------|------|----------|
| GET | `/api/agents` | Список агентов |
| POST | `/api/agents` | Создать агента |
| GET | `/api/agents/{id}` | Получить агента |
| PUT | `/api/agents/{id}` | Обновить агента |
| DELETE | `/api/agents/{id}` | Удалить агента |

### Беседы

| Method | Path | Описание |
|--------|------|----------|
| GET | `/api/conversations` | Список бесед |
| POST | `/api/conversations` | Создать беседу |
| GET | `/api/conversations/{id}` | Получить беседу |
| GET | `/api/conversations/{id}/messages` | Сообщения беседы |

### Чат

| Method | Path | Описание |
|--------|------|----------|
| POST | `/api/chat/{agentId}` | Отправить сообщение (JSON) |
| GET | `/api/chat/{agentId}/stream` | Отправить сообщение (SSE stream) |

### Инструменты

| Method | Path | Описание |
|--------|------|----------|
| GET | `/api/tools` | Список доступных инструментов |

---

## 🛠️ Технический стек

| Компонент | Технология |
|-----------|-----------|
| **Язык** | C# 12 (.NET 8) |
| **AI/LLM** | `IChatClient` (Microsoft.Extensions.AI) |
| **Оркестрация** | Orleans 8.x (Grains) |
| **ORM** | Entity Framework Core 8 + Npgsql |
| **База данных** | PostgreSQL 16 |
| **Шедулинг** | Quartz.NET 3.x |
| **Аутентификация** | JWT Bearer (ASP.NET Core) |
| **Стриминг** | Server-Sent Events (SSE) |
| **Тестирование** | xUnit + Moq + FluentAssertions |
| **Контейнеризация** | Docker / Docker Compose |

---

## 🐳 Docker Compose

```yaml
services:
  postgres:
    image: postgres:16
    environment:
      POSTGRES_DB: llm_demo
      POSTGRES_USER: llm_demo_user
      POSTGRES_PASSWORD: llm_demo_pass
    ports:
      - "5432:5432"

  postgres-orleans:
    image: postgres:16
    environment:
      POSTGRES_DB: orleans
      POSTGRES_USER: orleans_user
      POSTGRES_PASSWORD: orleans_pass
    ports:
      - "5433:5432"
```

---

## 🧪 Тестирование

```bash
# Запуск всех тестов
dotnet test

# Результат: 27 passed, 0 failed
```

Тесты покрывают:
- ✅ Domain Models (Agent, Message, Conversation, ToolResult)
- ✅ Middleware (Filtering, Safety, Logging)
- ✅ SubAgent Orchestrator
- ✅ Tool Registry
- ✅ Ownership Service
- ✅ SendSafety Tool
- ✅ Calculator Tool

---

## 📁 Структура проекта

```
LLM_Demo/
├── src/
│   ├── LLM_Demo.Domain/           # Модели и интерфейсы
│   │   ├── Agents/                # Agent, AgentStatus, IAgentLoop
│   │   ├── Messages/              # Message, MessageRole, StreamingChunk
│   │   ├── Conversations/         # Conversation, ConversationStatus
│   │   ├── Tools/                 # ToolDefinition, ToolResult, ToolCall
│   │   ├── Middleware/            # IToolMiddleware, ToolMiddlewareContext
│   │   ├── Connectors/            # IConnectorProvider
│   │   ├── Common/                # Result<T>, IRepository<T>
│   │   └── Ownership/             # IOwnable
│   │
│   ├── LLM_Demo.Application/      # Use Cases
│   │   ├── AgentLoop/             # MAFAgentLoop
│   │   ├── Middleware/            # Pipeline, Logging, Filtering, Safety, Streaming
│   │   ├── SubAgents/             # Orchestrator, Router
│   │   └── Ownership/             # OwnershipService
│   │
│   ├── LLM_Demo.Infrastructure/   # Реализации
│   │   ├── Persistence/           # EF Core DbContext, Repositories
│   │   ├── Auth/                  # JwtTokenService, JwtOptions
│   │   ├── Connectors/            # ConnectorProvider, OpenAIConnector
│   │   ├── Tools/                 # SendSafety, Calculator, FileSystem, ToolRegistry
│   │   └── Scheduling/            # QuartzService, AgentJob, ToolJob
│   │
│   ├── LLM_Demo.Agents/           # Orleans Grains
│   │   ├── Interfaces/            # IAgentGrain, IConversationGrain, ISubAgentGrain
│   │   ├── Grains/                # AgentGrain, ConversationGrain, SubAgentGrain
│   │   └── Configuration/         # OrleansConfigurator
│   │
│   └── LLM_Demo.Api/              # Minimal API
│       ├── Endpoints/             # Auth, Agent, Conversation, Chat
│       ├── Middleware/            # ExceptionHandlingMiddleware
│       ├── Models/                # Request/Response DTOs
│       └── Extensions/            # EndpointExtensions
│
├── tests/
│   └── LLM_Demo.Tests/            # 27 unit-тестов
│
├── docker-compose.yml             # PostgreSQL + Orleans DB
├── plans/
│   └── project-plan.md            # Детальный план с диаграммами
└── README.md                      # Этот файл
```

---

## 🔄 Agent Loop Flow

```mermaid
sequenceDiagram
    participant User
    participant API
    participant AgentLoop as MAFAgentLoop
    participant LLM as IChatClient
    participant Pipeline as Middleware Pipeline
    participant Tool as Executor

    User->>API: POST /chat/{agentId}
    API->>AgentLoop: ExecuteAsync()
    
    loop MaxIterations
        AgentLoop->>LLM: CompleteAsync(history)
        LLM-->>AgentLoop: ChatResponse
        
        alt Tool Call detected
            AgentLoop->>Pipeline: Execute(ToolCall)
            Pipeline->>Pipeline: Logging → Filtering → Safety
            Pipeline->>Tool: Invoke
            Tool-->>Pipeline: ToolResult
            Pipeline-->>AgentLoop: Result
            
            AgentLoop->>LLM: Send result back
        else Final Response
            AgentLoop-->>API: AgentLoopResult
            API-->>User: JSON Response
        end
    end
```

---

## 📄 Лицензия

MIT
