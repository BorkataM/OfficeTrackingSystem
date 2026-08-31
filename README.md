# Office Days

See who is in the office, and when. People join a **team**, mark each day as
_In office_, _Remote_, _Travelling_ or _Away_, and the team's week shows up as one
grid.

- **Backend** — .NET 10, Clean Architecture, minimal APIs, EF Core + SQLite, JWT with rotating refresh tokens
- **Frontend** — Angular 20 standalone components, signals, a hand-rolled SCSS design system with light and dark themes

---

## Running it

Two terminals. The database is created and seeded on first start, so there is
nothing to set up.

**1. API** (http://localhost:5055)

```bash
cd src/backend/OfficeSystem.Api && dotnet run
```

**2. Web** (http://localhost:4200)

```bash
cd src/frontend/office-system-web && npm start
```

Open http://localhost:4200. The dev server proxies `/api` to the backend, so the
browser only ever talks to one origin.

### Demo accounts

The development database is seeded with a **Platform Team** of five people. Every
demo account uses the password `Office123!`:

| Email | Role |
| --- | --- |
| `ada@example.com` | Owner |
| `grace@example.com` | Admin |
| `alan@example.com` | Member |
| `katherine@example.com` | Member |
| `linus@example.com` | Member |

The sign-in page has a **Fill in the demo account** button. Seeding only ever runs
against an empty database, and is off in Production.

### API reference

With the API running in Development, the OpenAPI document is at
`/openapi/v1.json` and a browsable reference at http://localhost:5055/scalar.

### Tests

```bash
dotnet test
```

122 tests: the domain rules in isolation, and every use case against a real
in-memory SQLite database so the EF mappings are covered too.

---

## How it is put together

### Backend — four projects, dependencies pointing inward

```
OfficeSystem.Api  ──►  OfficeSystem.Infrastructure  ──►  OfficeSystem.Application  ──►  OfficeSystem.Domain
   (HTTP)                  (EF Core, JWT, BCrypt)            (use cases, ports)          (rules, no deps)
```

**Domain** has no project or package references at all. It holds the three
aggregates and the rules that protect them:

- `User` — identity plus its refresh tokens; rotation and revocation live here
- `Team` — memberships, roles, invite code; enforces "exactly one owner", who may
  promote whom, and that the owner cannot simply walk out
- `AttendanceEntry` — one person's plan for one day; enforces the note length and
  the editable window (30 days back, 365 forward)

Invariants are enforced in constructors and methods, so an invalid aggregate cannot
be constructed. Failures come back as `Result`/`Result<T>` carrying a typed `Error`
— exceptions are for bugs, not for a wrong password.

**Application** holds one folder per use case (command or query + validator +
handler) and declares *ports* for everything it needs from the outside:
`IUserRepository`, `IClock`, `IPasswordHasher`, `ITokenProvider`, `ICurrentUser`.
Read and write sides are separate: commands load aggregates through repositories,
queries project straight to response records through `ITeamReadRepository` /
`IAttendanceReadRepository` and never hydrate an entity they will not change.

Requests reach handlers through a small `IDispatcher` (≈60 lines, no external
mediator). It resolves the handler by type and runs the FluentValidation stage
first, so shape-level validation is impossible to forget. Handlers and validators
are discovered by assembly scan — adding a use case needs no DI wiring.

**Infrastructure** implements the ports: EF Core configurations, repositories,
BCrypt hashing, JWT issuing, the clock. Two model-building conventions are worth
knowing about:

- `SnakeCaseNaming` renames every table, column, key and index, so the schema reads
  like hand-written SQL without being spelled out entity by entity.
- `DomainGeneratedKeys` marks Guid primary keys `ValueGenerated.Never`. Ids are
  minted by the domain with `Guid.CreateVersion7()`; left at its default, EF assumes
  it owns Guid key generation and reads an already-set key on a newly created child
  as "this row exists", issuing an `UPDATE` where an `INSERT` belongs.

**Api** is thin. Endpoints are grouped into `IEndpointModule` implementations
discovered by reflection, so a new feature never edits `Program.cs`. Every endpoint
does one thing: build the command, dispatch it, and hand the `Result` to
`ToHttpResult()`. That extension is the only place an `Error` becomes a status code:

| `ErrorType` | HTTP |
| --- | --- |
| `Validation` | 400 (RFC 9457 problem details, field errors keyed camelCase) |
| `NotFound` | 404 |
| `Conflict` | 409 |
| `Unauthorized` | 401 |
| `Forbidden` | 403 |

Every problem response also carries a stable machine-readable `code`
(`team.adminRequired`, `attendance.tooFarInTheFuture`, …) so the client can react to
a specific failure without matching on prose.

### Frontend — feature-first, signals throughout

```
src/app/
├── core/          api clients, session store, interceptor, guards, theme, dates
├── shared/        avatar, icon, status picker, toast host, status vocabulary
├── layout/        the app shell: sidebar, team switcher, user menu
└── features/      auth · teams · schedule · profile   (each lazy-loaded)
```

State lives in small signal stores (`SessionStore`, `TeamStore`), not in a global
store framework. Components are `OnPush` and read signals directly.

`authInterceptor` attaches the bearer token and, on a 401, refreshes once and
replays the request. Because the API rotates the refresh token on every use,
concurrent 401s would each present the same token and only the first would succeed
— so `TokenRefreshCoordinator` serialises them and the rest wait for the new token.

The design system is one place: `styles/_tokens.scss` defines every colour, space
and radius as a custom property, with dark mode redefining only what changes.
Attendance statuses have a single vocabulary in `shared/attendance.ts` — label,
abbreviation, CSS class, hint — so the grid, the month view and the picker cannot
drift apart.

Dates are handled as `yyyy-MM-dd` strings end to end and parsed at UTC noon, which
sidesteps the off-by-one where a midnight timestamp in a negative UTC offset lands
on the previous day.

### What the UI does

- **Team week** — the grid. Your own row is first and editable: click any cell for a
  status picker with an optional note. Day headers show office occupancy and open a
  "who's in" panel. *Plan my week* marks every weekday at once, copies last week, or
  clears it.
- **My month** — a six-week calendar of your own plan, with per-status day counts.
- **Team** — members and roles, the invite code (admins only, with re-issue), owner
  transfer, remove, leave, delete.
- **All teams** — switch, create, or join with a code. First-run onboarding when you
  are in no team yet.

---

## Configuration

| Setting | Meaning |
| --- | --- |
| `ConnectionStrings:Database` | SQLite connection string |
| `Jwt:SigningKey` | HMAC key, at least 32 characters. **Required** — startup fails without it |
| `Jwt:AccessTokenMinutes` | Access token lifetime (default 30) |
| `Jwt:RefreshTokenDays` | Refresh token lifetime (default 30) |
| `OfficeTime:TimeZone` | The zone whose calendar day counts as "today" for attendance (default `Europe/Berlin`) |
| `Cors:AllowedOrigins` | Origins allowed to call the API |
| `SeedDemoData` | Seed the demo team on an empty database. Defaults to on outside Production |

`appsettings.Development.json` carries a development-only signing key. **Production
must supply its own** via environment variable or a secret store:

```bash
Jwt__SigningKey="<32+ character secret>"
```

Options are validated with data annotations at startup, so a missing or too-short
key fails fast rather than at the first sign-in.

---

## Notes on scope

Deliberately left out, and where they would go:

- **Domain events** — there is no consumer yet, and an abstraction with no consumer
  is dead code. They would sit on the aggregate bases in `Domain/Common`.
- **Migrating at startup** suits SQLite and a single instance. A clustered
  deployment would move `DatabaseInitialiser.MigrateAsync` into a release step.
- **Rate limiting** covers the anonymous auth endpoints, the only ones reachable
  without a token.
