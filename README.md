<div align="center">

# 🫀 CardioFlow

**A clinical cath-lab management platform integrating PACS imaging, patient records, procedure documentation, and pharmacy inventory into a single, auditable workflow.**

[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![EF Core](https://img.shields.io/badge/EF%20Core-8.0-512BD4)](https://learn.microsoft.com/ef/core/)
[![Orthanc PACS](https://img.shields.io/badge/PACS-Orthanc-blue)](https://www.orthanc-server.com/)
[![License](https://img.shields.io/badge/license-MIT-green)](#license)
[![Build](https://img.shields.io/badge/build-passing-brightgreen)](#)

[Overview](#overview) • [Architecture](#architecture) • [Getting Started](#getting-started) • [API Reference](#api-reference) • [Security](#authentication--security) • [Roadmap](#roadmap)

</div>

---

## Overview

CardioFlow is a full-stack system built for interventional cardiology (cath lab) teams. It automates the ingestion of imaging studies from a PACS server (Orthanc), centralizes patient and procedure records, and tracks medical device inventory (stents, balloons, guidewires) through a clinically accurate reservation-to-consumption lifecycle.

**Core capabilities:**

- 🔄 **Automated PACS ingestion** — background worker polls Orthanc every 15 minutes and ingests new studies idempotently.
- 🧑‍⚕️ **Patient & study management** — searchable, paginated patient registry linked to cath lab studies.
- 🩺 **Procedure documentation** — coronary lesion mapping with stent/balloon treatment tracking.
- 💊 **Pharmacy & inventory control** — device reservation on use, irreversible consumption on report confirmation, low-stock and expiry tracking.
- 🔐 **JWT authentication & RBAC** — role-scoped access for `Admin`, `Medecin`, and `ResponsablePharmacie`.

---

## Architecture

CardioFlow follows a **Clean/Onion Architecture**, separating domain logic from infrastructure concerns:

```
┌─────────────────────────────────────────────────────────┐
│                     CathLab.WebApi                       │
│        Controllers · Auth Middleware · Swagger           │
└───────────────────────────┬───────────────────────────────┘
                             │
┌───────────────────────────▼───────────────────────────────┐
│                  CathLab.Application                      │
│     Services · DTOs · Interfaces (ports)                  │
└───────────────────────────┬───────────────────────────────┘
                             │
┌───────────────────────────▼───────────────────────────────┐
│                 CathLab.Infrastructure                    │
│  EF Core Persistence · Orthanc PACS Client · Background   │
│  Workers · Repository Implementations                     │
└───────────────────────────┬───────────────────────────────┘
                             │
┌───────────────────────────▼───────────────────────────────┐
│                    CathLab.Domain                          │
│     Entities · Business Rules (Patient, CathStudy, ...)   │
└─────────────────────────────────────────────────────────┘
```

**Design principles applied:**
- Dependency Inversion — `Application` defines interfaces (`IPacsReader`, `IPatientRepository`, etc.); `Infrastructure` implements them.
- Single Responsibility — one service per domain concern (`StudyIngestionService`, `PatientService`, `UserService`, `InventoryService`, `ProcedureService`).
- Idempotent ingestion — studies are deduplicated by `AccessionNumber` / `StudyInstanceUid`.

Full component and deployment diagrams (PlantUML) are available in [`docs/`](docs/).

---

## Tech Stack

| Layer | Technology |
|---|---|
| API | ASP.NET Core 8 Web API |
| ORM | Entity Framework Core |
| Database | PostgreSQL / SQL Server (via `CathLabDbContext`) |
| Imaging | Orthanc PACS (DICOM) |
| Auth | JWT Bearer + PBKDF2-SHA512 password hashing |
| Frontend | HTML / CSS / Vanilla JS |
| Infra | Docker Compose |
| Docs | PlantUML, Swagger / OpenAPI |

---

## Project Structure

```
CardioFlow/
├── backend/
│   ├── CathLab.Domain/            # Entities & core business models
│   ├── CathLab.Application/       # DTOs, service interfaces, services
│   ├── CathLab.Infrastructure/    # EF Core, PACS client, background jobs
│   │   ├── Pacs/                  # Orthanc integration
│   │   ├── Persistence/           # DbContext, migrations, repositories
│   │   └── Background_Services/   # Scheduled ingestion worker
│   └── CathLab.WebApi/            # Controllers, Program.cs, appsettings
├── frontend/
│   └── pages/PharmacyManagement/  # Pharmacy dashboard (HTML/CSS/JS)
├── database/
│   ├── docker-compose.yml         # Orthanc + supporting containers
│   ├── orthanc-config/            # Orthanc configuration
│   └── OrthancPACS.sql            # DB fixture script
└── docs/                          # PlantUML architecture diagrams
```

---

## Getting Started

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- [Docker](https://www.docker.com/) & Docker Compose
- A running database instance (PostgreSQL/SQL Server, per your connection string)

### 1. Clone & configure

```bash
git clone https://github.com/<your-org>/cardioflow.git
cd cardioflow
```

Create your local settings from the template:

```bash
cp backend/CathLab.WebApi/appsettings.Development.json.example \
   backend/CathLab.WebApi/appsettings.Development.json
```

Set the following in `appsettings.Development.json`:

```json
{
  "ConnectionStrings": {
    "CathLabDb": "<your-connection-string>",
    "OrthancPACS": "<orthanc-db-connection-string>"
  },
  "JwtSettings": {
    "Secret": "replace-with-a-real-256-bit-secret",
    "Issuer": "CathLabApi",
    "Audience": "CathLabClients",
    "DurationInMinutes": 60
  }
}
```

> ⚠️ **Never commit real secrets.** Use environment variables or a secrets manager in any shared or production environment.

### 2. Start infrastructure

```bash
cd database
docker-compose up -d
```

This brings up the Orthanc PACS server and any supporting containers.

### 3. Apply migrations & run the API

```bash
cd backend
dotnet restore
dotnet ef database update --project CathLab.Infrastructure --startup-project CathLab.WebApi
dotnet run --project CathLab.WebApi/CathLab.WebApi.csproj
```

The API will be available at `http://localhost:5152`, with Swagger UI at `/swagger`.

### 4. Verify

- Register a user → `POST /api/user`
- Log in → `POST /api/auth/login`
- Authorize Swagger with the returned bearer token
- Trigger a manual PACS ingest → `POST /api/pacs/ingest/all`

---

## API Reference

**Base URL:** `http://localhost:5152`
Interactive documentation: `http://localhost:5152/swagger`

### Auth & Users

| Method | Endpoint | Description | Auth |
|---|---|---|---|
| `POST` | `/api/user` | Register a new user | `Admin` |
| `POST` | `/api/auth/login` | Authenticate, receive JWT | Public |
| `GET` | `/api/user` | Paginated user list | `Admin` |
| `GET` | `/api/user/{id}` | Get user by ID | `Admin` |

### Patients & Studies

| Method | Endpoint | Description |
|---|---|---|
| `GET` | `/api/patients` | Search/filter/paginate patients |
| `GET` | `/api/cathstudies/{id}` | Get study metadata (incl. `PatientId`) |

### PACS Ingestion

| Method | Endpoint | Description |
|---|---|---|
| `GET` | `/api/pacs/studies` | List pending studies (last 30 days) |
| `POST` | `/api/pacs/ingest/{accessionNumber}` | Ingest a single study |
| `POST` | `/api/pacs/ingest/all` | Ingest all pending studies |

### Procedures & Lesion Treatment

| Method | Endpoint | Description | Auth |
|---|---|---|---|
| `GET` | `/api/procedures/reports/study/{cathStudyId}` | Get report for a study | `Admin, Medecin` |
| `PUT` | `/api/procedures/reports/{reportId}` | Update report narrative | `Admin, Medecin` |
| `POST` | `/api/procedures/lesions/{lesionId}/stent` | Reserve a stent for a lesion | `Admin, Medecin` |
| `POST` | `/api/procedures/lesions/{lesionId}/balloon` | Reserve a balloon for a lesion | `Admin, Medecin` |
| `DELETE` | `/api/procedures/lesions/{lesionId}` | Delete a lesion | `Admin, Medecin` |
| `DELETE` | `/api/procedures/treatments/{treatmentId}` | Delete a treatment | `Admin, Medecin` |
| `POST` | `/api/procedures/reports/{reportId}/confirm` | **Irreversible.** Finalizes report, moves reserved devices to Consumed | `Admin, Medecin` |
| `GET` | `/api/procedures/reports/{reportId}/pdf` | Export report as PDF | `Admin, Medecin` |

### Pharmacy & Inventory

| Method | Endpoint | Description | Auth |
|---|---|---|---|
| `GET` | `/api/pharmacy/items/available` | In-stock devices, filterable by type | Authenticated |
| `GET` | `/api/pharmacy/items/reserved/{cathStudyId}` | Devices reserved for a study | Authenticated |
| `POST` | `/api/pharmacy/items/receive-batch` | Receive new inventory stock | `Admin, ResponsablePharmacie` |

<details>
<summary><strong>Example: Login request/response</strong></summary>

```http
POST /api/auth/login
Content-Type: application/json

{
  "username": "john_doe",
  "password": "SecurePassword123!"
}
```

```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "username": "john_doe",
  "fullName": "John Doe"
}
```

Use it on subsequent requests:

```
Authorization: Bearer <token>
```

</details>

---

## Domain Model Highlights

- **Device lifecycle:** `InStock → Reserved → Consumed`. Devices are reserved the moment they're added to a procedure and only become `Consumed` when the report is confirmed — mirroring real clinical use rather than optimistic deduction at order time.
- **Idempotent ingestion:** every ingested study is deduplicated by `AccessionNumber` / `StudyInstanceUid`; re-running ingestion is always safe.
- **Incomplete records:** patients created from ambiguous or partial DICOM metadata are flagged `IsIncomplete` for manual review.

---

## Authentication & Security

- **JWT Bearer authentication** with configurable issuer, audience, and expiry.
- **Password hashing:** PBKDF2-HMAC-SHA512, 350,000 iterations, constant-time comparison to mitigate timing attacks.
- **Role-based access control (RBAC):**
  - `Admin` — full access
  - `Medecin` — procedure documentation
  - `ResponsablePharmacie` — inventory/pharmacy operations
- **CORS:** locked to the configured frontend origin (`http://127.0.0.1:5500` in development).

**Production hardening checklist:**
- [ ] Replace the JWT secret with a securely generated 256-bit+ key stored in a secrets manager (Azure Key Vault, AWS Secrets Manager, etc.), never in source control.
- [ ] Serve over HTTPS only; enable HSTS.
- [ ] Set restrictive CORS origins for production domains.
- [ ] Enable rate limiting on `/api/auth/login`.
- [ ] Rotate JWT signing keys periodically.

---

## Testing

```bash
cd backend
dotnet test
```

> Integration tests around `IPacsReader` / `OrthancPacsReader` and the ingestion pipeline are recommended next steps — see [Roadmap](#roadmap).

---

## Deployment Notes

- The API is stateless aside from the background worker; it can be horizontally scaled behind a load balancer, with the `PacsIngestionWorker` restricted to a single active instance (e.g., via a distributed lock or a dedicated worker deployment) to avoid duplicate ingestion cycles.
- Run EF Core migrations as part of your CI/CD pipeline (`dotnet ef database update`) rather than on application startup in production.
- Containerize `CathLab.WebApi` alongside Orthanc using the provided `database/docker-compose.yml` as a starting point for a full `docker-compose.prod.yml`.

---

## Roadmap

- [ ] Automated integration test suite for PACS ingestion and inventory flows
- [ ] Structured logging (Serilog) with correlation IDs across ingestion cycles
- [ ] Health check endpoints (`/health`, `/health/ready`)
- [ ] Distributed locking for the background ingestion worker in multi-instance deployments
- [ ] OpenAPI client generation for the frontend
- [ ] CI pipeline (build, test, lint) via GitHub Actions

---

## Contributing

1. Fork the repository and create a feature branch: `git checkout -b feature/your-feature`
2. Follow existing project conventions (Clean Architecture boundaries, DTOs at the application layer, no domain logic in controllers).
3. Add/update tests for any new service logic.
4. Open a pull request with a clear description of the change and its motivation.

---

## License

Distributed under the MIT License. See `LICENSE` for details.

---

## Acknowledgments

- [Orthanc](https://www.orthanc-server.com/) — open-source, lightweight DICOM server powering PACS integration.
- Architecture diagrams generated with [PlantUML](https://plantuml.com/).