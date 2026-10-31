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

CardioFlow was developed as part of a PFA (Projet de Fin d'Année) project
at <Nom de l'institut>, in collaboration with Clinisys ERP.

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
- 🐳 **One-command deployment**: the API and the web front end run in Docker and start automatically with the machine.

---

## Roles and permissions

| Role | Dashboard | What they can do |
|---|---|---|
| `Admin` | Administration console | Everything: patients, users, reports (edit/delete), PACS ingestion, archive/restore/purge, audit and activity logs, stock overview |
| `Medecin` | Doctor dashboard | Patient files, studies to treat, coronary mapping, report confirmation, PDF export, DICOM viewer |
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
- **One service per concern**: `StudyIngestionService`, `StudySchedulingService`, `PatientService`, `ProcedureService`, `InventoryService`, `UserService`, `AuditService`, `ArchiveService`.
- **Transactions where stock is involved**: reservation, release and consumption run through a unit of work.

### Runtime topology

```
Browser ──► http://localhost  (nginx, container "web")
              ├── /                 → redirects to the landing page
              ├── /frontend/...     → static HTML / CSS / JS
              ├── /viewer/          → compiled DICOM viewer
              └── /api/...          → proxied to container "api" (ASP.NET, port 8080)
                                          ├── SQL Server   (host port 1433)
                                          └── Orthanc PACS (host port 8042)
```

The front end and the viewer call the API with **relative URLs** (`/api/...`), so everything is served from a single origin and no CORS configuration is needed in this setup. SQL Server and Orthanc run in their own containers, outside this compose file, and the API reaches them through `host.docker.internal`.

PlantUML diagrams (use case, components, infrastructure, class diagram, ingestion sequence) are in [`docs/`](docs/).

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
| Infra | Docker Compose (API + web) · Orthanc and SQL Server in separate containers |
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
│   └── Dockerfile                 # API image
├── frontend/
│   ├── landing.html
│   ├── assets/                    # css, shared js (api client, sidebar, toast, utils)
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
├── database/                      # Orthanc docker-compose, Orthanc config, SQL fixture
├── docs/                          # PlantUML diagrams
├── docker-compose.yml             # api + web
└── .env                           # Secrets and local paths (git-ignored, see below)
```

---

## Getting started

### Prerequisites

- [Docker](https://www.docker.com/) with Docker Compose
- A **SQL Server** instance reachable on the host at port `1433` (a container or a native install), holding two databases: the application database (`CathLabDB`) and the Orthanc index database (`OrthancPACS`)
- An **Orthanc** server reachable on the host at port `8042` (the one in `database/` is provided)
- [.NET 8 SDK](https://dotnet.microsoft.com/download) and the EF tool (`dotnet tool install --global dotnet-ef`), **only** to create the database schema the first time

### 1. Clone

```bash
git clone https://github.com/Salma-Ben-Saida/CardioFlow.git
cd CardioFlow
```

### 2. Start Orthanc and SQL Server

The compose file at the repository root runs only the API and the web front end. SQL Server and Orthanc must already be running and must publish their ports on the host (`1433` and `8042`).

Orthanc is provided in `database/`. Edit the volume path in `database/docker-compose.yml` first (it currently points to a macOS path, `/Volumes/My_Disk/orthanc/db`), then:

```bash
cd database
docker compose up -d        # Orthanc: DICOM 4242, HTTP 8042
cd ..
```

`database/OrthancPACS.sql` is a fixture for the Orthanc index database. Orthanc reaches SQL Server through `host.docker.internal` (see `database/orthanc-config/orthanc.json`).

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
  }
}
```

Then apply the migrations:

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
| `Orthanc__Username` / `Orthanc__Password` | Credentials the API uses to call Orthanc. The password must match the `RegisteredUsers` entry in `orthanc.json` exactly |
| `SA_PASSWORD` | Password of the `sa` SQL Server login used in the connection strings |
| `JWT_SECRET` | Signing key of the JSON Web Tokens. Changing it invalidates existing sessions |
| `REPORTS_DIR` | Absolute path of the folder holding the generated PDF reports, mounted at the **same path** inside the container (see [Deployment notes](#deployment-notes)) |

The database name (`CathLabDB`), the JWT issuer and audience, and the Orthanc URL are set in `docker-compose.yml`. Adjust them there if yours differ. The `.env` file is git-ignored.

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

The first user has to be created directly (the user endpoint is admin-only). Insert an `Admin` row in the `AppUsers` table of `CathLabDB` with a hash generated by the application's password hasher, or temporarily remove `[Authorize]` from the create-user endpoint in a local environment, create the admin, and restore it. Then log in and create the other accounts from the admin console.

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

To have everything come back by itself when the machine restarts, enable "Start Docker Desktop when you sign in" and set a restart policy on the SQL Server and Orthanc containers:

```bash
docker update --restart unless-stopped <sqlserver-container> <orthanc-container>
```

The `api` and `web` services already use `restart: unless-stopped`.

### Running the API outside Docker (optional)

For debugging with Swagger, stop the Docker API first (`docker compose stop api`), then:

```bash
cd backend
dotnet run --project CathLab.WebApi
```

API: `http://localhost:5152` · Swagger UI: `http://localhost:5152/swagger` (Development only, disabled in the Docker image). This mode needs `appsettings.Development.json` (step 3) and the Orthanc settings (`Orthanc:Username`, `Orthanc:Password`, and optionally `Orthanc:BaseUrl`, default `http://localhost:8042`).

> Because the front end now calls `/api/...` on its own origin, serving it with a plain static server (Live Server, `npx serve`) no longer reaches the API. Use the Docker `web` container, which proxies `/api` for you.

---

## API reference

Base URL `http://localhost/api` (through nginx). Directly on the API process, `http://localhost:5152` when run outside Docker, with interactive documentation at `/swagger`. All endpoints except login require `Authorization: Bearer <token>`.

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
| `GET` | `/api/cathstudies` | Studies with scheduling status | Admin, Medecin, Secretaire |
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
| `DELETE` | `/api/procedures/lesions/{id}` · `/treatments/{id}` | Remove (releases devices) | Admin, Medecin |

### DICOM

| Method | Endpoint | Description | Roles |
|---|---|---|---|
| `GET` | `/api/dicom/web/{**path}` | DICOMweb proxy to Orthanc (used by the viewer) | Admin, Medecin |
| `GET` | `/api/dicom/study/{studyInstanceUid}/instances` | Series and instances of a study | Admin, Medecin |
| `GET` | `/api/dicom/preview/{id}` · `/frame/{id}/{index}` | Previews and rendered frames | Admin, Medecin |

### Pharmacy

| Method | Endpoint | Description | Roles |
|---|---|---|---|
| `GET` | `/api/pharmacy/items/available` | In-stock devices by type | Admin, ResponsablePharmacie, Medecin |
| `GET` | `/api/pharmacy/items/reserved/{cathStudyId}` | Devices reserved for a study | Admin, ResponsablePharmacie, Medecin |
| `POST` | `/api/pharmacy/items/receive-batch` | Receive stock | Admin, ResponsablePharmacie |

### Administration

Archive, restore and permanently purge patients and studies, the PACS ingestion settings, plus audit and activity logs, are exposed under `/api/admin/*` (`Admin` only).

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
- **Optimistic concurrency**: patient edits carry a `RowVersion`, and stale updates return a conflict.

---

## Security

- JWT Bearer authentication with configurable issuer, audience and lifetime.
- PBKDF2-HMAC-SHA512 password hashing (350,000 iterations) with constant-time comparison.
- Role checks are enforced **in the API**. Hiding a button in the UI is never the only protection.
- In the Docker setup the browser only talks to nginx (single origin). SQL Server and Orthanc are never exposed through the web container. The CORS policy remains restricted to local development origins for the standalone API.
- Secrets are read from environment variables (`.env`, git-ignored), not from the image.

### Known gaps before production

- [ ] Serve over HTTPS (reverse proxy such as Caddy or nginx with a certificate) and enable HSTS. The Docker setup currently serves plain HTTP.
- [ ] Remove the SQL Server and Orthanc passwords stored in plain text in `database/orthanc-config/orthanc.json`, and rotate them.
- [ ] Do not use the `sa` account for the application. Create a dedicated SQL login with the minimum rights.

---

## Troubleshooting

| Symptom | Likely cause and fix |
|---|---|
| Login says "Nom d'utilisateur ou mot de passe incorrect" although the credentials are right | The login page shows this message for **any** error without a JSON message (500, 502, 404). Test the API directly: `curl -i -X POST http://localhost/api/Auth/login -H 'Content-Type: application/json' -d '{"Username":"x","Password":"y"}'`. A `401` with `Identifiants invalides.` means the API works; a `500` means a database or configuration problem, see `docker compose logs api` |
| API log shows SQL error `4060` (`Cannot open database`) | The database name in `docker-compose.yml` does not match yours. List the real names with `SELECT name FROM sys.databases` |
| API log shows `Login failed for user 'sa'` | Wrong `SA_PASSWORD` in `.env` |
| Network error to SQL Server or Orthanc | Check that the containers run and publish ports `1433` / `8042` (`docker ps`) |
| DICOM images do not load | `Orthanc__Password` in `.env` must match `orthanc.json` exactly, and Orthanc must run on port `8042` |
| Existing PDF reports cannot be downloaded | `REPORTS_DIR` must be the exact absolute path stored in the database, see [Deployment notes](#deployment-notes) |
| `apt-get` fails during the API build (`Connection failed`) | Temporary network problem when downloading Debian packages. Retry, or change network |
| Viewer build fails with `UMD and IIFE output formats are not supported for code-splitting builds` | `viewer/vite.config.js` must contain `worker: { format: 'es' }` |

---

## Testing

```bash
cd backend
dotnet test
```

Unit tests for the report rules are in `backend/CathLab.Tests`. See the roadmap for what is still missing.

---

## Deployment notes

- Run `dotnet ef database update` in the CI/CD pipeline or manually, not at application start. The API does not apply migrations or create any user on startup.
- `PacsIngestionWorker` runs inside the API process. With several API instances, run it on one instance only (or add a distributed lock) to avoid duplicate ingestion cycles. The ingestion period is stored in `App_Data/ingestion-settings.json`, kept in a Docker volume.
- **PDF reports**: the database stores the **absolute path** of each generated PDF. The compose file mounts `REPORTS_DIR` at the same path inside the container so that existing reports stay downloadable. On another machine, either use the same path or update the stored paths. Keep this folder on durable storage.
- The Docker images target a single host. The API image installs `libfontconfig1` and `fonts-dejavu-core`, which QuestPDF needs on Linux.
- For a public deployment, put a reverse proxy with HTTPS in front of the `web` container and expose only ports 80/443. Never expose `1433` or `8042`.

---

## To do next

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