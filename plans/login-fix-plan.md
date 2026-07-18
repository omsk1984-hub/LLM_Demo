# План исправления потока логина

## Найденные проблемы

### 1. Vite proxy target — неверный порт и протокол ❌

| Источник | Значение |
|---|---|
| [`launchSettings.json`](../src/LLM_Demo.Api/Properties/launchSettings.json:17) — профиль `http` | `http://localhost:5023` |
| [`vite.config.ts`](../src/LLM_Demo.Frontend/vite.config.ts:10) — target | `https://localhost:5001` |

**Проблема**: Прокси фронтенда шлёт запросы на `https://localhost:5001`, но API запускается на `http://localhost:5023`.  
**Исправление**: Поменять target в [`vite.config.ts`](../src/LLM_Demo.Frontend/vite.config.ts:10) на `http://localhost:5023`.

### 2. Hardcoded `expiresAt` в AuthEndpoints ❌

[`AuthEndpoints.Login`](../src/LLM_Demo.Api/Endpoints/AuthEndpoints.cs:43) и [`Register`](../src/LLM_Demo.Api/Endpoints/AuthEndpoints.cs:81):
```csharp
DateTime.UtcNow.AddHours(1) // захардкожено
```
Однако JWT генерируется с ExpiryInMinutes из [`JwtOptions`](../src/LLM_Demo.Infrastructure/Auth/JwtOptions.cs:10) (60 минут = 1 час, совпадает сейчас, но если поменять — разойдётся).

**Исправление**: Внедрить `JwtOptions` в `AuthEndpoints` и читать `ExpiryInMinutes` для вычисления `expiresAt`.

### 3. Пустой 401 ответ при неверном логине ⚠️

При неверном пароле или email [`AuthEndpoints.Login`](../src/LLM_Demo.Api/Endpoints/AuthEndpoints.cs:30,36) возвращает `Results.Unauthorized()` — 401 без тела ответа.

Фронтенд ожидает `err.response.data.error` (см. [`LoginPage.tsx:30`](../src/LLM_Demo.Frontend/src/pages/LoginPage.tsx:30)). Сейчас срабатывает fallback «Ошибка входа...». 

**Исправление**: Возвращать JSON c ошибкой: `Results.Json(new ErrorResponse(...), statusCode: 401)`.

## Порядок исправлений

1. **Vite proxy** → [`vite.config.ts`](../src/LLM_Demo.Frontend/vite.config.ts) — target
2. **expiresAt** → внедрить `IOptions<JwtOptions>` в `AuthEndpoints`
3. **401 body** → добавить JSON в 401 ответ
4. **Документация** → обновить комментарий в `run.cmd`
