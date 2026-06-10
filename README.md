
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

