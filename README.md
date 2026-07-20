
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
- Web API: lightweight REST API in `CathLab.WebApi/` exposing PACS-related, patient-related, and authentication endpoints (see `Controllers/`), with CORS enabled for frontend development and configuration in `appsettings.json` / `appsettings.Development.json`.
- Services: `StudyIngestionService` (core PACS ingestion logic), `PatientService` (patient queries and filtering), `UserService` (user authentication and JWT token generation), `InventoryService` (device stock management), and `ProcedureService` (procedure and lesion treatment tracking).
- Authentication: JWT-based authentication system with secure password hashing (PBKDF2 with SHA512), implemented via `UserService`, `AuthController`, and `UserController`.
- Authorization: Role-based access control (RBAC) for sensitive operations: `Admin` and `ResponsablePharmacie` for inventory/pharmacy operations, `Admin` and `Medecin` for procedure documentation.
- Device Reservation Workflow: Devices are reserved when added to a procedure and remain reserved until the procedure report is confirmed, at which point they transition to consumed (reflecting actual clinical use).


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

### 1. Authentication & User Management

#### Register New User

**Endpoint:** `POST /api/user`

**Request Body:**
```json
{
  "username": "john_doe",
  "password": "SecurePassword123!",
  "fullName": "John Doe",
  "role": "ResponsablePharmacie"
}
```

**Response (201 Created):**
```json
{
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "username": "john_doe",
  "fullName": "John Doe",
  "role": "ResponsablePharmacie",
  "isActive": true,
  "createdAt": "2026-07-14T10:30:00Z"
}
```

#### Login & Get JWT Token

**Endpoint:** `POST /api/auth/login`

**Request Body:**
```json
{
  "username": "john_doe",
  "password": "SecurePassword123!"
}
```

**Response (200 OK):**
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "username": "john_doe",
  "fullName": "John Doe"
}
```

**Usage:** Include the token in all authenticated requests:
```
Authorization: Bearer <token>
```

#### List Users (Requires Authentication)

**Endpoint:** `GET /api/user`

**Query Parameters:**
- `page` (integer, default: 1) — Page number (1-indexed)
- `pageSize` (integer, default: 20, max: 100) — Number of results per page

**Response (200 OK):**
```json
[
  {
    "id": "550e8400-e29b-41d4-a716-446655440000",
    "username": "john_doe",
    "fullName": "John Doe",
    "role": "ResponsablePharmacie",
    "isActive": true,
    "createdAt": "2026-07-14T10:30:00Z"
  }
]
```

#### Get User by ID (Requires Authentication)

**Endpoint:** `GET /api/user/{id}`

**Response (200 OK):**
```json
{
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "username": "john_doe",
  "fullName": "John Doe",
  "role": "ResponsablePharmacie",
  "isActive": true,
  "createdAt": "2026-07-14T10:30:00Z"
}
```

---

### 2. Get Patients List (with filtering & pagination)

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

### 3. Get CathStudy by ID

**Endpoint:** `GET /api/cathstudies/{id}`

**Description:** Retrieves study metadata including PatientId (used by coronary mapping UI to resolve patient information from study ID in URL)

**Response (200 OK):**
```json
{
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "patientId": "660e8400-e29b-41d4-a716-446655440000",
  "accessionNumber": "ACC2026070001",
  "studyInstanceUid": "1.2.840.113619.2.55",
  "studyDate": "2026-07-15T09:30:00Z",
  "status": "Ingested",
  "operatorName": "Dr. Smith",
  "createdAt": "2026-07-15T10:00:00Z"
}
```

---

### 4. Get Available Inventory Items

**Endpoint:** `GET /api/pharmacy/items/available?deviceType=Stent`

**Query Parameters:**
- `deviceType` (string, optional) — Filter by device type (e.g., "Stent", "Ballon")

**Response:** Returns in-stock devices available for procedures

**Example:** Used by coronary mapping to populate device picker for stent/balloon selection

---

### 5. Get Pending PACS Studies (Last 30 Days)

**Endpoint:** `GET /api/pacs/studies`

**Response:** Returns list of studies pending ingestion from PACS (Orthanc)

---

### 6. Ingest Single Study

**Endpoint:** `POST /api/pacs/ingest/{accessionNumber}`

**Description:** Manually trigger ingestion of a specific study from PACS by its accession number

---

### 7. Ingest All Pending Studies

**Endpoint:** `POST /api/pacs/ingest/all`

**Description:** Manually trigger ingestion of all pending studies from PACS (runs automatically every 15 minutes via background service)

---

### 8. Add Stent Treatment to Lesion

**Endpoint:** `POST /api/procedures/lesions/{lesionId}/stent`

**Request Body:**
```json
{
  "inventoryItemId": "550e8400-e29b-41d4-a716-446655440000",
  "stentDiameterMm": 3.0,
  "stentLengthMm": 18.0
}
```

**Description:** Reserves an inventory item (stent) for use in this procedure. Device remains Reserved until report is confirmed.

---

### 9. Add Balloon Treatment to Lesion

**Endpoint:** `POST /api/procedures/lesions/{lesionId}/balloon`

**Request Body:**
```json
{
  "inventoryItemId": "550e8400-e29b-41d4-a716-446655440000",
  "balloonDiameterMm": 2.5,
  "balloonLengthMm": 15.0,
  "isPreDilation": true,
  "isPostDilation": false,
  "maxPressureAtm": 8,
  "inflationDurationSec": 30
}
```

**Description:** Reserves an inventory item (balloon) for use in this procedure. Device remains Reserved until report is confirmed.

---

### 10. Confirm Procedure Report

**Endpoint:** `POST /api/procedures/reports/{reportId}/confirm`

**Response:** 204 No Content on success

**Description:** **Irreversible** operation that finalizes the report and transitions all Reserved devices to Consumed status. This matches the clinical reality that devices have been used on the patient and should no longer be counted as available inventory.

---

**CORS Configuration:**
- Frontend URL: `http://127.0.0.1:5500` (configured in `Program.cs`)
- All HTTP methods and headers are allowed

**JWT Authentication:**
- Configure JWT settings in `appsettings.json`:
  ```json
  "JwtSettings": {
    "Secret": "your-very-long-secret-key-with-at-least-256-bits",
    "Issuer": "CathLabApi",
    "Audience": "CathLabClients",
    "DurationInMinutes": 60
  }
  ```
- All authenticated endpoints require a bearer token in the Authorization header: `Authorization: Bearer <token>`
- Protected endpoints (e.g., `[Authorize]` or `[Authorize(Roles = "Admin")]`) will return 401 Unauthorized without a valid token.
- Role-based authorization is enforced for sensitive operations (e.g., Pharmacy inventory updates require `ResponsablePharmacie` or `Admin` role).

**Configuration notes:**
- App settings and connection strings are stored in `CathLab.WebApi/appsettings.json` and `appsettings.Development.json`
- The Orthanc configuration file is located at `database/orthanc-config/orthanc.json`
- Frontend is expected to run on `http://127.0.0.1:5500` to allow CORS requests to the API

---

**Development tips & next steps**

**Authentication & Security:**
- Update the JWT secret in `appsettings.Development.json` with a real cryptographic key (minimum 256 bits) for production.
- Test the authentication flow: first create a user via `POST /api/user`, then login via `POST /api/auth/login` to get a token.
- Use the bearer token in Swagger UI by clicking the "Authorize" button and pasting the JWT token.
- Password hashing uses PBKDF2 with SHA512 (350,000 iterations) — change iterations in `UserService.CryptoUtils` if needed (higher = more secure but slower).
- Role-based access control: assign appropriate roles (`Admin`, `ResponsablePharmacie`, etc.) when creating users to control access to sensitive operations.

**General Development:**
- Run the Web API and use Postman or curl to exercise the PACS endpoints while Orthanc runs in Docker.
- Add and apply EF Core migrations under `CathLab.Infrastructure/Persistence` as the domain model evolves.
- Expand integration tests for PACS flows (optional): adding unit/integration tests around `IPacsReader` and `OrthancPacsReader` will improve reliability.
- Test protected endpoints by both including and excluding the Authorization header to verify security is working.


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
- `backend/CathLab.Domain/Entities/AppUser.cs` — basic user entity: `Id`, `Username`, `PasswordHash`, `FullName`, `Role`, `IsActive`, `CreatedAt`. (Removed `Specialty` field in migration)
- `backend/CathLab.Domain/Entities/ProcedureReport.cs` — updated with confirmation tracking:
	- `IsConfirmed` (bool) — whether the report has been finalized
	- `ConfirmedAt` (DateTime?) — timestamp of confirmation (when devices transition from Reserved → Consumed)


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
- `backend/CathLab.Infrastructure/Persistence/Repositories/UserRepository.cs` — concrete `IUserRepository` implementation using `CathLabDbContext`.
	- Implements `GetByUsernameAsync` for login queries, `GetByIdAsync` for individual user retrieval, `AddAsync` for user registration, `SaveChangesAsync` for persistence, and `GetAllAsync` for paginated user listings.
	- Uses case-insensitive username lookups for consistency.
	- Applies `AsNoTracking()` in read operations for performance optimization.
- `backend/CathLab.Infrastructure/Persistence/Repositories/CathStudyRepository.cs` — updated with:
	- `FindByIdAsync(id)` — retrieves study with Operator relationship loaded (for coronary mapping UI to display physician name)
- `backend/CathLab.Infrastructure/Persistence/Repositories/InventoryItemRepository.cs` — updated with:
	- `GetAvailableAsync(deviceType)` — queries items with `Status == InStock` and optional device type filtering

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

**CathLab.Application — User Management & Authentication**

- `backend/CathLab.Application/Services/UserService.cs` — application service for user authentication and management:
	- Public methods: `CreateUserAsync(CreateUserDto)` for user registration, `LoginAsync(LoginDto)` for authentication, `GetUsersAsync(page, pageSize)` for pagination, `GetUserByIdAsync(id)` for retrieving individual user details
	- Handles password hashing using PBKDF2 with SHA512 (350,000 iterations for security)
	- Generates JWT tokens with claims for user identification and role-based authorization
	- Implements secure password verification using constant-time comparison to prevent timing attacks
	- JWT token includes user Id (NameIdentifier), Username (Name), and Role claims
- `backend/CathLab.Application/DTOs/Users/CreateUserDto.cs` — DTO for user registration with Username, Password, FullName, and Role
- `backend/CathLab.Application/DTOs/Users/UserDto.cs` — DTO for user responses (excludes PasswordHash for security)
- `backend/CathLab.Application/Interfaces/IUserRepository.cs` — repository contract for user persistence:
	- Methods: `GetByUsernameAsync(username)`, `GetByIdAsync(id)`, `AddAsync(user)`, `SaveChangesAsync()`, `GetAllAsync(page, pageSize)` for pagination
- `backend/CathLab.Application/Interfaces/ICathStudyRepository.cs` — updated with:
	- `FindByIdAsync(id)` — retrieve study details by ID (used by coronary mapping to resolve patient from study)
- `backend/CathLab.Application/Interfaces/IInventoryItemRepository.cs` — updated with:
	- `GetAvailableAsync(deviceType)` — retrieve in-stock devices filtered by type (Stent, Ballon, etc.)

**CathLab.Application — Inventory & Device Management**

- `backend/CathLab.Application/Services/InventoryService.cs` — extended with device availability:
	- `GetAvailableItemsAsync(deviceType)` — retrieves sorted list of in-stock devices by type
	- Updated `ConsumeItemAsync()` to enforce Reserved status (devices must be reserved before consumption)
	- `ReceiveBatchAsync()` — receives new inventory items and records stock movements
- `backend/CathLab.Application/DTOs/Pharmacy/InventoryItemDto.cs` — updated with:
	- `DeviceType` property — descriptive device category (Stent, Ballon, Guidewire, etc.)

**CathLab.Application — Procedure & Treatment Management**

- `backend/CathLab.Application/Services/ProcedureService.cs` — completely refactored device flow:
	- `AddStentTreatmentAsync(dto)` — no longer consumes items immediately; instead **reserves** them for this study
	- `AddBalloonTreatmentAsync(dto, performedBy)` — same reservation pattern as stents
	- NEW: `ConfirmReportAsync(reportId, confirmedBy)` — **irreversible** confirmation: transitions all Reserved devices attached to this procedure to Consumed status. This reflects the clinical reality that devices have been used on the patient.
	- Devices stay Reserved throughout procedure documentation and only become Consumed when the report is confirmed (matching the clinical workflow)
- `backend/CathLab.Application/DTOs/Patients/CathStudyDto.cs` — updated with:
	- `PatientId` property — enables coronary mapping UI to resolve patient when only study ID is known

**CathLab.Application — Patient Service**

- `backend/CathLab.Application/Services/PatientService.cs` — application service for patient queries:
	- Uses `IPatientRepository` to search and filter patients from the database
	- Public method: `GetListAsync(search, sex, status, page, pageSize)` returns paginated results
	- Supports filtering by: patient name/ID (search), sex, completion status (complete/incomplete)
	- Converts domain `Patient` entities to `PatientListItemDto` for API responses
- `backend/CathLab.Application/DTOs/Patients/PatientListItemDto.cs` — transfer object containing patient summary data returned by API

**CathLab.WebApi**

- `backend/CathLab.WebApi/Program.cs` — application startup and DI wiring:
	- **JWT Authentication Configuration:**
		- Reads JWT settings from `appsettings.json` (Secret, Issuer, Audience, DurationInMinutes).
		- Configures `JwtBearer` authentication scheme with token validation (validates issuer, audience, lifetime, and signature).
		- Sets `RoleClaimType = "role"` to ensure role-based authorization attributes (`[Authorize(Roles = "...")]`) work correctly.
		- Adds `AddAuthentication()` and `AddAuthorization()` to enable authorization middleware.
	- **Middleware Pipeline:**
		- `UseAuthentication()` — validates JWT tokens (must be before `UseAuthorization()`).
		- `UseAuthorization()` — enforces role-based access control.
	- **Swagger/OpenAPI Configuration:**
		- Adds JWT security definition so Swagger UI displays a token input field.
		- Applies security requirement globally to show "Authorize" button on all protected endpoints.
	- **Database Contexts:** Registers `CathLabDbContext` and `OrthancPacsContext` with connection strings `CathLabDb` and `OrthancPACS`.
	- **Repository & Service DI:** 
		- Registers `IPacsReader` → `OrthancPacsReader`, `IPatientRepository` → `PatientRepository`, `ICathStudyRepository` → `CathStudyRepository`.
		- Registers `IUserRepository` → `UserRepository` for user persistence.
		- Registers `StudyIngestionService`, `PatientService`, and `UserService`.
	- **CORS policy:** Configures for frontend at `http://127.0.0.1:5500` and other development URLs.
	- **Background Services:** Registers `PacsIngestionWorker` as a hosted service for automatic background polling.
- `backend/CathLab.WebApi/Controllers/PharmacyController.cs` — inventory/pharmacy endpoints:
	- `GET /api/pharmacy/items/reserved/{cathStudyId}` — list devices already reserved for a specific study
	- `GET /api/pharmacy/items/available?deviceType=Stent` — query in-stock devices by type (used by coronary mapping to populate device picker)
	- `POST /api/pharmacy/items/receive-batch` — receive new inventory (requires `Admin` or `ResponsablePharmacie` role)
	- Secure `GetCurrentUserId()` extraction from JWT claims with error handling.
- `backend/CathLab.WebApi/Controllers/ProceduresController.cs` — procedure documentation endpoints:
	- `[Authorize(Roles = "Admin,Medecin")]` — restricts access to physicians and admins
	- `POST /api/procedures/lesions/{lesionId}/stent` — adds stent treatment (reserves inventory item)
	- `POST /api/procedures/lesions/{lesionId}/balloon` — adds balloon treatment (reserves inventory item)
	- NEW: `POST /api/procedures/reports/{reportId}/confirm` — confirms report and transitions all Reserved devices to Consumed (irreversible, matching clinical reality)
- `backend/CathLab.WebApi/Controllers/UserController.cs` — updated:
	- `POST /api/user` — creates new user; CreatedAtAction now properly routes to `GetUserById`
	- `GET /api/user` — paginated list of users (requires authentication)
	- `GET /api/user/{id}` — retrieve individual user by ID (requires authentication)
- `backend/CathLab.WebApi/Controllers/CathStudiesController.cs` — NEW controller:
	- `GET /api/cathstudies/{id}` — retrieves study with PatientId (allows coronary mapping to resolve patient from study ID in URL)


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
- DTOs: [backend/CathLab.Application/DTOs/Pacs/PacsStudyDto.cs](backend/CathLab.Application/DTOs/Pacs/PacsStudyDto.cs), [backend/CathLab.Application/DTOs/Patients/PatientListItemDto.cs](backend/CathLab.Application/DTOs/Patients/PatientListItemDto.cs), [backend/CathLab.Application/DTOs/Patients/CathStudyDto.cs](backend/CathLab.Application/DTOs/Patients/CathStudyDto.cs), [backend/CathLab.Application/DTOs/Users/UserDto.cs](backend/CathLab.Application/DTOs/Users/UserDto.cs), [backend/CathLab.Application/DTOs/Users/CreateUserDto.cs](backend/CathLab.Application/DTOs/Users/CreateUserDto.cs), [backend/CathLab.Application/DTOs/Pharmacy/InventoryItemDto.cs](backend/CathLab.Application/DTOs/Pharmacy/InventoryItemDto.cs)
- Interfaces: [backend/CathLab.Application/Interfaces/IPacsReader.cs](backend/CathLab.Application/Interfaces/IPacsReader.cs), [backend/CathLab.Application/Interfaces/IPatientRepository.cs](backend/CathLab.Application/Interfaces/IPatientRepository.cs), [backend/CathLab.Application/Interfaces/ICathStudyRepository.cs](backend/CathLab.Application/Interfaces/ICathStudyRepository.cs), [backend/CathLab.Application/Interfaces/IUserRepository.cs](backend/CathLab.Application/Interfaces/IUserRepository.cs), [backend/CathLab.Application/Interfaces/IInventoryItemRepository.cs](backend/CathLab.Application/Interfaces/IInventoryItemRepository.cs)
- Services: [backend/CathLab.Application/Services/StudyIngestionService.cs](backend/CathLab.Application/Services/StudyIngestionService.cs), [backend/CathLab.Application/Services/PatientService.cs](backend/CathLab.Application/Services/PatientService.cs), [backend/CathLab.Application/Services/UserService.cs](backend/CathLab.Application/Services/UserService.cs), [backend/CathLab.Application/Services/InventoryService.cs](backend/CathLab.Application/Services/InventoryService.cs), [backend/CathLab.Application/Services/ProcedureService.cs](backend/CathLab.Application/Services/ProcedureService.cs)

**Infrastructure Layer:**
- Persistence: [backend/CathLab.Infrastructure/Persistence/CathLabDbContext.cs](backend/CathLab.Infrastructure/Persistence/CathLabDbContext.cs), [backend/CathLab.Infrastructure/Persistence/Repositories/PatientRepository.cs](backend/CathLab.Infrastructure/Persistence/Repositories/PatientRepository.cs), [backend/CathLab.Infrastructure/Persistence/Repositories/CathStudyRepository.cs](backend/CathLab.Infrastructure/Persistence/Repositories/CathStudyRepository.cs), [backend/CathLab.Infrastructure/Persistence/Repositories/UserRepository.cs](backend/CathLab.Infrastructure/Persistence/Repositories/UserRepository.cs), [backend/CathLab.Infrastructure/Persistence/Repositories/InventoryItemRepository.cs](backend/CathLab.Infrastructure/Persistence/Repositories/InventoryItemRepository.cs)
- PACS integration: [backend/CathLab.Infrastructure/Pacs/OrthancPacsContext.cs](backend/CathLab.Infrastructure/Pacs/OrthancPacsContext.cs), [backend/CathLab.Infrastructure/Pacs/OrthancPacsReader.cs](backend/CathLab.Infrastructure/Pacs/OrthancPacsReader.cs)
- Background services: [backend/CathLab.Infrastructure/Background_Services/PacsIngestionWorker.cs](backend/CathLab.Infrastructure/Background_Services/PacsIngestionWorker.cs)

**API Layer:**
- Controllers: [backend/CathLab.WebApi/Controllers/PacsController.cs](backend/CathLab.WebApi/Controllers/PacsController.cs), [backend/CathLab.WebApi/Controllers/PatientsController.cs](backend/CathLab.WebApi/Controllers/PatientsController.cs), [backend/CathLab.WebApi/Controllers/AuthController.cs](backend/CathLab.WebApi/Controllers/AuthController.cs), [backend/CathLab.WebApi/Controllers/UserController.cs](backend/CathLab.WebApi/Controllers/UserController.cs), [backend/CathLab.WebApi/Controllers/PharmacyController.cs](backend/CathLab.WebApi/Controllers/PharmacyController.cs), [backend/CathLab.WebApi/Controllers/ProceduresController.cs](backend/CathLab.WebApi/Controllers/ProceduresController.cs), [backend/CathLab.WebApi/Controllers/CathStudiesController.cs](backend/CathLab.WebApi/Controllers/CathStudiesController.cs)
- Configuration: [backend/CathLab.WebApi/Program.cs](backend/CathLab.WebApi/Program.cs)

# Updates

## Backend updates

- Added new procedure report DTOs:
  - `backend/CathLab.Application/DTOs/Procedures/ProcedureReportDetailDto.cs`
  - `backend/CathLab.Application/DTOs/Procedures/UpdateReportNarrativeDto.cs`
- Extended `backend/CathLab.Application/DTOs/Procedures/CoronaryLesionDto.cs` with treatment details.
- Extended `backend/CathLab.Application/DTOs/Procedures/LesionTreatmentDto.cs` with product name, lot number, diameter and length.
- Added delete methods to procedure persistence interfaces and repositories:
  - `backend/CathLab.Application/Interfaces/ICoronaryLesionRepository.cs`
  - `backend/CathLab.Application/Interfaces/ILesionTreatmentRepository.cs`
  - `backend/CathLab.Infrastructure/Persistence/Repositories/CoronaryLesionRepository.cs`
  - `backend/CathLab.Infrastructure/Persistence/Repositories/LesionTreatmentRepository.cs`
- Added internal inventory release support in `backend/CathLab.Application/Services/InventoryService.cs`.
- Extended `backend/CathLab.Application/Services/ProcedureService.cs` with:
  - report hydration by cath study id
  - narrative update support
  - treatment deletion
  - lesion deletion
  - transaction-safe inventory release
- Added new API endpoints to `backend/CathLab.WebApi/Controllers/ProceduresController.cs`:
  - GET `/api/procedures/reports/study/{cathStudyId}`
  - PUT `/api/procedures/reports/{reportId}`
  - DELETE `/api/procedures/lesions/{lesionId}`
  - DELETE `/api/procedures/treatments/{treatmentId}`
  - GET `/api/procedures/reports/{reportId}/pdf`
- Updated `backend/CathLab.WebApi/Controllers/UserController.cs` to require `Admin` role on the controller and keep GET users authorized.

## Frontend updates

- Added a new Pharmacy Management UI under `frontend/pages/PharmacyManagement`:
  - `dashboard.html`
  - `main.js`
  - `style.css`
- Implemented REST integration with backend pharmacy API endpoints.
- Aligned frontend payloads with backend DTOs for:
  - product creation
  - item reception
- Added response normalization for backend paged results and item lists.
- Added dashboard views for:
  - products
  - items in stock
  - low stock alerts
  - expiring stock
  - reservations by study
- Added modal dialogs for product creation, item reception and waste operations.
- Added toast notifications, search, filtering, and table loading/error states.
- Added client-side routing and view switching for the Pharmacy management UI.

## Files created or modified

- `backend/CathLab.Application/DTOs/Procedures/ProcedureReportDetailDto.cs`
- `backend/CathLab.Application/DTOs/Procedures/UpdateReportNarrativeDto.cs`
- `backend/CathLab.Application/DTOs/Procedures/CoronaryLesionDto.cs`
- `backend/CathLab.Application/DTOs/Procedures/LesionTreatmentDto.cs`
- `backend/CathLab.Application/Interfaces/ICoronaryLesionRepository.cs`
- `backend/CathLab.Application/Interfaces/ILesionTreatmentRepository.cs`
- `backend/CathLab.Application/Services/InventoryService.cs`
- `backend/CathLab.Application/Services/ProcedureService.cs`
- `backend/CathLab.Infrastructure/Persistence/Repositories/CoronaryLesionRepository.cs`
- `backend/CathLab.Infrastructure/Persistence/Repositories/LesionTreatmentRepository.cs`
- `backend/CathLab.WebApi/Controllers/ProceduresController.cs`
- `backend/CathLab.WebApi/Controllers/UserController.cs`
- `frontend/pages/PharmacyManagement/dashboard.html`
- `frontend/pages/PharmacyManagement/main.js`
- `frontend/pages/PharmacyManagement/style.css`
