# RealTime-App

A trainee project: a **real-time chatbot** built with an **ASP.NET Core Web API** (.NET 10) and an **Angular 21** front end.
Bot replies are streamed word by word over **SignalR**, conversations are saved in **SQLite** with **EF Core**, and the bot can be
either an offline **demo bot** or **Claude** (Anthropic's AI model).

```
Angular (http://localhost:4200)
   │  REST  /api/conversations      → list / create / rename / delete chats
   │  SignalR /hubs/chat (stream)   → send a message, receive the reply chunk by chunk
   ▼
ASP.NET Core Web API (http://localhost:5000)
   ├── Application  → ChatService / ConversationService (business logic)
   ├── Infrastructure → EF Core + SQLite, Claude API client or Demo bot
   └── Domain       → Conversation, Message entities
```

A full project guide (technologies, architecture, every file, API, concepts) is in
[`docs/RealTime-App-Project-Guide.docx`](docs/RealTime-App-Project-Guide.docx).

## Project structure

```
backend/
├── ChatBot.slnx
├── src/
│   ├── ChatBot.Domain/            # Entities and enums. No dependencies on other projects.
│   │   ├── Common/BaseEntity.cs
│   │   ├── Entities/Conversation.cs, Message.cs
│   │   └── Enums/MessageRole.cs
│   ├── ChatBot.Application/       # Use cases. Depends only on Domain.
│   │   ├── Abstractions/          # Interfaces implemented by Infrastructure (repository, AI service)
│   │   ├── DTOs/                  # What the API sends and receives
│   │   ├── Exceptions/            # NotFoundException, ValidationException
│   │   ├── Mappings/              # Entity → DTO
│   │   ├── Services/              # ChatService, ConversationService
│   │   └── DependencyInjection.cs
│   ├── ChatBot.Infrastructure/    # Technical details. Depends on Application.
│   │   ├── AI/                    # ClaudeChatCompletionService, DemoChatCompletionService, options
│   │   ├── Persistence/           # AppDbContext, entity configurations, repository, migrations
│   │   └── DependencyInjection.cs
│   └── ChatBot.Api/               # Entry point. Controllers, SignalR hub, error handling.
│       ├── Controllers/ConversationsController.cs
│       ├── Hubs/ChatHub.cs, HubExceptionFilter.cs
│       ├── Middleware/GlobalExceptionHandler.cs
│       └── Program.cs
└── tests/
    └── ChatBot.Application.Tests/ # xUnit tests with hand-written fakes (no database needed)

frontend/chatbot-ui/src/app/
├── core/
│   ├── models/                    # TypeScript interfaces that match the API DTOs
│   └── services/                  # ConversationApiService (HTTP), ChatHubService (SignalR)
├── features/chat/
│   ├── chat.store.ts              # Signal-based state for the chat screen
│   ├── chat-page/                 # Container component (talks to the store)
│   ├── conversation-list/         # Sidebar            (presentational: inputs + outputs)
│   ├── message-list/              # Chat bubbles       (presentational)
│   └── message-input/             # Text box + Send    (presentational)
├── app.config.ts                  # HttpClient, router
└── app.routes.ts                  # "/" and "/c/:id"
```

**Why this structure?** The backend follows *Clean Architecture*: dependencies point inwards
(`Api → Infrastructure → Application → Domain`). The business logic in `Application` does not know about EF Core,
SQLite, SignalR or Claude, so you can swap any of them and unit-test the logic with simple fakes.
The front end separates *container* components (know about state) from *presentational* components (only inputs/outputs).

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Node.js](https://nodejs.org/) 22.12+ (or 24)
- Optional: `dotnet tool install --global dotnet-ef` to create new migrations

## Run it

**1. API** (creates `chatbot.db` and applies migrations on startup):

```bash
cd backend/src/ChatBot.Api
dotnet run --launch-profile http
```

- API: http://localhost:5000
- Interactive API docs (Scalar): http://localhost:5000/scalar

**2. Angular app** (in a second terminal):

```bash
cd frontend/chatbot-ui
npm install
npm start
```

Open http://localhost:4200 and start chatting.

## Use Claude instead of the demo bot

By default `ChatBot:Provider` is `Demo`, so everything works without an API key.
To talk to Claude, get an API key from the [Claude Console](https://platform.claude.com/) and keep it **out of git** with user-secrets:

```bash
cd backend/src/ChatBot.Api
dotnet user-secrets init
dotnet user-secrets set "ChatBot:Provider" "Claude"
dotnet user-secrets set "ChatBot:Claude:ApiKey" "<your-api-key>"
```

(Or set the `ANTHROPIC_API_KEY` environment variable and `ChatBot__Provider=Claude`.)

Other settings in `appsettings.json` → `ChatBot:Claude`:

| Setting        | Default            | Meaning                                                  |
| -------------- | ------------------ | -------------------------------------------------------- |
| `Model`        | `claude-opus-5-5`  | Claude model to use                                      |
| `MaxTokens`    | `16000`            | Maximum length of one reply                              |
| `Effort`       | `low`              | `low` / `medium` / `high` / `xhigh` / `max` thinking effort |
| `SystemPrompt` | friendly assistant | Instructions that define the bot's personality           |

The Claude requests enable server-side **refusal fallbacks** (`fallbacks: "default"`), so if Claude declines a request the API
retries it on a suitable fallback model inside the same call.

## API endpoints

| Method | URL                                   | Description                               |
| ------ | ------------------------------------- | ----------------------------------------- |
| GET    | `/api/conversations`                  | List conversations (newest first)         |
| GET    | `/api/conversations/{id}`             | One conversation with its messages        |
| POST   | `/api/conversations`                  | Create `{ "title": "optional" }`          |
| PATCH  | `/api/conversations/{id}`             | Rename `{ "title": "..." }`               |
| DELETE | `/api/conversations/{id}`             | Delete a conversation                     |
| POST   | `/api/conversations/{id}/messages`    | Send `{ "content": "..." }`, wait for full reply |
| HUB    | `/hubs/chat` → `SendMessage(id, text)` | Same, but streams the reply in chunks    |

Errors are returned as [problem details](https://www.rfc-editor.org/rfc/rfc9457) (`404` not found, `400` validation).

## Tests

```bash
cd backend && dotnet test          # domain + service tests
cd frontend/chatbot-ui && npm test # component tests (Vitest)
```

## Adding a database change

```bash
cd backend
dotnet ef migrations add <Name> -p src/ChatBot.Infrastructure -s src/ChatBot.Api -o Persistence/Migrations
```

The migration is applied automatically the next time the API starts.

## Ideas for next steps

- Add user accounts (ASP.NET Core Identity + JWT) so each user only sees their own chats.
- Render the bot's Markdown replies (e.g. with `ngx-markdown`).
- Rename a chat from the sidebar (the `PATCH` endpoint already exists).
- Add integration tests for the API with `WebApplicationFactory`.
- Add input validation with FluentValidation.
- Dockerize the API and the Angular app.
