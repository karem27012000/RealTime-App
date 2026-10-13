# RealTime-App

A trainee project: a **real-time chat between users**, built with an **ASP.NET Core Web API** (.NET 10) and an **Angular 21** front end.
Users log in with a nickname, chat **one-to-one** or in **group rooms**, and see messages, **typing indicators**, **online status**
and **unread counts** update instantly over **SignalR**. Everything is saved in **SQLite** with **EF Core**.

A full project guide (technologies, architecture, every file, API, concepts) is in
[`docs/RealTime-App-Project-Guide.docx`](docs/RealTime-App-Project-Guide.docx).

```
Angular (http://localhost:4200)
   │  REST     /api/users, /api/rooms         → login, users, rooms, message history
   │  SignalR  /hubs/chat?userId=...          → send messages, typing; receive live events
   ▼
ASP.NET Core Web API (http://localhost:5000)
   ├── Application    → UserService / RoomService / MessageService (business logic)
   ├── Infrastructure → EF Core + SQLite, online-presence tracker
   └── Domain         → User, ChatRoom, RoomMember, Message
```

## Features

- Nickname login (no password). The same nickname brings you back to your account.
- **Private chats** between two users and **group rooms** anyone can create, join and leave.
- Messages are delivered **instantly** to every open tab of every member.
- **Online / last seen** status, **"… is typing"** indicator, **unread badges**.
- Message history with **"Load older messages"** paging.
- Works on desktop and phone, light and dark mode.

## Project structure

```
backend/
├── ChatApp.slnx
├── src/
│   ├── ChatApp.Domain/            # Entities and business rules. No dependencies.
│   │   ├── Entities/User.cs, ChatRoom.cs, RoomMember.cs, Message.cs
│   │   ├── Enums/RoomType.cs
│   │   └── Exceptions/DomainException.cs
│   ├── ChatApp.Application/       # Use cases. Depends only on Domain.
│   │   ├── Abstractions/          # Interfaces: repositories, IChatNotifier, IPresenceTracker
│   │   ├── DTOs/                  # What the API sends and receives
│   │   ├── Exceptions/            # NotFound (404), Forbidden (403), Unauthorized (401)
│   │   ├── Mappings/              # Entity → DTO
│   │   └── Services/              # UserService, RoomService, MessageService
│   ├── ChatApp.Infrastructure/    # Technical details. Depends on Application.
│   │   ├── Persistence/           # AppDbContext, configurations, repositories, migrations
│   │   └── Presence/              # InMemoryPresenceTracker (who is online)
│   └── ChatApp.Api/               # Entry point.
│       ├── Controllers/           # UsersController, RoomsController
│       ├── Hubs/                  # ChatHub, IChatClient, SignalRChatNotifier, user-id provider
│       ├── Middleware/            # GlobalExceptionHandler (problem details)
│       └── Program.cs
└── tests/
    └── ChatApp.Application.Tests/ # xUnit tests with hand-written fakes (no database needed)

frontend/chat-ui/src/app/
├── core/
│   ├── models/                    # TypeScript interfaces matching the API DTOs
│   ├── services/                  # AuthService, UsersApi, RoomsApi, ChatHubService (SignalR)
│   ├── interceptors/              # Adds the X-User-Id header to API calls
│   └── guards/                    # authGuard / guestGuard
├── features/
│   ├── login/login-page/          # Pick a nickname
│   └── chat/
│       ├── chat.store.ts          # Signal-based state + real-time event handling
│       ├── chat-page/             # Container component
│       ├── sidebar/               # Chats / People / Groups tabs
│       ├── room-header/           # Name, online status, members, Leave
│       ├── message-list/          # Bubbles, dates, load older
│       └── message-input/         # Text box, typing notifications
├── app.config.ts                  # HttpClient + interceptor, router
└── app.routes.ts                  # /login, /chat, /chat/:roomId
```

**Why this structure?** The backend follows *Clean Architecture*: dependencies point inwards
(`Api → Infrastructure → Application → Domain`). The business logic doesn't know about EF Core, SQLite or SignalR:
it talks to interfaces like `IChatNotifier`, which the Api layer implements with SignalR. That makes it easy to
unit-test with fakes and to swap technologies. The front end separates the *container* component (knows the state)
from *presentational* components (only inputs/outputs), with all state in one signal-based store.

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download) (Visual Studio 2026, VS Code or Rider — Visual Studio 2022 does not support .NET 10)
- [Node.js](https://nodejs.org/) **22.12+ or 24** (older versions fail with a `require() of ES Module` error)
- Optional: `dotnet tool install --global dotnet-ef` to create new migrations

## Run it

**1. API** (creates `chatapp.db` and applies migrations on startup):

```bash
cd backend/src/ChatApp.Api
dotnet run --launch-profile http
```

- API: http://localhost:5000
- Interactive API docs (Scalar): http://localhost:5000/scalar

**2. Angular app** (in a second terminal):

```bash
cd frontend/chat-ui
npm install
npm start
```

**3. Try it:** open http://localhost:4200 in two different browsers (or one normal + one private window),
log in with two nicknames, and chat between them.

## API

REST endpoints (except login) need the header `X-User-Id: <your user id>`; the Angular app adds it automatically.

| Method | URL                               | Description                                   |
| ------ | --------------------------------- | --------------------------------------------- |
| POST   | `/api/users/login`                | `{ "nickname": "karem" }` → user (created first time) |
| GET    | `/api/users/me`                   | The current user                              |
| GET    | `/api/users`                      | All users with online status                  |
| GET    | `/api/rooms`                      | My chats (private + groups), newest first     |
| GET    | `/api/rooms/groups`               | Groups I can join                             |
| GET    | `/api/rooms/{id}`                 | Room details with members                     |
| POST   | `/api/rooms/private`              | `{ "userId": "..." }` → open/create private chat |
| POST   | `/api/rooms/groups`               | `{ "name": "Trainees" }` → create group       |
| POST   | `/api/rooms/{id}/join`            | Join a group                                  |
| POST   | `/api/rooms/{id}/leave`           | Leave a group                                 |
| GET    | `/api/rooms/{id}/messages`        | History, oldest first (`?before=<date>&take=50` for older) |
| POST   | `/api/rooms/{id}/messages`        | `{ "content": "Hi" }` → send (also pushed live) |

SignalR hub `/hubs/chat?userId=<id>`:

| Direction        | Name              | Payload                       |
| ---------------- | ----------------- | ----------------------------- |
| client → server  | `SendMessage`     | `roomId, content` → message   |
| client → server  | `StartTyping`     | `roomId`                      |
| server → client  | `ReceiveMessage`  | message                       |
| server → client  | `RoomAdded`       | room (new private chat, created/joined group) |
| server → client  | `MembersChanged`  | `roomId, memberCount`         |
| server → client  | `PresenceChanged` | user (online / offline)       |
| server → client  | `UserTyping`      | `{ roomId, userId, nickname }` |

Errors are returned as [problem details](https://www.rfc-editor.org/rfc/rfc9457): `400` invalid input, `401` unknown user,
`403` not a member, `404` not found.

> **Security note:** nickname login trusts the user id the browser sends, so anyone could pretend to be someone else.
> That's fine for learning; a real app would use passwords and signed tokens (JWT). See *next steps*.

## Tests

```bash
cd backend && dotnet test             # domain rules + services (27 tests)
cd frontend/chat-ui && npm test       # components (7 tests)
```

## Adding a database change

```bash
cd backend
dotnet ef migrations add <Name> -p src/ChatApp.Infrastructure -s src/ChatApp.Api -o Persistence/Migrations
```

The migration is applied automatically the next time the API starts.

## Ideas for next steps

- Real accounts: ASP.NET Core Identity + JWT (protect the REST API and the hub).
- Read receipts ("seen"), message editing/deleting, emoji reactions.
- Send images and files.
- Browser notifications for new messages when the tab is in the background.
- Store online presence in Redis and use the SignalR Redis backplane to run several servers.
- Integration tests with `WebApplicationFactory` and end-to-end tests with Playwright.
- Dockerize the API and the Angular app.
