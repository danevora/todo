# Todo App

A task management app: .NET 8 Web API + SQLite on the backend, React + TanStack Query on the frontend.

## Stack

| Layer | Tech |
|---|---|
| Backend | .NET 8, ASP.NET Core Web API |
| Persistence | EF Core 8 + SQLite (file-backed, survives restarts) |
| Frontend | React 19, TypeScript, Vite |
| Server state | TanStack Query + Axios |

## Running it

### Prerequisites
- .NET 8 SDK
- Node.js 18+

### Backend

```bash
dotnet run --project src/TodoApp
# API:     http://localhost:5120
# Swagger: http://localhost:5120/swagger
```

The SQLite database (`todo.db`) is created automatically on first run via `EnsureCreated()`.

### Frontend

```bash
cd frontend
npm install
npm run dev
# App at http://localhost:5173
```

### Tests

```bash
dotnet test
```

## API

| Method | Path | Description |
|---|---|---|
| `GET` | `/api/tasks` | List tasks (paginated, filterable, sortable) |
| `GET` | `/api/tasks/{id}` | Get one task |
| `POST` | `/api/tasks` | Create a task |
| `PATCH` | `/api/tasks/{id}` | Partial update |
| `DELETE` | `/api/tasks/{id}` | Soft delete |
| `POST` | `/api/tasks/{id}/restore` | Restore a soft-deleted task |

`GET /api/tasks` query params: `page` (default 1), `pageSize` (default 20, max 100),
`status` (`Todo`/`InProgress`/`Done`), `priority` (`Low`/`Medium`/`High`),
`sortBy` (`title`/`dueDate`/`priority`/`createdAt`), `sortDesc` (bool).

## Structure

One backend project (`src/TodoApp`) with `Controllers/ → Services/ → Data/` and `Models/`,
and a small test project (`tests/TodoApp.Tests`). A single CRUD resource doesn't need more
than that, so there's no separate domain/application/infrastructure split or repository layer —
the service talks to `DbContext` directly.

## Notable behavior

- **Validation.** Bad input is rejected with `400`, not silently accepted: empty/whitespace
  titles, titles over 500 chars, unknown `status`/`priority` values, and out-of-range
  pagination. All errors (400s and 404s) come back as RFC-7807 ProblemDetails JSON, and the
  `detail` message is shown in the UI.
- **Soft delete with undo.** `DELETE` sets `DeletedAt` and an EF Core global query filter hides
  those rows from every read; nothing is physically removed. `POST /api/tasks/{id}/restore`
  brings a task back, and the UI surfaces an "Undo" action right after a delete.
- **Status as an enum** (`Todo`/`InProgress`/`Done`) rather than a boolean, so adding states
  later doesn't require a data migration.
- **PATCH, not PUT.** Updates send only the fields they include. Omitting a field leaves it
  unchanged; sending an explicit `null` for `notes`/`dueDate` clears it (an `Optional<T>`
  wrapper distinguishes "absent" from "null" in the request body).
- **Due dates are date-only.** The UI sends and renders `YYYY-MM-DD` and formats from the date
  part directly, so the displayed day never shifts across timezones.
- **Live updates.** Create / edit / delete / status-change invalidate the task query, so the
  list reflects changes immediately without a refresh. Failures surface as a banner or inline
  form error rather than failing silently.

## Authentication — intentionally out of scope

There is **no authentication or per-user ownership**. Every task is global and any client can
read or modify any task. This is a deliberate scope decision for the exercise, not an oversight.

Doing it properly is more than a login screen — it means user accounts, scoping every task to
an owner, and enforcing that ownership on every endpoint (so user A can never read or mutate
user B's tasks). Done halfway it's worse than not at all, so it's left out cleanly rather than
stubbed. The production approach would be JWT-based auth with a `UserId` on each task and an
ownership check (and tests) on every read and write.

## Tests

`tests/TodoApp.Tests` runs the real app through `WebApplicationFactory` against an in-memory
SQLite database (real SQL, not EF In-Memory). It focuses on the two highest-risk areas:

- **Validation** — empty titles, unknown enum values, and bad page sizes return `400`.
- **Core CRUD + soft delete + restore** — create/get/patch round-trip and persist; a deleted
  task returns `404` and leaves the list, and restore brings it back.
- **PATCH semantics** — an omitted field is left unchanged while an explicit `null` clears it.
- **Date-only contract** — a due date round-trips as the same calendar day (no timezone shift).

Ownership tests are absent on purpose — there's no auth to enforce (see above).

## With more time

- JWT auth + per-user task scoping (the section above)
- Real EF Core migrations instead of `EnsureCreated()`
- Subtasks, recurring tasks, assignees
