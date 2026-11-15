<div align="center">

# 🫀 CardioFlow

**A cath-lab management platform that brings PACS imaging, patient records, coronary procedure reports and pharmacy inventory into one auditable workflow.**

[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![EF Core](https://img.shields.io/badge/EF%20Core-8.0-512BD4)](https://learn.microsoft.com/ef/core/)
[![SQL Server](https://img.shields.io/badge/DB-SQL%20Server-CC2927?logo=microsoftsqlserver)](https://www.microsoft.com/sql-server)
[![Orthanc PACS](https://img.shields.io/badge/PACS-Orthanc-blue)](https://www.orthanc-server.com/)
[![Docker](https://img.shields.io/badge/deploy-Docker%20Compose-2496ED?logo=docker)](https://www.docker.com/)
[![License](https://img.shields.io/badge/license-MIT-green)](#license)

[Overview](#overview) • [Roles](#roles-and-permissions) • [Architecture](#architecture) • [Getting Started](#getting-started) • [API](#api-reference) • [Security](#security) • [Roadmap](#roadmap)

</div>

---

## Context

CardioFlow was developed as part of a PFA (Projet de Fin d'Année) project at the Higher Institute of Computer Science and Multimedia of Sfax (ISIMS), in collaboration with Clinisys ERP.

## Overview

CardioFlow is a full-stack system for interventional cardiology teams. It ingests imaging studies from an Orthanc PACS server, centralises patient and procedure records, and tracks implantable devices (stents, balloons) from reservation to consumption.

**Demo:** [Watch the walkthrough](https://www.loom.com/share/4bee607c69e347c893e80f4a47192c54)

**Main features**

- 🔄 **Automated PACS ingestion**: a background worker polls Orthanc and ingests new studies idempotently (deduplicated by `AccessionNumber` / `StudyInstanceUid`). Studies can also be ingested on demand from the PACS screen.
- 🗓️ **Intervention planning**: ingested studies are scheduled, rescheduled or cancelled from a planning screen.
- 🧑‍⚕️ **Patient registry**: searchable, filterable, paginated, with incomplete-record detection, risk profile, optimistic concurrency and archiving.
- 🩺 **Coronary mapping**: place lesions on an interactive vessel tree, attach stents and balloons, generate the report text.
- 📄 **Report lifecycle**: draft, then irreversible confirmation, then PDF export. Confirmed reports can only be amended or deleted by an administrator.
- 💊 **Pharmacy & inventory**: device reservation, consumption on confirmation, low-stock and expiry tracking, stock movement history.
- 🖼️ **DICOM viewer**: a Cornerstone-based viewer (built with Vite, served by nginx at `/viewer/`), reading from Orthanc through the API.
- 🧾 **Audit trail**: every sensitive action (confirm, update, delete, archive, restore, purge, stock operations) is logged, along with user activity (login/logout).
- 🔐 **JWT authentication with role-based access control.**
- 🐳 **Simple deployment**: the API and the web front end run in Docker and restart automatically with the machine.

---

## Roles and permissions

| Role | Dashboard | What they can do |
|---|---|---|
| `Admin` | Administration console | Everything: patients, users, reports (edit/delete), PACS ingestion, archive/restore/purge, audit and activity logs, stock overview |
| `Medecin` | Doctor dashboard | Patient files, studies to treat, coronary mapping, report confirmation, PDF export, DICOM viewer, read access to available/reserved devices |
| `Secretaire` | Reception dashboard | Patient registry (edit), intervention planning, on-demand PACS ingestion. **No access** to DICOM viewer, coronary mapping, reports or PDFs |
| `ResponsablePharmacie` | Pharmacy dashboard | Products, batches, stock levels, expiry, movements |

Business rules worth knowing:

- A **confirmed report is locked for doctors**. Only an `Admin` can amend its text fields (approach, catheter, conclusion, recommendations). The PDF is regenerated and the change is audit-logged.
- Lesions and treatments of a confirmed report stay locked, because their devices are already `Consumed`.
- **Deleting a draft** releases its reserved devices back to stock. **Deleting a confirmed report** keeps its devices `Consumed` (they were physically used) and reopens the study.

---

## Architecture

Clean / onion architecture. Dependencies point inward.

```
┌─────────────────────────────────────────────────────────┐
│                     CathLab.WebApi                       │
│        Controllers · JWT auth · CORS · Swagger           │
└───────────────────────────┬─────────────────────────────┘
                            │
┌───────────────────────────▼─────────────────────────────┐
│                  CathLab.Application                     │
│        Services · DTOs · Interfaces (ports)              │
└───────────────────────────┬─────────────────────────────┘
                            │
┌───────────────────────────▼─────────────────────────────┐
│                 CathLab.Infrastructure                   │
│   EF Core · Orthanc reader · QuestPDF · Background       │
│   workers · Repositories                                 │
└───────────────────────────┬─────────────────────────────┘
                            │
┌───────────────────────────▼─────────────────────────────┐
│                     CathLab.Domain                       │
│           Entities · Enums · Business rules              │
└─────────────────────────────────────────────────────────┘
```

Design principles:

- **Dependency inversion**: `Application` defines `IPacsReader`, `IProcedureReportRepository`, `IPdfGenerator`, and so on. `Infrastructure` implements them.
- **One service per concern**: `StudyIngestionService`, `StudySchedulingService`, `PatientService`, `PatientLifecycleService` (archive / restore / purge), `PacsOverviewService`, `ProcedureService`, `InventoryService`, `UserService`, `AuditService`.
- **Transactions where stock is involved**: reservation, release and consumption run through a unit of work.

### Runtime topology

```
Browser ──► http://localhost  (nginx, container "web")
              ├── /                 → redirects to /frontend/landing.html
              ├── /frontend/...     → static HTML / CSS / JS
              ├── /viewer/          → compiled DICOM viewer
              └── /api/...          → proxied to container "api" (ASP.NET, port 8080)
                                          ├── SQL Server   (host port 1433)
                                          └── Orthanc PACS (host port 8042)
```

The front end and the viewer call the API with **relative URLs** (`/api/...`), so everything is served from a single origin and no CORS configuration is needed in this setup. SQL Server and Orthanc run in their own containers or installs, outside the root compose file, and the API reaches them through `host.docker.internal`.

PlantUML diagrams (use case, components, infrastructure, deployment, class diagram, ingestion sequence, plus per-sprint diagrams) are in [`docs/`](docs/).

---

## Tech stack

| Layer | Technology |
|---|---|
| API | ASP.NET Core 8 Web API |
| ORM / DB | Entity Framework Core 8 · **SQL Server** (application DB and read-only Orthanc DB) |
| Imaging | Orthanc (DICOM) · Cornerstone3D viewer · Vite |
| PDF | QuestPDF |
| Auth | JWT Bearer · PBKDF2-HMAC-SHA512 password hashing |
| Frontend | HTML / CSS / vanilla JavaScript (ES modules), no build step (the viewer is the only compiled part) |
| Web server | nginx (static files, reverse proxy to the API) |
| Infra | Docker Compose (API + web) · Orthanc and SQL Server run separately |
| Docs | Swagger / OpenAPI · PlantUML |

---

## Project structure

```
CardioFlow/
├── backend/
│   ├── CathLab.Domain/            # Entities, enums, domain rules
│   ├── CathLab.Application/       # DTOs, interfaces, services
│   ├── CathLab.Infrastructure/    # EF Core, migrations, Orthanc access, PDF, workers
│   ├── CathLab.WebApi/            # Controllers, Program.cs, configuration
│   ├── CathLab.Tests/             # xUnit tests
│   ├── CathLab.sln
│   └── Dockerfile                 # API image
├── frontend/
│   ├── landing.html
│   ├── style.css
│   ├── assets/                    # shared css / js
│   └── pages/
│       ├── login.html
│       ├── Admin/                 # Administration console
│       ├── dashboard/             # Doctor dashboard
│       ├── secretaire/            # Reception dashboard
│       ├── PharmacyManagement/    # Pharmacy dashboard
│       ├── patients/              # List, detail, coronary mapping
│       ├── studies/               # Intervention planning
│       └── pacs/                  # PACS studies (doctor / admin / secretary)
├── viewer/                        # Standalone DICOM viewer (Vite + Cornerstone)
├── deploy/
│   ├── web.Dockerfile             # Builds the viewer, then serves everything with nginx
│   └── nginx.conf                 # Landing redirect, /api proxy, viewer headers
├── database/                      # Orthanc image + compose file, SQL script
├── docs/                          # PlantUML diagrams
├── docker-compose.yml             # api + web
└── .env                           # Secrets and local paths (git-ignored, create it yourself)
```

---

## Getting started

### Prerequisites

- [Docker](https://www.docker.com/) with Docker Compose
- A **SQL Server** instance reachable on the host at port `1433` (a container or a native install), holding two databases: the application database (`CathLabDB`) and the Orthanc index database (`OrthancPACS`)
- An **Orthanc** server reachable on the host at port `8042` (an image definition and compose file are provided in `database/`)
- [.NET 8 SDK](https://dotnet.microsoft.com/download) and the EF tool (`dotnet tool install --global dotnet-ef`), **only** to create the database schema the first time

### 1. Clone

```bash
git clone https://github.com/Salma-Ben-Saida/CardioFlow.git
cd CardioFlow
```

### 2. Start Orthanc and SQL Server

The compose file at the repository root runs only the API and the web front end. SQL Server and Orthanc must already be running and must publish their ports on the host (`1433` and `8042`). SQL Server is not provided by this repository.

`database/` contains a `dockerfile` (the `orthancteam/orthanc` image plus the Microsoft ODBC driver, so Orthanc can use SQL Server as its index) and a `docker-compose.yml`. Two things must be prepared before starting it:

1. **Orthanc configuration.** The compose file mounts `database/orthanc-config/orthanc.json`, but this file is **git-ignored and not included in the repository** because it contains credentials. Create it yourself. It must configure the SQL Server index (`OrthancPACS`) and declare the account used by the API, for example:

   ```json
   {
     "AuthenticationEnabled": true,
     "RegisteredUsers": { "cardioflow-api": "<password>" }
   }
   ```

   The username and password must match `Orthanc__Username` / `Orthanc__Password` in your `.env` (step 4).
2. **Volume path.** Edit the storage volume in `database/docker-compose.yml`, which currently points to a macOS path (`/Volumes/My_Disk/orthanc/db`).

Then:

```bash
cd database
docker compose up -d        # container "cathlab-orthanc": DICOM 4242, HTTP 8042
cd ..
```

`database/OrthancPACS.sql` is only a small helper script (it creates a `dbo.Resources` table if it does not exist). It is not a full schema: Orthanc creates its own index tables in `OrthancPACS`.

### 3. Create the database schema (first time only)

Skip this step if the databases already exist. Otherwise create `backend/CathLab.WebApi/appsettings.Development.json` (git-ignored, no template is provided):

```json
{
  "ConnectionStrings": {
    "CathLabDb": "Server=localhost,1433;Database=CathLabDB;User Id=...;Password=...;TrustServerCertificate=True",
    "OrthancPACS": "Server=localhost,1433;Database=OrthancPACS;User Id=...;Password=...;TrustServerCertificate=True"
  },
  "JwtSettings": {
    "Secret": "replace-with-a-random-secret-of-at-least-32-characters",
    "Issuer": "CathLabApi",
    "Audience": "CathLabClient",
    "DurationInMinutes": 60
  },
  "Orthanc": {
    "BaseUrl": "http://localhost:8042",
    "Username": "cardioflow-api",
    "Password": "..."
  }
}
```

The API refuses to start if `Orthanc:Username` and `Orthanc:Password` are missing, so they are required here too. Then apply the migrations:

```bash
cd backend
dotnet restore
dotnet ef database update --project CathLab.Infrastructure --startup-project CathLab.WebApi
cd ..
```

> ⚠️ Never commit real secrets.

### 4. Create the `.env` file

Create a file named `.env` **at the repository root**, next to `docker-compose.yml`. Docker Compose reads it automatically.

```dotenv
Orthanc__Username=cardioflow-api
Orthanc__Password=<same password as in database/orthanc-config/orthanc.json>
SA_PASSWORD=<SQL Server password>
JWT_SECRET=<random secret, at least 32 characters>
REPORTS_DIR=/absolute/path/to/backend/CathLab.WebApi/reports
```

| Variable | Meaning |
|---|---|
| `Orthanc__Username` / `Orthanc__Password` | Credentials the API uses to call Orthanc. They must match the `RegisteredUsers` entry in your `orthanc.json` exactly |
| `SA_PASSWORD` | Password of the `sa` SQL Server login used in the connection strings |
| `JWT_SECRET` | Signing key of the JSON Web Tokens. Changing it invalidates existing sessions |
| `REPORTS_DIR` | Absolute path of a host folder for PDF reports, mounted at the **same path** inside the container (see [Deployment notes](#deployment-notes)) |

The database names (`CathLabDB`, `OrthancPACS`), the JWT issuer and audience, and the Orthanc URL are set in `docker-compose.yml`. Adjust them there if yours differ. The `.env` file is git-ignored.

### 5. Build and start

```bash
docker compose up -d --build
```

The first build takes several minutes (images, `npm ci` for Cornerstone, .NET compilation). Then open:

```
http://localhost
```

The landing page is displayed, and **Se connecter** leads to the login page. From another machine on the same network, use `http://<ip-of-the-host>`.

### 6. First login

The first user has to be created directly, because the user endpoints are admin-only and the API does not seed any account. Insert an `Admin` row in the `AppUsers` table of `CathLabDB` with a hash generated by the application's password hasher, or temporarily remove `[Authorize]` from the create-user endpoint in a local environment, create the admin, and restore it. Then log in and create the other accounts from the admin console.

### Day-to-day commands

Run these from the repository root.

| Situation | Command |
|---|---|
| Start the app (nothing changed) | `docker compose up -d` |
| After changing C# code | `docker compose up -d --build api` |
| After changing HTML / CSS / JS or the viewer | `docker compose up -d --build web` |
| After changing `.env` or `docker-compose.yml` | `docker compose up -d` |
| Check the services | `docker compose ps` |
| Follow the API logs | `docker compose logs -f api` |
| Stop without deleting anything | `docker compose stop` |
| Remove the containers (data is kept) | `docker compose down` |

`--build` is only needed when the code changed, because the code is copied into the image at build time.

To have everything come back by itself when the machine restarts, enable "Start Docker Desktop when you sign in" and set a restart policy on the SQL Server container (the Orthanc container in `database/` already uses `restart: unless-stopped`):

```bash
docker update --restart unless-stopped <sqlserver-container>
```

The `api` and `web` services also use `restart: unless-stopped`.

### Running the API outside Docker (optional)

For debugging with Swagger, stop the Docker API first (`docker compose stop api`), then:

```bash
cd backend
dotnet run --project CathLab.WebApi
```

API: `http://localhost:5152` · Swagger UI: `http://localhost:5152/swagger` (Development environment only, disabled in the Docker image, which runs in Production). This mode needs `appsettings.Development.json` (step 3). The API also loads a `.env` file found in a parent folder, so the Orthanc settings can be provided as `Orthanc__Username` / `Orthanc__Password` there.

> Because the front end calls `/api/...` on its own origin, serving it with a plain static server (Live Server, `npx serve`) does not reach the API. Use the Docker `web` container, which proxies `/api` for you.

---

## API reference

Base URL `http://localhost/api` (through nginx). Directly on the API process, `http://localhost:5152` when run outside Docker, with interactive documentation at `/swagger`. All endpoints except login require `Authorization: Bearer <token>`. The tables below list the main endpoints; Swagger has the complete list.

### Auth and users

| Method | Endpoint | Description | Roles |
|---|---|---|---|
| `POST` | `/api/auth/login` | Authenticate, receive JWT | Public |
| `POST` | `/api/auth/logout` | Record logout in the activity log | Authenticated |
| `GET/POST/PUT/DELETE` | `/api/user` · `/api/user/{id}` | User management | `Admin` |

### Patients, studies and planning

| Method | Endpoint | Description | Roles |
|---|---|---|---|
| `GET` | `/api/patients` | Search, filter, sort, paginate | Admin, Medecin, Secretaire |
| `GET/PUT` | `/api/patients/{id}` | Read / edit (with `RowVersion`) | Admin, Medecin, Secretaire |
| `GET` | `/api/patients/{id}/studies` | Studies of a patient | Admin, Medecin, Secretaire |
| `GET` | `/api/patients/{id}/risk-profile` | Risk profile of a patient | Admin, Medecin, Secretaire |
| `GET` | `/api/cathstudies` · `/api/cathstudies/{id}` | Studies with scheduling status | Admin, Medecin, Secretaire |
| `PUT` | `/api/cathstudies/{id}/schedule` | Schedule or reschedule | Admin, Medecin, Secretaire |
| `POST` | `/api/cathstudies/{id}/cancel` | Cancel a pending / scheduled study | Admin, Medecin, Secretaire |

### PACS

| Method | Endpoint | Description | Roles |
|---|---|---|---|
| `GET` | `/api/pacs/studies` | Studies available in Orthanc | Admin, Medecin, Secretaire |
| `GET` | `/api/pacs/overview` | Ingestion summary | Admin, Medecin, Secretaire |
| `POST` | `/api/pacs/ingest/{studyInstanceUid}` | Ingest one study | Admin, Medecin, Secretaire |
| `POST` | `/api/pacs/ingest/all` | Ingest all new studies | `Admin` |

### Procedures and reports

| Method | Endpoint | Description | Roles |
|---|---|---|---|
| `GET` | `/api/reports` | Reports (doctor: own, admin: all or `?doctorId=`) | Admin, Medecin |
| `GET` | `/api/reports/patient/{patientId}` | Report history of a patient | Admin, Medecin |
| `GET` | `/api/procedures/reports/study/{cathStudyId}` | Hydrate coronary mapping | Admin, Medecin |
| `PUT` | `/api/procedures/reports/{reportId}` | Update narrative. Confirmed reports: **Admin only**, PDF regenerated | Admin, Medecin |
| `DELETE` | `/api/procedures/reports/{reportId}` | Delete a report (draft or confirmed) | `Admin` |
| `POST` | `/api/procedures/reports/{reportId}/confirm` | **Irreversible.** Lock report, consume devices, generate PDF | Admin, Medecin |
| `GET` | `/api/procedures/reports/{reportId}/pdf` | Download PDF (confirmed only) | Admin, Medecin |
| `POST` | `/api/procedures/lesions` | Add a lesion | Admin, Medecin |
| `POST` | `/api/procedures/lesions/{id}/stent` · `/balloon` | Add a treatment (reserves a device) | Admin, Medecin |
| `DELETE` | `/api/procedures/lesions/{id}` · `/api/procedures/treatments/{id}` | Remove (releases devices) | Admin, Medecin |

### DICOM

| Method | Endpoint | Description | Roles |
|---|---|---|---|
| `GET` | `/api/dicom/web/{**path}` | DICOMweb proxy to Orthanc (used by the viewer) | Admin, Medecin |
| `GET` | `/api/dicom/study/{studyInstanceUid}/instances` | Series and instances of a study | Admin, Medecin |
| `GET` | `/api/dicom/file/{id}` | Raw DICOM instance | Admin, Medecin |
| `GET` | `/api/dicom/preview/{id}` · `/framecount/{id}` · `/frame/{id}/{index}` | Previews, frame count and rendered frames | Admin, Medecin |

### Pharmacy

| Method | Endpoint | Description | Roles |
|---|---|---|---|
| `GET/POST/PUT/DELETE` | `/api/pharmacy/products` · `/products/{id}` | Product catalogue | Admin, ResponsablePharmacie |
| `GET` | `/api/pharmacy/products/low-stock` | Products below their stock threshold | Admin, ResponsablePharmacie |
| `GET` | `/api/pharmacy/items` · `/items/expiring` | All devices / devices close to expiry | Admin, ResponsablePharmacie |
| `GET` | `/api/pharmacy/items/available` | In-stock devices by type | Admin, ResponsablePharmacie, Medecin |
| `GET` | `/api/pharmacy/items/reserved/{cathStudyId}` | Devices reserved for a study | Admin, ResponsablePharmacie, Medecin |
| `POST` | `/api/pharmacy/items/receive` · `/items/receive-batch` | Receive stock | Admin, ResponsablePharmacie |
| `POST` | `/api/pharmacy/items/{id}/reserve` · `/release` | Reserve / release a device | Admin, ResponsablePharmacie, Medecin |
| `POST` | `/api/pharmacy/items/{id}/waste` | Declare a device as wasted | Admin, ResponsablePharmacie |

### Administration

All under `/api/admin/*` (`Admin` only):

| Method | Endpoint | Description |
|---|---|---|
| `GET` | `/api/admin/audits` · `/logs` · `/movements` | Audit trail, user activity log, stock movements |
| `GET` | `/api/admin/archive` | Archived patients and studies |
| `GET` | `/api/admin/pacs/studies` | PACS studies (admin view) |
| `GET/PUT` | `/api/admin/ingestion-settings` | Read / change the PACS ingestion period |
| `POST` | `/api/admin/patients/{id}/archive` · `/restore` | Archive / restore a patient |
| `POST` | `/api/admin/studies/{id}/archive` · `/restore` | Archive / restore a study |
| `DELETE` | `/api/admin/patients/{id}/purge` · `/api/admin/studies/{id}/purge` | Permanent deletion |

<details>
<summary><strong>Example: login</strong></summary>

```http
POST /api/auth/login
Content-Type: application/json

{ "username": "john_doe", "password": "SecurePassword123!" }
```

```json
{ "token": "eyJhbGciOi...", "username": "john_doe", "fullName": "John Doe", "role": "Medecin" }
```
</details>

---

## Domain model highlights

- **Device lifecycle**: `InStock → Reserved → Consumed`. A device is reserved when added to a treatment and consumed only when the report is confirmed.
- **Study lifecycle**: `Pending → Scheduled → InProgress → Completed` (or `Cancelled`). A scheduled study moves to `InProgress` automatically when its time arrives.
- **Idempotent ingestion**: unique indexes on accession number and study UID make re-running ingestion safe.
- **Incomplete patients**: records built from partial DICOM metadata are flagged `IsIncomplete` for the secretary to complete.
- **Soft delete**: patients and studies are archived first and can be restored. Permanent purge is a separate, type-to-confirm admin action.
- **Optimistic concurrency**: patient edits carry a `RowVersion`, and stale updates return `409 Conflict`.

---

## Security

- JWT Bearer authentication with configurable issuer, audience and lifetime (no clock-skew tolerance).
- PBKDF2-HMAC-SHA512 password hashing (350,000 iterations) with constant-time comparison.
- Role checks are enforced **in the API**. Hiding a button in the UI is never the only protection.
- In the Docker setup the browser only talks to nginx (single origin). SQL Server and Orthanc are never exposed through the web container. The CORS policy of the standalone API only allows local development origins (`localhost`, `127.0.0.1`, StackBlitz / WebContainer sandboxes).
- Secrets are read from environment variables (`.env`, git-ignored), not from the image.

### Known gaps before production

- [ ] Serve over HTTPS (reverse proxy such as Caddy or nginx with a certificate) and enable HSTS. The Docker setup currently serves plain HTTP.
- [ ] Do not store Orthanc and SQL Server passwords in plain text in `database/orthanc-config/orthanc.json`, and rotate any password that was shared.
- [ ] Do not use the `sa` account for the application. Create a dedicated SQL login with the minimum rights.

---

## Troubleshooting

| Symptom | Likely cause and fix |
|---|---|
| Login says "Nom d'utilisateur ou mot de passe incorrect" although the credentials are right | The login page shows this message for **any** error without a JSON message (500, 502, 404). Test the API directly: `curl -i -X POST http://localhost/api/Auth/login -H 'Content-Type: application/json' -d '{"Username":"x","Password":"y"}'`. A `401` with `Identifiants invalides.` means the API works; a `500` means a database or configuration problem, see `docker compose logs api` |
| API container exits at startup with `Orthanc:Username and Orthanc:Password must be configured` | `Orthanc__Username` / `Orthanc__Password` are missing from `.env` |
| API log shows SQL error `4060` (`Cannot open database`) | The database name in `docker-compose.yml` does not match yours. List the real names with `SELECT name FROM sys.databases` |
| API log shows `Login failed for user 'sa'` | Wrong `SA_PASSWORD` in `.env` |
| Network error to SQL Server or Orthanc | Check that the containers run and publish ports `1433` / `8042` (`docker ps`) |
| DICOM images do not load | `Orthanc__Password` in `.env` must match `orthanc.json` exactly, and Orthanc must run on port `8042` |
| Existing PDF reports cannot be downloaded | The database stores absolute paths. The folder holding the PDFs must be available at that exact path inside the container, see [Deployment notes](#deployment-notes) |
| `apt-get` fails during the API build (`Connection failed`) | Temporary network problem when downloading Debian packages. Retry, or change network |
| Viewer build fails with `UMD and IIFE output formats are not supported for code-splitting builds` | `viewer/vite.config.js` must contain `worker: { format: 'es' }` |

---

## Testing

```bash
cd backend
dotnet test
```

Unit tests for the report rules are in `backend/CathLab.Tests` (`ProcedureReportRulesTests`). `UnitTest1.cs` is an empty placeholder. Coverage of the other services is still missing, see the [Roadmap](#roadmap).

---

## Deployment notes

- Run `dotnet ef database update` in the CI/CD pipeline or manually, not at application start. The API does not apply migrations or create any user on startup.
- `PacsIngestionWorker` runs inside the API process. With several API instances, run it on one instance only (or add a distributed lock) to avoid duplicate ingestion cycles. The ingestion period is stored in `App_Data/ingestion-settings.json`, kept in the Docker volume `appdata`.
- **PDF reports**: the API writes PDFs to a `reports` folder under its content root (`/app/reports` in the container) and the database stores the **absolute path** of each file. The compose file mounts `REPORTS_DIR` at the same path inside the container so that reports created on the host stay downloadable. Two consequences: on another machine, use the same path or update the stored paths; and for new reports to survive a container rebuild, `REPORTS_DIR` must point to the folder the API actually writes to (`/app/reports`) or that folder must be mounted separately. Keep this folder on durable storage and back it up.
- The Docker images target a single host. The API image installs `libfontconfig1` and `fonts-dejavu-core`, which QuestPDF needs on Linux.
- For a public deployment, put a reverse proxy with HTTPS in front of the `web` container and expose only ports 80/443. Never expose `1433` or `8042`.

---

## Roadmap

- [ ] Optional first-admin seeding and automatic migrations at startup (opt-in)
- [ ] HTTPS and a dedicated, least-privilege SQL login
- [ ] Health checks (`/health`) and structured logging (Serilog)

---

## Contributing

1. Create a feature branch: `git checkout -b feature/your-feature`
2. Respect the layer boundaries: no business logic in controllers, DTOs at the application layer.
3. Add or update tests for service logic.
4. Open a pull request describing the change and its motivation.

---

## License

Distributed under the MIT License. See `LICENSE`.

## Acknowledgments

- [Orthanc](https://www.orthanc-server.com/) for the open-source DICOM server.
- [Cornerstone3D](https://www.cornerstonejs.org/) for the DICOM viewer components.
- [QuestPDF](https://www.questpdf.com/) for PDF generation.
- [PlantUML](https://plantuml.com/) for the architecture diagrams.