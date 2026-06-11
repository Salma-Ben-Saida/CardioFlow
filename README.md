
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
- Application layer: DTOs and interfaces (`IPacsReader` etc.) were added in `CathLab.Application/` to define boundaries and interaction contracts.
- Infrastructure:
	- Implemented EF Core persistence with `CathLabDbContext` in `CathLab.Infrastructure/Persistence` and prepared migrations.
	- Integrated Orthanc PACS client in `CathLab.Infrastructure/Pacs` with a concrete `OrthancPacsReader` and generated models for Orthanc API resources.
	- Added `OrthancPacsContext` to centralize PACS connectivity and configuration.
- Web API: lightweight REST API in `CathLab.WebApi/` exposing PACS-related endpoints (see `Controllers/PacsController.cs`) and configuration in `appsettings.json` / `appsettings.Development.json`.


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

3. Verify PACS connectivity: the Web API exposes PACS-related endpoints (see `CathLab.WebApi/Controllers/PacsController.cs`) which use the Orthanc integration in `CathLab.Infrastructure/Pacs`.


**Configuration notes**

- App settings and connection strings are stored in `CathLab.WebApi/appsettings.json` and `appsettings.Development.json`. Update the Orthanc URL, credentials, and database connection string there for your environment.
- The Orthanc configuration file is located at `database/orthanc-config/orthanc.json` and is referenced by the Docker Compose setup.


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


**CathLab.WebApi**

- `backend/CathLab.WebApi/Program.cs` — application startup and DI wiring:
	- Registers `CathLabDbContext` and `OrthancPacsContext` with connection strings `CathLabDb` and `OrthancPACS`.
	- Registers implementations: `IPacsReader` → `OrthancPacsReader`, `IPatientRepository` → `PatientRepository`, `ICathStudyRepository` → `CathStudyRepository`.
	- Registers `StudyIngestionService` and maps controllers.
- `backend/CathLab.WebApi/Controllers/PacsController.cs` — API endpoints:
	- `GET /api/pacs/studies` — returns pending studies (last 30 days) via `IPacsReader`.
	- `POST /api/pacs/ingest/{accessionNumber}` — ingests a single study using `StudyIngestionService.IngestStudyAsync`.
	- `POST /api/pacs/ingest/all` — ingests all pending studies returned by PACS.


**How the pieces interact (summary)**

- The Web API controller calls `StudyIngestionService` for ingestion operations.
- `StudyIngestionService` queries the PACS through `IPacsReader` (implemented by `OrthancPacsReader`).
- `OrthancPacsReader` reads Orthanc's DB using `OrthancPacsContext` to build `PacsStudyDto` objects.
- The ingestion service uses repository interfaces (`IPatientRepository`, `ICathStudyRepository`) to persist domain entities in `CathLabDbContext`.


**Files to review**

- Domain entities: [backend/CathLab.Domain/Entities/Patient.cs](backend/CathLab.Domain/Entities/Patient.cs), [backend/CathLab.Domain/Entities/CathStudy.cs](backend/CathLab.Domain/Entities/CathStudy.cs), [backend/CathLab.Domain/Entities/RiskProfile.cs](backend/CathLab.Domain/Entities/RiskProfile.cs), [backend/CathLab.Domain/Entities/AppUser.cs](backend/CathLab.Domain/Entities/AppUser.cs)
- Application DTO & interfaces: [backend/CathLab.Application/DTOs/Pacs/PacsStudyDto.cs](backend/CathLab.Application/DTOs/Pacs/PacsStudyDto.cs), [backend/CathLab.Application/Interfaces/IPacsReader.cs](backend/CathLab.Application/Interfaces/IPacsReader.cs)
- Ingestion service: [backend/CathLab.Application/Services/StudyIngestionService.cs](backend/CathLab.Application/Services/StudyIngestionService.cs)
- Persistence and repositories: [backend/CathLab.Infrastructure/Persistence/CathLabDbContext.cs](backend/CathLab.Infrastructure/Persistence/CathLabDbContext.cs), [backend/CathLab.Infrastructure/Persistence/Repositories/PatientRepository.cs](backend/CathLab.Infrastructure/Persistence/Repositories/PatientRepository.cs), [backend/CathLab.Infrastructure/Persistence/Repositories/CathStudyRepository.cs](backend/CathLab.Infrastructure/Persistence/Repositories/CathStudyRepository.cs)
- Orthanc PACS integration: [backend/CathLab.Infrastructure/Pacs/OrthancPacsContext.cs](backend/CathLab.Infrastructure/Pacs/OrthancPacsContext.cs), [backend/CathLab.Infrastructure/Pacs/OrthancPacsReader.cs](backend/CathLab.Infrastructure/Pacs/OrthancPacsReader.cs)
- API: [backend/CathLab.WebApi/Controllers/PacsController.cs](backend/CathLab.WebApi/Controllers/PacsController.cs), [backend/CathLab.WebApi/Program.cs](backend/CathLab.WebApi/Program.cs)


If you want, I can now:

- Add example `curl` commands for the `/api/pacs` endpoints into this README.
- Add a compact diagram (ASCII or PlantUML) summarizing the data flow from Orthanc → ingestion → DB.


