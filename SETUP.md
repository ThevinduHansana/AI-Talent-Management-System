# GetCareers — Setup & Run Guide

AI-powered recruitment & talent management platform.
**Stack:** ASP.NET Core 8 (Clean Architecture) + EF Core + PostgreSQL · React 19 + Vite + Tailwind.

This guide gets the project running on a fresh machine (Windows shown; macOS/Linux notes inline).

---

## 1. Prerequisites

Install these three tools. On Windows, in PowerShell:

```powershell
winget install Microsoft.DotNet.SDK.8      # .NET 8 SDK
winget install OpenJS.NodeJS               # Node.js 18+ (includes npm)
winget install PostgreSQL.PostgreSQL.16    # PostgreSQL 16
```

- During the **PostgreSQL** install, note the password you set for the `postgres` superuser — you need it in step 3.
- Close and reopen your terminal afterwards so `dotnet`, `node`, and `psql` are on PATH.

(macOS: `brew install --cask dotnet-sdk`, `brew install node`, `brew install postgresql@16` and `brew services start postgresql@16`.)

Verify:
```powershell
dotnet --version    # 8.x
node --version      # v18+ (v20/22/24 fine)
```

---

## 2. Get the code

Unzip (or `git clone`) into a simple path, e.g. `C:\Projects\AI Recruitment Platform`.

> If this came as a zip, `frontend/node_modules` and `backend/**/bin` & `obj` are **not** included (or should be deleted) — they are regenerated below.

---

## 3. Configure the database connection

The backend **auto-creates the database, applies all migrations, and seeds demo data** on first run — you do **not** need to create the database manually. You only need valid PostgreSQL credentials.

Edit `backend/src/RecruitmentPlatform.API/appsettings.json` → `ConnectionStrings:DefaultConnection`:

```json
"DefaultConnection": "Host=localhost;Port=5432;Database=recruitment_platform;Username=postgres;Password=YOUR_POSTGRES_PASSWORD"
```

Replace `YOUR_POSTGRES_PASSWORD` with the `postgres` superuser password from step 1.

<details>
<summary>Optional: use a dedicated app role instead of the superuser</summary>

```sql
-- run in psql as the postgres superuser
CREATE ROLE recruit_app LOGIN PASSWORD 'Recruit@2026!';
CREATE DATABASE recruitment_platform OWNER recruit_app;
```
Then set `Username=recruit_app;Password=Recruit@2026!` in the connection string.
</details>

---

## 4. Run the backend

```powershell
cd "backend\src\RecruitmentPlatform.API"
dotnet run
```

First run restores packages, creates/migrates/seeds the database, then listens on:

- API + Swagger: **http://localhost:5094/swagger**
- Health check: **http://localhost:5094/health**

Leave this terminal running.

---

## 5. Run the frontend (new terminal)

```powershell
cd "frontend"
npm install
npm run dev
```

Opens at **http://localhost:5173**. The dev server proxies `/api` to the backend on port 5094 automatically (configurable via the `VITE_API_PROXY` env var if you ever change the backend port).

---

## 6. Log in

Open **http://localhost:5173** and use a seeded account:

| Role | Email | Password |
|------|-------|----------|
| Administrator | `admin@recruitment.local` | `Admin@12345` |
| Recruiter | `recruiter@recruitment.local` | `Recruiter@12345` |
| Hiring Manager | `manager@recruitment.local` | `Manager@12345` |
| Candidate | *self-register on the signup page* | — |

Recruiters and hiring managers are provisioned by an admin (Admin → Users → New user); only candidates self-register.

---

## Troubleshooting

| Symptom | Fix |
|--------|-----|
| Backend: `password authentication failed` | Wrong password in the connection string (step 3), or PostgreSQL service not running. |
| Backend: `Npgsql … could not connect` | PostgreSQL isn't running / wrong host or port. Start the `postgresql-x64-16` service. |
| Frontend login fails / API 404 | Backend not running, or on a different port. Confirm http://localhost:5094/health responds; if the backend port differs, start the frontend with `VITE_API_PROXY=http://localhost:<port>`. |
| Port already in use | Something else is on 5094/5173. Stop it, or change the port. |
| `dotnet`/`node` not recognized | Reopen the terminal after installing so PATH updates. |

---

## Ports

| Service | URL |
|--------|-----|
| Frontend (Vite dev) | http://localhost:5173 |
| Backend API + Swagger | http://localhost:5094 |
| PostgreSQL | localhost:5432 |
