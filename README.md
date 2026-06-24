
# CardioFlow — Backend & Infrastructure Overview

This repository contains the CardioFlow solution. This README summarizes what was implemented in the `backend` folder and describes the repository's infrastructure and how to run the system for development.

**Project structure (relevant parts)**

- `backend/`
	- `CathLab.Application/` — Application layer: DTOs, interfaces, and application services.
	- `CathLab.Domain/` — Domain entities and core business models (e.g. `Patient`, `CathStudy`, `RiskProfile`).
	- `CathLab.Infrastructure/` — Infrastructure code: EF Core `CathLabDbContext`, PACS integration, and persistence.
		- `Pacs/` contains the Orthanc client integration and generated Orthanc models.
		- `Persistence/` contains database context and migrations.
	- `CathLab.WebApi/` — ASP.NET Web API project exposing endpoints (contains `Controllers/`, `Program.cs`, and appsettings).

- `database/` — Local database and container orchestration resources.
	- `docker-compose.yml` and `dockerfile` for development infrastructure.
	- `orthanc-config/` and `OrthancPACS.sql` for setting up Orthanc PACS and initial DB scripts.

- `docs/` — Architecture diagrams (PlantUML files) describing system components and infra.


**Backend: what was implemented**

- Layered solution (Clean/Onion-like structure): Domain, Application, Infrastructure, and WebApi projects are separated to keep concerns clear.
- Domain model: core entity classes representing users, patients, studies, and risk profiles were created under `CathLab.Domain/Entities`.
- Application layer: DTOs and interfaces (`IPacsReader`, `IPatientsRepository` etc.) were added in `CathLab.Application/` to define boundaries and interaction contracts.
- Infrastructure:
	- Implemented EF Core persistence with `CathLabDbContext` in `CathLab.Infrastructure/Persistence` and prepared migrations.
	- Integrated Orthanc PACS client in `CathLab.Infrastructure/Pacs` with a concrete `OrthancPacsReader` and generated models for Orthanc API resources.
	- Added `OrthancPacsContext` to centralize PACS connectivity and configuration.
	- Implemented background services in `CathLab.Infrastructure/Background_Services` for automatic PACS polling and ingestion.
- Web API: lightweight REST API in `CathLab.WebApi/` exposing PACS-related and patient-related endpoints (see `Controllers/`), with CORS enabled for frontend development and configuration in `appsettings.json` / `appsettings.Development.json`.
- Services: `StudyIngestionService` (core PACS ingestion logic) and `PatientService` (patient queries and filtering).


**Infrastructure: what was implemented**

- Local dev environment via Docker Compose: `database/docker-compose.yml` includes an Orthanc container and any other service containers used for local testing.
- Orthanc configuration: `database/orthanc-config/orthanc.json` and an `OrthancPACS.sql` script to prepare any local DB fixtures.
- The repository includes PlantUML diagrams in `docs/` to illustrate component interactions, deployment boundaries, and high-level infrastructure.


**How to build & run (development)**

1. Start infrastructure (Orthanc) with Docker Compose from the `database/` folder:

```bash
cd database
docker-compose up -d
```

2. Build the solution and run the Web API from the repository root (requires .NET 8 SDK):

```bash
cd backend
dotnet build
dotnet run --project CathLab.WebApi/CathLab.WebApi.csproj
```

3. Once running, the Web API will:
	- Expose REST endpoints for querying patients and studies (see API Endpoints section below)
	- Automatically start background workers that poll PACS every 15 minutes for new studies
	- Log ingestion cycles and study metadata to the console

4. Verify PACS connectivity: the Web API exposes PACS and patient endpoints (see `CathLab.WebApi/Controllers/`) which use the Orthanc integration and database persistence.


---

## API Endpoints (for Frontend Development)

**Base URL:** `http://localhost:5152` (default when running locally)

### 1. Get Patients List (with filtering & pagination)

**Endpoint:** `GET /api/patients`

**Query Parameters:**
- `search` (string, optional) — Filter by patient name, national ID, or conquest patient ID
- `sex` (string, optional) — Filter by sex (e.g., "M", "F")
- `status` (string, optional) — Filter by completion status: `"complete"` or `"incomplete"` (empty string = no filter)
- `page` (integer, default: 1) — Page number (1-indexed)
- `pageSize` (integer, default: 20, max: 100) — Number of results per page

**Example Request:**
```
GET http://localhost:5152/api/patients?search=ali&sex=M&status=complete&page=1&pageSize=20
```

**Response Structure:**
```json
{
  "items": [
    {
      "id": "550e8400-e29b-41d4-a716-446655440000",
      "fullName": "Ali Ahmed Hassan",
      "conquestPatientId": "PACS00001",
      "nationalId": "123456789",
      "birthDate": "1985-03-15",
      "sex": "M",
      "bloodGroup": "O+",
      "phoneNumber": "+966501234567",
      "isIncomplete": false,
      "studyCount": 5,
      "createdAt": "2024-01-15T10:30:00Z",
      "updatedAt": "2024-06-10T14:25:00Z"
    }
  ],
  "totalCount": 124,
  "page": 1,
  "pageSize": 20,
  "totalPages": 7
}
```

**Response Field Descriptions:**
- `id` — Unique patient identifier (GUID)
- `fullName` — Patient full name
- `conquestPatientId` — PACS system patient ID
- `nationalId` — National/Government ID number
- `birthDate` — Date of birth (ISO 8601 format, nullable)
- `sex` — Sex/Gender (nullable)
- `bloodGroup` — Blood group type (nullable)
- `phoneNumber` — Contact phone number (nullable)
- `isIncomplete` — Boolean flag: `true` if patient record is incomplete, `false` if complete
- `studyCount` — Number of cath lab studies associated with this patient
- `createdAt` — Record creation timestamp (UTC)
- `updatedAt` — Last modification timestamp (UTC)
- `totalPages` — Total number of pages available (calculated from totalCount / pageSize)

---

### 2. Get Pending PACS Studies (Last 30 Days)

**Endpoint:** `GET /api/pacs/studies`

**Response:** Returns list of studies pending ingestion from PACS (Orthanc)

---

### 3. Ingest Single Study

**Endpoint:** `POST /api/pacs/ingest/{accessionNumber}`

**Description:** Manually trigger ingestion of a specific study from PACS by its accession number

---

### 4. Ingest All Pending Studies

**Endpoint:** `POST /api/pacs/ingest/all`

**Description:** Manually trigger ingestion of all pending studies from PACS (runs automatically every 15 minutes via background service)

---

**CORS Configuration:**
- Frontend URL: `http://127.0.0.1:5500` (configured in `Program.cs`)
- All HTTP methods and headers are allowed

**Configuration notes:**
- App settings and connection strings are stored in `CathLab.WebApi/appsettings.json` and `appsettings.Development.json`
- The Orthanc configuration file is located at `database/orthanc-config/orthanc.json`
- Frontend is expected to run on `http://127.0.0.1:5500` to allow CORS requests to the API

---

**Development tips & next steps**

- Run the Web API and use Postman or curl to exercise the PACS endpoints while Orthanc runs in Docker.
- Add and apply EF Core migrations under `CathLab.Infrastructure/Persistence` as the domain model evolves.
- Expand integration tests for PACS flows (optional): adding unit/integration tests around `IPacsReader` and `OrthancPacsReader` will improve reliability.


**References & diagrams**

- Architecture diagrams: see the `docs/` folder (PlantUML files) for component diagrams and use-case flows.
- PACS integration code: `backend/CathLab.Infrastructure/Pacs`.
- API controllers: `backend/CathLab.WebApi/Controllers`.


If you want, I can also:

- Add a short section with example curl commands for the main PACS API endpoints.
- Create a basic `.env` and a sample `docker-compose.override.yml` to simplify running the stack locally.

-- CardioFlow


## Detailed backend implementation (what I did)

This section documents, file-by-file and class-by-class, the work implemented under `backend/` so reviewers can quickly understand the code and responsibilities.


**CathLab.Domain**

- `backend/CathLab.Domain/Entities/Patient.cs` — `Patient` entity with core patient fields and navigation properties:
	- Properties: `Id`, `CreatedBy`, `ConquestPatientID`, `NationalId`, `FullName`, `BirthDate`, `Sex`, `PhoneNumber`, `Address`, `BloodGroup`, `IsIncomplete`, `CreatedAt`, `UpdatedAt`.
	- Navigation: `CreatedByUser` (to `AppUser`), `RiskProfile`, `Studies` (collection of `CathStudy`).
- `backend/CathLab.Domain/Entities/CathStudy.cs` — `CathStudy` entity representing a cath lab study:
	- Properties: `Id`, `PatientId`, `OperatorId`, `AccessionNumber`, `StudyInstanceUid`, `StudyDate`, `Status`, `CreatedAt`.
	- Navigation: `Patient`, `Operator` (an `AppUser`).
- `backend/CathLab.Domain/Entities/RiskProfile.cs` — risk profile details tied to a `Patient`.
- `backend/CathLab.Domain/Entities/AppUser.cs` — basic user entity: `Id`, `Username`, `PasswordHash`, `FullName`, `Specialty`, `Role`, `IsActive`, `CreatedAt`.


**CathLab.Application**

- `backend/CathLab.Application/DTOs/Pacs/PacsStudyDto.cs` — transfer object used for PACS → application boundaries. Contains study identifiers and basic patient/study metadata (StudyInstanceUid, AccessionNumber, PatientId, PatientName, PatientBirthDate, PatientSex, StudyDate, Modality).
- `backend/CathLab.Application/Interfaces/IPacsReader.cs` — PACS abstraction used by application services. Methods implemented:
	- `Task<PacsStudyDto?> GetStudyByAccessionAsync(string accessionNumber)`
	- `Task<List<PacsStudyDto>> GetPendingStudiesAsync(DateTime since)`
	- `Task<List<PacsStudyDto>> GetPatientStudiesAsync(string patientId)`
- `backend/CathLab.Application/Interfaces/IPatientRepository.cs` — repository contract for patient persistence (Find/Create/Update methods).
- `backend/CathLab.Application/Interfaces/ICathStudyRepository.cs` — repository contract for studies (FindByAccession/Create).
- `backend/CathLab.Application/Services/StudyIngestionService.cs` — core ingestion logic implemented:
	- Uses `IPacsReader` to fetch studies from PACS.
	- Uses `IPatientRepository` and `ICathStudyRepository` to avoid duplicates, create or update patients and persist studies.
	- Public methods: `IngestStudyAsync(string accessionNumber)` and `IngestAllPendingAsync()`.
	- Returns `IngestResult` (success, already exists, or failure with error). The service also contains helper logic to build a `Patient` from a `PacsStudyDto` (formatting DICOM patient name, marking `IsIncomplete` true).


**CathLab.Infrastructure**

- `backend/CathLab.Infrastructure/Persistence/CathLabDbContext.cs` — EF Core DB context for application data.
	- Registers `DbSet<AppUser>`, `DbSet<Patient>`, `DbSet<RiskProfile>`, `DbSet<CathStudy>`.
	- Fluent API config: relationships (Patient ↔ RiskProfile one-to-one, Patient ↔ CathStudy one-to-many), precision for decimals, Id generation, timestamps.
- `backend/CathLab.Infrastructure/Persistence/Repositories/PatientRepository.cs` — concrete `IPatientRepository` implementation using `CathLabDbContext`.
	- Implements `FindByConquestIdAsync`, `CreateAsync`, `UpdateAsync` with proper Id generation and timestamp handling.
- `backend/CathLab.Infrastructure/Persistence/Repositories/CathStudyRepository.cs` — concrete `ICathStudyRepository` implementation.

- PACS integration (Orthanc):
	- `backend/CathLab.Infrastructure/Pacs/OrthancPacsContext.cs` — EF Core context targeting Orthanc's internal database tables (generated models in `Pacs/Generated/*`).
		- Registers `DbSet<MainDicomTag>`, `DbSet<DicomIdentifier>`, `DbSet<Resource>`, and the rest of Orthanc tables.
	- `backend/CathLab.Infrastructure/Pacs/OrthancPacsReader.cs` — `IPacsReader` implementation that reads Orthanc tables via `OrthancPacsContext`:
		- Implements the three methods from `IPacsReader`.
		- Uses DICOM tag constants to locate accession numbers, patient IDs, study UIDs and build `PacsStudyDto`.
		- Key private helpers: `BuildDtoAsync(internalId, publicId)` and `ParseDate(string?)`.
	- Generated models: `backend/CathLab.Infrastructure/Pacs/Generated/*` — classes mapping Orthanc tables (e.g., `MainDicomTag`, `DicomIdentifier`, `Resource`, `AttachedFile`, etc.). These are used by `OrthancPacsContext`.


**CathLab.Infrastructure — Background Services**

- `backend/CathLab.Infrastructure/Background_Services/PacsIngestionWorker.cs` — long-running background service implementing `BackgroundService`:
	- Runs continuously in the background, polling PACS every 15 minutes (900 seconds)
	- Creates a scoped DI container on each cycle to safely call scoped services
	- Uses `StudyIngestionService.IngestAllPendingAsync()` to automatically ingest new studies
	- Logs each ingestion cycle with timestamp and any errors encountered
	- Registered as a hosted service in `Program.cs` via `AddHostedService<PacsIngestionWorker>()`

**CathLab.Application — Patient Service**

- `backend/CathLab.Application/Services/PatientService.cs` — application service for patient queries:
	- Uses `IPatientRepository` to search and filter patients from the database
	- Public method: `GetListAsync(search, sex, status, page, pageSize)` returns paginated results
	- Supports filtering by: patient name/ID (search), sex, completion status (complete/incomplete)
	- Converts domain `Patient` entities to `PatientListItemDto` for API responses
- `backend/CathLab.Application/DTOs/Patients/PatientListItemDto.cs` — transfer object containing patient summary data returned by API

**CathLab.WebApi**

- `backend/CathLab.WebApi/Program.cs` — application startup and DI wiring:
	- Registers `CathLabDbContext` and `OrthancPacsContext` with connection strings `CathLabDb` and `OrthancPACS`.
	- Registers implementations: `IPacsReader` → `OrthancPacsReader`, `IPatientRepository` → `PatientRepository`, `ICathStudyRepository` → `CathStudyRepository`.
	- Registers `StudyIngestionService` and `PatientService`.
	- Adds CORS policy for frontend at `http://127.0.0.1:5500`.
	- Registers `PacsIngestionWorker` as a hosted service for automatic background polling.
- `backend/CathLab.WebApi/Controllers/PacsController.cs` — PACS ingestion endpoints:
	- `GET /api/pacs/studies` — returns pending studies (last 30 days) via `IPacsReader`.
	- `POST /api/pacs/ingest/{accessionNumber}` — ingests a single study using `StudyIngestionService.IngestStudyAsync`.
	- `POST /api/pacs/ingest/all` — ingests all pending studies returned by PACS.
- `backend/CathLab.WebApi/Controllers/PatientsController.cs` — patient management endpoints:
	- `GET /api/patients` — returns paginated, filtered list of patients using `PatientService.GetListAsync`.
		- Query parameters: `search` (name/ID), `sex` (M/F), `status` (complete/incomplete), `page`, `pageSize`.
	- Response: paginated result object with patient summaries (see API Endpoints section above).


**How the pieces interact (summary)**

1. **Manual/On-demand ingestion** (triggered via API):
	- The `PacsController` HTTP endpoint receives a request to ingest studies
	- Controller calls `StudyIngestionService` for ingestion operations
	- `StudyIngestionService` queries the PACS through `IPacsReader` (implemented by `OrthancPacsReader`)
	- `OrthancPacsReader` reads Orthanc's DB using `OrthancPacsContext` to build `PacsStudyDto` objects
	- The ingestion service uses repository interfaces (`IPatientRepository`, `ICathStudyRepository`) to persist domain entities in `CathLabDbContext`

2. **Automatic background ingestion** (runs every 15 minutes):
	- `PacsIngestionWorker` background service wakes up on schedule
	- Creates a scoped DI container and retrieves `StudyIngestionService`
	- Calls `IngestAllPendingAsync()` to automatically query PACS and ingest new studies
	- Logs ingestion activity to console

3. **Patient queries** (via frontend or API client):
	- `PatientsController` receives HTTP GET requests with filtering parameters
	- Controller calls `PatientService.GetListAsync()` to search the database
	- `PatientService` uses `IPatientRepository` to query domain entities and convert them to DTOs
	- Returns paginated results as JSON response


**Files to review**

	---

	## PACS Ingestion Logic

	This project includes both manual and automatic PACS ingestion. The core logic and responsibilities are:

	- Triggering
		- Manual: `POST /api/pacs/ingest/{accessionNumber}` ingests a single study.
		- Manual: `POST /api/pacs/ingest/all` ingests all pending studies.
		- Automatic: `PacsIngestionWorker` runs on a schedule (default: every 15 minutes) and calls `StudyIngestionService.IngestAllPendingAsync()`.

	- Reading PACS
		- `IPacsReader` abstraction is implemented by `OrthancPacsReader`, which reads Orthanc's DB via `OrthancPacsContext`.
		- The reader returns `PacsStudyDto` objects with study metadata (AccessionNumber, StudyInstanceUid, Patient identifiers, StudyDate, Modality).

	- Ingestion service responsibilities (`StudyIngestionService`)
		- Fetch pending studies from `IPacsReader` or a specific study by accession number.
		- For each study, check existing `ICathStudyRepository` to avoid duplicates (idempotency by `AccessionNumber`/StudyInstanceUid).
		- Ensure patient exists: use `IPatientRepository.FindByConquestIdAsync` (or create a new `Patient` entity if missing).
		- Persist `CathStudy` entities via `ICathStudyRepository` and link to `Patient`.
		- Mark records `IsIncomplete` when DICOM patient metadata is missing or ambiguous.

	- Error handling & logging
		- Each ingestion attempt logs success/failure and exceptions. The background worker continues on errors (logs and moves to next study).
		- The service returns structured results (success, already exists, failed) for API responses.

	- Idempotency & safety
		- Ingestion checks for existing studies to avoid duplicates.
		- Re-running ingestion for the same accession is safe and returns an appropriate status (already exists).

	Key implementation files:

	- `backend/CathLab.Infrastructure/Pacs/OrthancPacsReader.cs` — PACS reader implementation.
	- `backend/CathLab.Application/Services/StudyIngestionService.cs` — ingestion business logic.
	- `backend/CathLab.Infrastructure/Persistence/Repositories/*` — repositories used to persist patients and studies.
	- `backend/CathLab.Infrastructure/Background_Services/PacsIngestionWorker.cs` — scheduled background worker.

	This section can be expanded with sequence diagrams or example flows on request.

**Domain & Common:**
- Domain entities: [backend/CathLab.Domain/Entities/Patient.cs](backend/CathLab.Domain/Entities/Patient.cs), [backend/CathLab.Domain/Entities/CathStudy.cs](backend/CathLab.Domain/Entities/CathStudy.cs), [backend/CathLab.Domain/Entities/RiskProfile.cs](backend/CathLab.Domain/Entities/RiskProfile.cs), [backend/CathLab.Domain/Entities/AppUser.cs](backend/CathLab.Domain/Entities/AppUser.cs)
- Common types: [backend/CathLab.Application/Common/PagedResult.cs](backend/CathLab.Application/Common/PagedResult.cs)

**Application Layer:**
- DTOs: [backend/CathLab.Application/DTOs/Pacs/PacsStudyDto.cs](backend/CathLab.Application/DTOs/Pacs/PacsStudyDto.cs), [backend/CathLab.Application/DTOs/Patients/PatientListItemDto.cs](backend/CathLab.Application/DTOs/Patients/PatientListItemDto.cs)
- Interfaces: [backend/CathLab.Application/Interfaces/IPacsReader.cs](backend/CathLab.Application/Interfaces/IPacsReader.cs), [backend/CathLab.Application/Interfaces/IPatientRepository.cs](backend/CathLab.Application/Interfaces/IPatientRepository.cs), [backend/CathLab.Application/Interfaces/ICathStudyRepository.cs](backend/CathLab.Application/Interfaces/ICathStudyRepository.cs)
- Services: [backend/CathLab.Application/Services/StudyIngestionService.cs](backend/CathLab.Application/Services/StudyIngestionService.cs), [backend/CathLab.Application/Services/PatientService.cs](backend/CathLab.Application/Services/PatientService.cs)

**Infrastructure Layer:**
- Persistence: [backend/CathLab.Infrastructure/Persistence/CathLabDbContext.cs](backend/CathLab.Infrastructure/Persistence/CathLabDbContext.cs), [backend/CathLab.Infrastructure/Persistence/Repositories/PatientRepository.cs](backend/CathLab.Infrastructure/Persistence/Repositories/PatientRepository.cs), [backend/CathLab.Infrastructure/Persistence/Repositories/CathStudyRepository.cs](backend/CathLab.Infrastructure/Persistence/Repositories/CathStudyRepository.cs)
- PACS integration: [backend/CathLab.Infrastructure/Pacs/OrthancPacsContext.cs](backend/CathLab.Infrastructure/Pacs/OrthancPacsContext.cs), [backend/CathLab.Infrastructure/Pacs/OrthancPacsReader.cs](backend/CathLab.Infrastructure/Pacs/OrthancPacsReader.cs)
- Background services: [backend/CathLab.Infrastructure/Background_Services/PacsIngestionWorker.cs](backend/CathLab.Infrastructure/Background_Services/PacsIngestionWorker.cs)

**API Layer:**
- Controllers: [backend/CathLab.WebApi/Controllers/PacsController.cs](backend/CathLab.WebApi/Controllers/PacsController.cs), [backend/CathLab.WebApi/Controllers/PatientsController.cs](backend/CathLab.WebApi/Controllers/PatientsController.cs)
- Configuration: [backend/CathLab.WebApi/Program.cs](backend/CathLab.WebApi/Program.cs)


If you want, I can now:

- Add example `curl` commands for the `/api/pacs` endpoints into this README.
- Add a compact diagram (ASCII or PlantUML) summarizing the data flow from Orthanc → ingestion → DB.


