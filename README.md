# TalentSphere — AI-Powered Recruitment & Talent Management Platform

A production-grade recruitment platform built with **ASP.NET Core 8** (Clean Architecture) and a
**React 19 + Vite** frontend, backed by **PostgreSQL**.

> **Build status:** All four roles — **candidate, recruiter, hiring manager and administrator** —
> are implemented end-to-end, from PostgreSQL through the REST API to the React UI, plus in-app
> notifications, direct messaging, and AI features (resume parsing/skill extraction and job
> recommendations) — verified by 17 unit/integration tests and 122 live end-to-end assertions.
> See [Roadmap](#roadmap) for the remaining LLM/calendar integrations.

---

## Tech stack

| Layer      | Technology |
|------------|------------|
| Frontend   | React 19, Vite, React Router 7, Tailwind CSS v4, Axios, React Hook Form, React Icons, Recharts, Framer Motion |
| Backend    | ASP.NET Core 8 Web API, EF Core 8, AutoMapper, FluentValidation, Serilog, Swagger/OpenAPI |
| Auth       | JWT access + rotating refresh tokens, BCrypt hashing, role-based authorization |
| Database   | PostgreSQL 16 (EF Core code-first migrations) |
| Testing    | xUnit, FluentAssertions, EF Core InMemory |

## Architecture

Clean Architecture with strict dependency flow (`API → Infrastructure → Application → Domain`):

```
backend/
  src/
    RecruitmentPlatform.Domain          # Entities, enums, domain constants (no dependencies)
    RecruitmentPlatform.Application      # DTOs, interfaces, services, validators, mappings
    RecruitmentPlatform.Infrastructure   # EF Core DbContext, repositories, auth, storage, seeding
    RecruitmentPlatform.API              # Controllers, middleware, DI composition, Swagger
  tests/
    RecruitmentPlatform.Tests            # Unit + integration tests
frontend/
  src/
    api/  components/  contexts/  hooks/  layouts/  pages/  routes/  constants/
```

Key patterns: Repository + Unit of Work, Service layer (no business logic in controllers),
DTO pattern, dependency injection throughout, async/await for all I/O, swappable external-service
abstractions (email, SMS, file storage, calendar).

---

## Quick start with Docker (recommended)

The entire stack — PostgreSQL, the ASP.NET Core API and the React frontend (served by nginx) —
runs with a single command. Only Docker is required.

```bash
docker compose up --build
```

Then open:

- **App (frontend):** http://localhost:8080
- **API + Swagger:** http://localhost:5080/swagger
- **Health check:** http://localhost:5080/health

The API waits for PostgreSQL to become healthy, then **automatically applies EF Core migrations and
seeds** baseline data on first run. The frontend's nginx reverse-proxies `/api` to the API service,
so everything is same-origin (no CORS needed). Data, uploaded resumes and logs persist in named
volumes (`pgdata`, `uploads`, `logs`). Stop with `docker compose down` (add `-v` to wipe volumes).

Configuration has safe defaults so `docker compose up` works as-is; copy `.env.example` to `.env`
to override secrets. **Change `JWT_KEY` and `POSTGRES_PASSWORD` before deploying to production.**

| Variable            | Default                        | Purpose                                  |
|---------------------|--------------------------------|------------------------------------------|
| `POSTGRES_PASSWORD` | `Recruit@2026!`                | Database password                        |
| `JWT_KEY`           | (dev placeholder)              | JWT signing key — **must** be long/random |
| `WEB_PORT`          | `8080`                         | Host port for the frontend               |
| `API_PORT`          | `5080`                         | Host port for the API/Swagger            |

### Compose services

- **db** — `postgres:16-alpine`, with a `pg_isready` healthcheck and a persistent data volume.
- **api** — multi-stage .NET 8 build, runs as a non-root user, `/health` liveness probe; depends on
  `db` being healthy.
- **web** — Vite production build served by nginx (SPA fallback + `/api` reverse proxy); depends on
  `api` being healthy.

---

## Manual setup (without Docker)

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Node.js 20+](https://nodejs.org/) (developed against Node 24)
- [PostgreSQL 16](https://www.postgresql.org/)

## Database setup

Create the database and application role (adjust the password as needed):

```sql
CREATE ROLE recruit_app WITH LOGIN PASSWORD 'Recruit@2026!';
CREATE DATABASE recruitment_platform OWNER recruit_app;
GRANT ALL PRIVILEGES ON DATABASE recruitment_platform TO recruit_app;
```

The connection string lives in `backend/src/RecruitmentPlatform.API/appsettings.json` under
`ConnectionStrings:DefaultConnection`. Override it in production via the
`ConnectionStrings__DefaultConnection` environment variable.

## Running the backend

```bash
cd backend/src/RecruitmentPlatform.API
dotnet run
```

On startup the API **automatically applies migrations and seeds** baseline data (roles,
permissions, an administrator, a sample organization, recruiter, skills and open jobs).

- API base URL: `http://localhost:5080`
- Swagger UI: `http://localhost:5080/swagger`

### Seeded accounts

| Role          | Email                        | Password         |
|---------------|------------------------------|------------------|
| Administrator  | `admin@recruitment.local`    | `Admin@12345`    |
| Recruiter      | `recruiter@recruitment.local`| `Recruiter@12345`|
| Hiring Manager | `manager@recruitment.local`  | `Manager@12345`  |

Candidates self-register from the UI or `POST /api/auth/register`. Sign in as the recruiter to
manage jobs, review the applicant pipeline, run AI ranking and schedule interviews; as the hiring
manager to review shortlisted candidates, score evaluations, add interview feedback and
approve/reject hires; and as the administrator to view the analytics dashboard, manage users,
organizations, roles/permissions and browse the audit log.

**End-to-end demo flow:** apply as a candidate → shortlist, interview and rank as the recruiter →
evaluate and hire as the hiring manager → watch the numbers update on the admin analytics dashboard.

## Running the frontend

```bash
cd frontend
npm install
npm run dev
```

- App: `http://localhost:5173`
- API calls to `/api/*` are proxied to the backend (`http://localhost:5080`) by Vite, so no CORS
  configuration is needed in development. Configure a different backend with `VITE_API_PROXY`.

## Running tests

```bash
cd backend
dotnet test
```

---

## API overview

All endpoints are documented in Swagger with request/response schemas, status codes and JWT
authorization. Highlights:

- **Auth** — `POST /api/auth/{register,login,refresh,logout,forgot-password,reset-password}`, `GET /api/auth/me`
- **Jobs (public)** — `GET /api/jobs` (search/filter/sort/paginate), `GET /api/jobs/{id}`
- **Candidate** — `GET/PUT /api/candidate/profile`; skills, education, experience, certificates CRUD
- **Applications** — `POST /api/applications`, `GET /api/applications`, withdraw, saved jobs
- **Resumes** — upload / download / set primary / delete (PDF & Word, secured & size-limited)
- **Recruiter jobs** — `GET/POST/PUT/DELETE /api/recruiter/jobs` (own jobs, with skills)
- **Recruiter pipeline** — `GET /api/recruiter/applications/by-job/{jobId}`, status changes,
  `POST …/rank` (AI match scoring)
- **Interviews** — `POST /api/recruiter/interviews`, upcoming/by-job listing, cancel
- **Hiring manager** — `GET /api/hiring-manager/{dashboard,review-queue,candidates/{id}}`,
  `POST …/evaluations`, `POST …/interview-feedback`, `POST …/candidates/{id}/{approve,reject}`
- **Admin analytics** — `GET /api/admin/analytics/overview` (totals, hiring rate, top skills,
  department hiring, 6-month application/hire trends, recruiter performance)
- **Admin management** — `/api/admin/users` (CRUD + role assignment), `/api/admin/organizations`
  (+ departments), `/api/admin/roles` (+ permissions), `/api/admin/audit-logs`
- **Notifications** — `GET /api/notifications` (+ unread-count), mark one/all read
- **Messaging** — `POST /api/messages`, `GET /api/messages/conversations`,
  `GET /api/messages/{otherUserId}` (thread, auto-marks read), unread-count
- **AI (candidate)** — `POST /api/ai/resumes/{id}/analyze` (real PDF/DOCX text extraction, then
  Claude for skill extraction, strengths/gaps and completeness insights; optional auto-add to
  profile), `GET /api/ai/recommendations` (open jobs re-ranked by Claude with a per-job rationale),
  `GET /api/ai/applications/{id}/feedback` (automated fit summary, strengths, gaps and next steps).
  Every response carries a `source` field (`claude` or `heuristic`) so clients can label AI output
  honestly.

## Security

JWT auth with rotating refresh tokens, BCrypt password hashing (work factor 12), role-based
endpoint protection, FluentValidation input validation, EF Core parameterized queries (SQL-injection
safe), secured file uploads with content-type and size limits, path-traversal-guarded storage,
global exception handling with RFC 7807 problem responses, and audit logging of security events.

## Roadmap

Implemented — **all four roles end to end**: authentication & RBAC; shared layout & navigation;
candidate module (profile, documents, job board, applications); recruiter module (job CRUD,
applicant pipeline, AI candidate ranking, interview scheduling with calendar sync); hiring-manager
module (review queue, scored evaluations, interview feedback, approve/reject decisions with
notifications); administrator module (analytics dashboard, user management, organizations &
departments, roles & permissions, audit-log viewer); **in-app notifications** (topnav bell + page)
and **direct messaging** (recruiter ↔ candidate conversations with contextual entry points); the AI
services — **Claude-backed** resume analysis, skill extraction, candidate scoring, job
recommendations and automated feedback, behind the swappable `IAiService` / `IMatchingService`
abstractions with deterministic heuristics as the offline fallback; and the testing foundation
(17 xUnit + 122 live end-to-end assertions).

Next, building on the existing abstractions: real calendar-provider integration (Google/Outlook)
behind the existing `ICalendarService`.

### AI configuration

Set `ANTHROPIC_API_KEY` (or `Ai__ApiKey`) to enable the model-backed path; get a key from the
[Anthropic Console](https://console.anthropic.com/settings/keys). Without a key — or if a call
times out, is declined, or returns unparseable output — the same endpoints answer from the
deterministic heuristics, so nothing breaks. Tunables live under the `Ai` section of
`appsettings.json` (`Model`, `Effort`, `MaxTokens`, `TimeoutSeconds`, `MaxResumeCharacters`).

Cost note: recruiter ranking (`POST /api/recruiter/applications/jobs/{id}/rank`) makes one model
call per applicant. Job recommendations use a cheap heuristic to shortlist, then a single call to
re-rank — one request regardless of how many jobs are open.
