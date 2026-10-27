<div align="center">

# 🫀 CardioFlow

**A cath-lab management platform that brings PACS imaging, patient records, coronary procedure reports and pharmacy inventory into one auditable workflow.**

[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![EF Core](https://img.shields.io/badge/EF%20Core-8.0-512BD4)](https://learn.microsoft.com/ef/core/)
[![SQL Server](https://img.shields.io/badge/DB-SQL%20Server-CC2927?logo=microsoftsqlserver)](https://www.microsoft.com/sql-server)
[![Orthanc PACS](https://img.shields.io/badge/PACS-Orthanc-blue)](https://www.orthanc-server.com/)
[![License](https://img.shields.io/badge/license-MIT-green)](#license)

[Overview](#overview) • [Roles](#roles-and-permissions) • [Architecture](#architecture) • [Getting Started](#getting-started) • [API](#api-reference) • [Security](#security) • [Roadmap](#roadmap)

</div>

---

## Overview

CardioFlow is a full-stack system for interventional cardiology teams. It ingests imaging studies from an Orthanc PACS server, centralises patient and procedure records, and tracks implantable devices (stents, balloons) from reservation to consumption.

**Demo:** [Watch the walkthrough](https://share.descript.com/view/8C6c1Z1BfQR)

**Main features**

- 🔄 **Automated PACS ingestion**: a background worker polls Orthanc and ingests new studies idempotently (deduplicated by `AccessionNumber` / `StudyInstanceUid`).
- 🗓️ **Intervention planning**: ingested studies are scheduled, rescheduled or cancelled from a planning screen.
- 🧑‍⚕️ **Patient registry**: searchable, filterable, paginated, with incomplete-record detection, risk profile, optimistic concurrency and archiving.
- 🩺 **Coronary mapping**: place lesions on an interactive vessel tree, attach stents and balloons, generate the report text.
- 📄 **Report lifecycle**: draft, then irreversible confirmation, then PDF export. Confirmed reports can only be amended or deleted by an administrator.
- 💊 **Pharmacy & inventory**: device reservation, consumption on confirmation, low-stock and expiry tracking, stock movement history.
- 🖼️ **DICOM viewer**: a Cornerstone-based viewer served by Vite, reading from Orthanc.
- 🧾 **Audit trail**: every sensitive action (confirm, update, delete, archive, restore, purge, stock operations) is logged, along with user activity (login/logout).
- 🔐 **JWT authentication with role-based access control.**

---

## Roles and permissions

| Role | Dashboard | What they can do |
|---|---|---|
| `Admin` | Administration console | Everything: patients, users, reports (edit/delete), PACS ingestion, archive/restore/purge, audit and activity logs, stock overview |
| `Medecin` | Doctor dashboard | Patient files, studies to treat, coronary mapping, report confirmation, PDF export, DICOM viewer |
| `Secretaire` | Reception dashboard | Patient registry (create/edit), intervention planning. **No access** to DICOM viewer, coronary mapping, reports or PDFs |
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
| Frontend | HTML / CSS / vanilla JavaScript (ES modules), no build step |
| Infra | Docker Compose (Orthanc) |
| Docs | Swagger / OpenAPI · PlantUML |

---

## Project structure

```
CardioFlow/
├── backend/
│   ├── CathLab.Domain/            # Entities, enums, domain rules
│   ├── CathLab.Application/       # DTOs, interfaces, services
│   ├── CathLab.Infrastructure/    # EF Core, migrations, Orthanc access, PDF, workers
│   └── CathLab.WebApi/            # Controllers, Program.cs, configuration
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
│       └── pacs/                  # PACS studies (doctor / admin)
├── viewer/                        # Standalone DICOM viewer (Vite + Cornerstone)
├── database/                      # docker-compose, Orthanc config, SQL fixture
└── docs/                          # PlantUML diagrams
```

---

## Getting started

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download) and the EF tool: `dotnet tool install --global dotnet-ef`
- [Docker](https://www.docker.com/) with Docker Compose
- A **SQL Server** instance for the application database, and read access to the Orthanc index database
- [Node.js](https://nodejs.org/) 18+ (viewer only)
- Any static file server for the frontend (VS Code Live Server, `npx serve`, …)

### 1. Clone

```bash
git clone https://github.com/Salma-Ben-Saida/CardioFlow.git
cd CardioFlow
```

### 2. Configure the API

`appsettings.Development.json` is git-ignored and there is no template, so create `backend/CathLab.WebApi/appsettings.Development.json`:

```json
{
  "ConnectionStrings": {
    "CathLabDb": "Server=localhost;Database=CathLab;User Id=...;Password=...;TrustServerCertificate=True",
    "OrthancPACS": "Server=localhost;Database=OrthancPACS;User Id=...;Password=...;TrustServerCertificate=True"
  },
  "JwtSettings": {
    "Secret": "replace-with-a-random-secret-of-at-least-32-characters",
    "Issuer": "CathLabApi",
    "Audience": "CathLabClients",
    "DurationInMinutes": 60
  }
}
```

> ⚠️ Never commit real secrets. Use user-secrets or environment variables outside local development.

### 3. Start Orthanc

Edit the volume path in `database/docker-compose.yml`. It currently points to a macOS path (`/Volumes/My_Disk/orthanc/db`), so replace it with a folder on your machine. Then:

```bash
cd database
docker compose up -d        # Orthanc: DICOM 4242, HTTP 8042
```

`database/OrthancPACS.sql` is a fixture for the Orthanc index database.

### 4. Apply migrations and run the API

```bash
cd backend
dotnet restore
dotnet ef database update --project CathLab.Infrastructure --startup-project CathLab.WebApi
dotnet run --project CathLab.WebApi
```

API: `http://localhost:5152` · Swagger UI: `http://localhost:5152/swagger` (Development only).

PDF reports are written to a `reports/` folder in the API's content root.

### 5. Serve the frontend

Serve the **repository root** (pages use absolute paths like `/frontend/pages/login.html`) on port 5500 or 5501, then open:

```
http://localhost:5500/frontend/pages/login.html
```

The API base URL is set in `frontend/assets/js/api/client.js` (`http://localhost:5152` by default).

### 6. Start the DICOM viewer (optional)

```bash
cd viewer
npm install
npm run dev                 # http://localhost:5501
```

The Vite dev server proxies `/orthanc-wadors` to Orthanc on port 8042. If you serve the main frontend on 5501 as well, use 5500 for it, because the viewer needs 5501.

### 7. First login

The first user has to be created directly (the user endpoint is admin-only). Insert an `Admin` row in the `Users` table of `CathLab` with a hash generated by the application's password hasher, or temporarily remove `[Authorize]` from the create-user endpoint in a local environment, create the admin, and restore it. Then log in at `/frontend/pages/login.html` and create the other accounts from the admin console.

---

## API reference

Base URL `http://localhost:5152`. Full interactive documentation is available at `/swagger`. All endpoints except login require `Authorization: Bearer <token>`.

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
| `POST` | `/api/pacs/ingest/{accessionNumber}` | Ingest one study | Admin, Medecin, Secretaire |
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

### Pharmacy

| Method | Endpoint | Description | Roles |
|---|---|---|---|
| `GET` | `/api/pharmacy/items/available` | In-stock devices by type | Authenticated |
| `GET` | `/api/pharmacy/items/reserved/{cathStudyId}` | Devices reserved for a study | Authenticated |
| `POST` | `/api/pharmacy/items/receive-batch` | Receive stock | Admin, ResponsablePharmacie |

### Administration

Archive, restore and permanently purge patients and studies, plus audit and activity logs, are exposed under `/api/admin/*` (`Admin` only).

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
- CORS is restricted to local development origins (`localhost`, `127.0.0.1`).

### Known gaps before production

- [ ] Serve over HTTPS and enable HSTS.

---

## Testing

```bash
cd backend
dotnet test
```

A test project is not included yet. See the roadmap.

---

## Deployment notes

- Run `dotnet ef database update` in the CI/CD pipeline, not at application start.
- `PacsIngestionWorker` runs inside the API process. With several API instances, run it on one instance only (or add a distributed lock) to avoid duplicate ingestion cycles.
- Persist the `reports/` folder (generated PDFs) on durable storage.

---

## Roadmap

- [ ] Unit tests for report rules (lock, admin amendment, delete) and inventory transitions
- [ ] Integration tests for PACS ingestion
- [ ] Authorize DICOM endpoints and the viewer
- [ ] Central API-URL configuration for frontend and viewer
- [ ] Patient header and French labels in the PDF report
- [ ] Search and filters on the admin report list
- [ ] Health checks (`/health`) and structured logging (Serilog)
- [ ] CI pipeline (build, test) with GitHub Actions

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