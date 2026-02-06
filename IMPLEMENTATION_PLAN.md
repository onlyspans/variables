# Variables Service - Implementation Plan

## Overview

**Solution Name:** Onlyspans.Variables
**Organization:** Onlyspans
**Purpose:** Centralized variable storage for release configuration with key-value variables, project/environment scoping, and variable sets (similar to [Octopus Deploy Variables](https://octopus.com/docs/projects/variables))

**Source Requirements:** [onlyspans/issues#1](https://github.com/onlyspans/issues/issues/1)
**Reference Implementation:** `~/Developer/Tariff/p4m.service.fasoauth` (patterns only, not packages)

---

## Phase 0: Documentation & Pattern Discovery (COMPLETED)

### Allowed APIs & Patterns

| Component | Technology | Notes |
|-----------|------------|-------|
| Solution Format | .slnx (XML-based) | Modern solution format |
| Framework | .NET 10 / ASP.NET Core | Latest LTS |
| Domain Models | EF Core entities (no GraphQL) | Plain POCO classes |
| DbContext | PostgreSQL + DbContextPool | Npgsql provider |
| REST API | Minimal APIs with extension methods | From reference |
| gRPC Services | .proto files + service implementations | From reference |
| DI Container | Modular Startup.*.cs partial classes | From reference |
| Configuration | [Strongly.Options](https://github.com/nikitakaralius/Strongly.Options) | NOT P4m packages |
| CQRS | Mediator pattern (ICommand/ICommandHandler) | From reference |
| Migrations | MigrationHostedService (auto-migrate on startup) | From reference |
| Docker | Multi-stage build | From reference |

### Anti-Patterns to Avoid
- Do NOT use GraphQL
- Do NOT use OpenTelemetry
- Do NOT use optimistic concurrency ([Timestamp])
- Do NOT use P4m.* NuGet packages
- Do NOT create explicit EF entity for many-to-many (use EF Core implicit)
- Do NOT use repository pattern (use DbContext directly in handlers)
- Do NOT add Kafka/Wolverine messaging (not required per issue)

---

## Phase 1: Project Scaffolding

### Goal
Create the solution structure matching the reference implementation pattern.

### Tasks

1. **Create solution file (.slnx format)**
   ```
   Onlyspans.Variables.slnx
   ```

2. **Create main API project**
   ```
   src/Onlyspans.Variables.Api/
   ├── Onlyspans.Variables.Api.csproj (net10.0)
   ├── Program.cs
   ├── Startup.cs (partial class)
   ├── Startup.Db.cs
   ├── Startup.Grpc.cs
   ├── Startup.Logging.cs
   ├── Startup.FluentValidation.cs
   ├── Startup.Healthz.cs
   ├── Startup.Mediator.cs
   ├── appsettings.json
   ├── appsettings.Development.json
   └── Dockerfile
   ```

3. **Create directory structure**
   ```
   src/Onlyspans.Variables.Api/
   ├── Abstractions/
   │   └── Services/
   ├── Data/
   │   ├── Contexts/
   │   ├── Entities/
   │   ├── Enums/
   │   ├── Options/
   │   └── Records/
   ├── Endpoints/
   ├── Features/
   │   ├── Variables/
   │   └── VariableSets/
   ├── gRPC/
   │   ├── Protos/
   │   └── Services/
   ├── Migrations/
   └── Services/
   ```

4. **Add NuGet packages**
   - Microsoft.EntityFrameworkCore
   - Npgsql.EntityFrameworkCore.PostgreSQL
   - Microsoft.EntityFrameworkCore.Design (dev)
   - Grpc.AspNetCore
   - Grpc.Tools
   - FluentValidation.DependencyInjectionExtensions
   - Serilog.AspNetCore
   - Serilog.Exceptions
   - Mediator.Abstractions
   - Mediator.SourceGenerator
   - [Strongly.Options](https://github.com/nikitakaralius/Strongly.Options)

### Verification Checklist
- [ ] `dotnet build` succeeds
- [ ] Solution structure matches expected layout
- [ ] All NuGet packages restore successfully

---

## Phase 2: Domain Models & Database

### Goal
Define domain entities and configure Entity Framework Core with PostgreSQL.

### Entities (from issue requirements)

1. **Variable** (`Data/Entities/Variable.cs`)
   ```csharp
   public class Variable {
       public Guid Id { get; init; }
       public required string Key { get; set; }
       public required string Value { get; set; }
       public Guid? EnvironmentId { get; set; }  // Scope (nullable = no scope)

       // Belongs to either Project OR VariableSet (not both)
       public Guid? ProjectId { get; set; }
       public Guid? VariableSetId { get; set; }

       public virtual VariableSet? VariableSet { get; set; }

       public required DateTime CreatedAt { get; set; }
       public required DateTime UpdatedAt { get; set; }
   }
   ```

2. **VariableSet** (`Data/Entities/VariableSet.cs`)
   ```csharp
   public class VariableSet {
       public Guid Id { get; init; }
       public required string Name { get; set; }
       public string? Description { get; set; }

       public virtual ICollection<Variable> Variables { get; set; } = [];

       // Implicit M:M - EF Core will create join table automatically
       public virtual ICollection<Guid> LinkedProjectIds { get; set; } = [];

       public required DateTime CreatedAt { get; set; }
       public required DateTime UpdatedAt { get; set; }
   }
   ```

3. **Many-to-Many (Project ↔ VariableSet)**
   - NO explicit entity - use EF Core's implicit join table
   - Configure in DbContext with shadow properties if needed:
   ```csharp
   // In VariableSetConfiguration.cs
   builder.HasMany<Guid>("LinkedProjectIds")
       .WithMany()
       .UsingEntity("ProjectVariableSets",
           l => l.HasOne(typeof(VariableSet)).WithMany().HasForeignKey("VariableSetId"),
           r => r.HasNoNavigation().HasForeignKey("ProjectId"));
   ```
   - Or simpler: store ProjectIds as a collection and manage manually

4. **ApplicationDbContext** (`Data/Contexts/ApplicationDbContext.cs`)
   ```csharp
   public class ApplicationDbContext(DbContextOptions options) : DbContext(options) {
       public DbSet<Variable> Variables => Set<Variable>();
       public DbSet<VariableSet> VariableSets => Set<VariableSet>();

       protected override void OnModelCreating(ModelBuilder modelBuilder) {
           modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
       }
   }
   ```

5. **Entity Configurations** (`Data/Entities/*.Configuration.cs`)
   - Configure indexes on: ProjectId, VariableSetId, Key, EnvironmentId
   - Configure relationships and constraints

6. **MigrationHostedService** (`Services/MigrationHostedService.cs`)
   - Auto-migrate on startup (copy pattern from reference)

### Verification Checklist
- [ ] `dotnet ef migrations add InitialCreate` succeeds
- [ ] Database schema matches entity definitions
- [ ] Relationships are correctly configured
- [ ] Indexes exist on frequently queried columns (ProjectId, VariableSetId, Key)

---

## Phase 3: Configuration & Infrastructure

### Goal
Set up configuration, logging, and health checks.

### Tasks

1. **Configuration Options using [Strongly.Options](https://github.com/nikitakaralius/Strongly.Options)** (`Data/Options/`)

   **GrpcClientsOptions.cs:**
   ```csharp
   [StronglyOptions("GrpcClients")]
   public sealed partial class GrpcClientsOptions {
       public required string ProjectsServiceUrl { get; init; }
       public required string TargetsPlaneServiceUrl { get; init; }
   }
   ```

   Registration (automatic with source generator):
   ```csharp
   // Startup.cs
   builder.Services.AddStronglyOptions<GrpcClientsOptions>(builder.Configuration);
   ```

2. **appsettings.json**
   ```json
   {
     "Serilog": {
       "MinimumLevel": "Information",
       "Using": ["Serilog.Exceptions"],
       "Enrich": ["FromLogContext", "WithExceptionDetails"],
       "Properties": {
         "Application": "Onlyspans.Variables.Api"
       },
       "WriteTo": [
         {
           "Name": "Console",
           "Args": {
             "outputTemplate": "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}"
           }
         }
       ]
     },
     "ConnectionStrings": {
       "Default": "Server=localhost;Port=5432;Database=variables;User Id=variables;Password=variables;Include Error Detail=true"
     },
     "GrpcClients": {
       "ProjectsServiceUrl": "",
       "TargetsPlaneServiceUrl": ""
     }
   }
   ```

3. **Startup modules** (copy patterns from reference):
   - `Startup.Db.cs` - DbContextPool + MigrationHostedService
   - `Startup.Logging.cs` - Serilog configuration
   - `Startup.FluentValidation.cs` - Validator registration
   - `Startup.Healthz.cs` - Health checks
   - `Startup.Mediator.cs` - Mediator registration

### Verification Checklist
- [ ] Application starts without configuration errors
- [ ] Logs appear in console with correct format
- [ ] Health endpoint responds at `/health`
- [ ] Strongly.Options configuration binding works

---

## Phase 4: REST API (CRUD Operations)

### Goal
Implement REST endpoints for frontend CRUD operations.

### Endpoints

**Variables:**
| Method | Path | Description |
|--------|------|-------------|
| GET | `/api/projects/{projectId}/variables` | List project variables |
| POST | `/api/projects/{projectId}/variables` | Create project variable |
| PUT | `/api/variables/{id}` | Update variable |
| DELETE | `/api/variables/{id}` | Delete variable |

**Variable Sets:**
| Method | Path | Description |
|--------|------|-------------|
| GET | `/api/variable-sets` | List all variable sets |
| GET | `/api/variable-sets/{id}` | Get variable set with variables |
| POST | `/api/variable-sets` | Create variable set |
| PUT | `/api/variable-sets/{id}` | Update variable set |
| DELETE | `/api/variable-sets/{id}` | Delete variable set |
| POST | `/api/variable-sets/{id}/variables` | Add variable to set |

**Project-VariableSet Links:**
| Method | Path | Description |
|--------|------|-------------|
| GET | `/api/projects/{projectId}/variable-sets` | List linked sets |
| POST | `/api/projects/{projectId}/variable-sets/{setId}` | Link set to project |
| DELETE | `/api/projects/{projectId}/variable-sets/{setId}` | Unlink set |

### Implementation Pattern
Copy from `fasoauth/src/.../Endpoints/` - use Minimal API extension methods:

```csharp
// Endpoints/VariablesEndpoint.cs
public static class VariablesEndpoint {
    public static WebApplication MapVariablesEndpoints(this WebApplication app) {
        app.MapGet("/api/projects/{projectId}/variables", GetProjectVariables);
        app.MapPost("/api/projects/{projectId}/variables", CreateVariable);
        // ...
        return app;
    }

    private static async Task<IResult> GetProjectVariables(
        [FromRoute] Guid projectId,
        [FromServices] ISender sender,
        CancellationToken ct) {
        var result = await sender.Send(new GetProjectVariables(projectId), ct);
        return Results.Ok(result);
    }
}
```

### Request/Response Records (`Data/Records/`)
```csharp
public sealed record CreateVariableRequest(
    string Key,
    string Value,
    Guid? EnvironmentId);

public sealed record VariableResponse(
    Guid Id,
    string Key,
    string Value,
    Guid? EnvironmentId,
    DateTime CreatedAt,
    DateTime UpdatedAt);
```

### Verification Checklist
- [ ] All endpoints return correct status codes
- [ ] Validation errors return 400 with details
- [ ] Not found returns 404
- [ ] Swagger/OpenAPI documents all endpoints

---

## Phase 5: CQRS Handlers (Business Logic)

### Goal
Implement command/query handlers with business logic.

### Commands & Queries (`Features/`)

**Variables:**
- `GetProjectVariables` - Query all variables for a project (including from linked sets)
- `CreateVariable` - Create with validation
- `UpdateVariable` - Update with optimistic concurrency
- `DeleteVariable` - Soft or hard delete

**Variable Sets:**
- `GetVariableSets` - List all sets
- `GetVariableSet` - Get single set with variables
- `CreateVariableSet` - Create new set
- `UpdateVariableSet` - Update metadata
- `DeleteVariableSet` - Delete (check no projects linked?)
- `LinkVariableSetToProject` - Create link (validate project exists via gRPC)
- `UnlinkVariableSetFromProject` - Remove link

### Key Business Logic

**Scoping Resolution** (from issue):
```csharp
// When getting variables for a project + environment:
// 1. Get all project variables
// 2. Get all variables from linked variable sets
// 3. Filter by environment scope (environment-specific beats unscoped)
// 4. Resolve conflicts:
//    - Project Variable beats Variable Set (same key + scope)
//    - Multiple Variable Sets with same key + scope → ERROR
```

Implementation in `Features/Variables/GetResolvedVariables.cs`:
```csharp
public sealed record GetResolvedVariables(
    Guid ProjectId,
    Guid EnvironmentId) : IQuery<ResolvedVariablesResult>;

public sealed class GetResolvedVariablesHandler(
    ApplicationDbContext db,
    ILogger<GetResolvedVariablesHandler> logger)
    : IQueryHandler<GetResolvedVariables, ResolvedVariablesResult> {

    public async ValueTask<ResolvedVariablesResult> Handle(
        GetResolvedVariables query,
        CancellationToken ct) {
        // Implementation with conflict detection
    }
}
```

### Verification Checklist
- [ ] Project variable beats variable set variable (same key/scope)
- [ ] Multiple variable sets with same key/scope returns error with source info
- [ ] Environment-specific value beats unscoped value
- [ ] All handlers log appropriately

---

## Phase 6: gRPC Service (Inter-Service Communication)

### Goal
Implement gRPC service for snapper and validation calls.

### Proto Definition (`gRPC/Protos/variables.proto`)
```protobuf
syntax = "proto3";

package variables;

option csharp_namespace = "Variables.Communication";

service VariablesService {
  // Called by snapper to get resolved variables for snapshot
  rpc GetResolvedVariables(GetResolvedVariablesInput) returns (GetResolvedVariablesResult);

  // Validation endpoints (internal use)
  rpc ValidateProjectExists(ValidateProjectInput) returns (ValidationResult);
  rpc ValidateEnvironmentExists(ValidateEnvironmentInput) returns (ValidationResult);
}

message GetResolvedVariablesInput {
  string project_id = 1;
  string environment_id = 2;
}

message GetResolvedVariablesResult {
  message Success {
    repeated Variable variables = 1;
  }

  message ConflictError {
    string key = 1;
    repeated string conflicting_sources = 2;
  }

  oneof result {
    Success success = 1;
    ConflictError conflict_error = 2;
    InternalError internal_error = 3;
  }
}

message Variable {
  string key = 1;
  string value = 2;
  optional string environment_id = 3;
  string source = 4;  // "project" or variable set name
}

message InternalError {
  string message = 1;
  string trace = 2;
}

// ... validation messages
```

### gRPC Service Implementation
Copy pattern from `fasoauth/src/.../gRPC/Services/FasOAuthService.cs`:

```csharp
public sealed class VariablesGrpcService(
    ISender sender,
    ILogger<VariablesGrpcService> logger)
    : VariablesService.VariablesServiceBase {

    public override async Task<GetResolvedVariablesResult> GetResolvedVariables(
        GetResolvedVariablesInput request,
        ServerCallContext context) {

        logger.LogInformation("GetResolvedVariables called for project {ProjectId}",
            request.ProjectId);

        try {
            var result = await sender.Send(
                new GetResolvedVariables(
                    Guid.Parse(request.ProjectId),
                    Guid.Parse(request.EnvironmentId)),
                context.CancellationToken);

            return MapToProto(result);
        }
        catch (VariableConflictException ex) {
            logger.LogWarning("Variable conflict detected: {Key}", ex.Key);
            return new GetResolvedVariablesResult {
                ConflictError = new() {
                    Key = ex.Key,
                    ConflictingSources = { ex.Sources }
                }
            };
        }
    }
}
```

### gRPC Clients (for validation)
- `IProjectsClient` - Call projects service to validate project ID
- `ITargetsPlaneClient` - Call targets-plane to validate environment ID

### Verification Checklist
- [ ] gRPC reflection enabled in development
- [ ] Proto compiles without errors
- [ ] Service responds correctly to grpcurl calls
- [ ] Validation calls to external services work

---

## Phase 7: Validation Layer

### Goal
Implement synchronous validation of project and environment IDs.

### Validation Services

**IProjectValidator** (`Abstractions/Services/IProjectValidator.cs`)
```csharp
public interface IProjectValidator {
    Task<bool> ProjectExistsAsync(Guid projectId, CancellationToken ct);
}
```

**IEnvironmentValidator** (`Abstractions/Services/IEnvironmentValidator.cs`)
```csharp
public interface IEnvironmentValidator {
    Task<bool> EnvironmentExistsAsync(Guid environmentId, CancellationToken ct);
}
```

### Implementation
```csharp
// Services/GrpcProjectValidator.cs
public sealed class GrpcProjectValidator(ProjectsService.ProjectsServiceClient client)
    : IProjectValidator {

    public async Task<bool> ProjectExistsAsync(Guid projectId, CancellationToken ct) {
        try {
            var result = await client.ValidateProjectAsync(
                new() { ProjectId = projectId.ToString() },
                cancellationToken: ct);
            return result.Exists;
        }
        catch (RpcException) {
            return false;
        }
    }
}
```

### FluentValidation Validators
```csharp
public sealed class CreateVariableRequestValidator
    : AbstractValidator<CreateVariableRequest> {

    public CreateVariableRequestValidator(IEnvironmentValidator envValidator) {
        RuleFor(x => x.Key)
            .NotEmpty()
            .MaximumLength(256);

        RuleFor(x => x.Value)
            .NotEmpty();

        RuleFor(x => x.EnvironmentId)
            .MustAsync(async (id, ct) =>
                !id.HasValue || await envValidator.EnvironmentExistsAsync(id.Value, ct))
            .WithMessage("Environment does not exist");
    }
}
```

### Verification Checklist
- [ ] Creating variable with invalid environment ID fails
- [ ] Linking variable set to invalid project ID fails
- [ ] Validation errors include descriptive messages
- [ ] gRPC client handles connection failures gracefully

---

## Phase 8: Docker & Deployment

### Goal
Create Docker configuration for deployment.

### Tasks

1. **Dockerfile** (adapt from `fasoauth/src/.../Dockerfile`)
   - Multi-stage build with .NET 10 SDK/runtime
   - Proper labels
   - Health check

2. **docker-compose.yml**
   ```yaml
   services:
     db:
       image: postgres:16
       environment:
         POSTGRES_DB: variables
         POSTGRES_USER: variables
         POSTGRES_PASSWORD: variables
       ports:
         - "5432:5432"
       healthcheck:
         test: pg_isready -U variables
       profiles:
         - db
         - all

     api:
       build:
         context: .
         dockerfile: src/Onlyspans.Variables.Api/Dockerfile
       environment:
         ConnectionStrings__Default: "Server=db;Port=5432;Database=variables;User Id=variables;Password=variables"
       ports:
         - "8080:8080"
       depends_on:
         db:
           condition: service_healthy
       profiles:
         - api
         - all
   ```

3. **init.sql** for database initialization (optional, migrations handle schema)

### Verification Checklist
- [ ] `docker compose --profile all up` starts all services
- [ ] API connects to PostgreSQL
- [ ] Migrations run automatically
- [ ] Health checks pass

---

## Phase 9: Testing & Verification

### Goal
Ensure all functionality works as specified.

### Test Scenarios

1. **Variable CRUD**
   - Create variable with/without environment scope
   - Update variable value
   - Delete variable

2. **Variable Set CRUD**
   - Create/update/delete variable sets
   - Add/remove variables from sets

3. **Project-Set Linking**
   - Link set to project
   - Verify validation of project ID
   - Unlink set

4. **Scoping & Conflict Resolution**
   - Variable with environment scope beats unscoped
   - Project variable beats variable set variable
   - Multiple sets with same key → error

5. **gRPC Service**
   - GetResolvedVariables returns correct data
   - Conflict scenarios return proper errors

### Verification Commands
```bash
# Build
dotnet build

# Run tests
dotnet test

# Start locally
docker compose up -d db
dotnet run --project src/Variables.Api

# Test endpoints
curl http://localhost:8080/health
curl http://localhost:8080/api/variable-sets

# Test gRPC
grpcurl -plaintext localhost:8080 list
grpcurl -plaintext -d '{"project_id":"...","environment_id":"..."}' \
  localhost:8080 variables.VariablesService/GetResolvedVariables
```

---

## Summary

| Phase | Description | Dependencies |
|-------|-------------|--------------|
| 0 | Documentation Discovery | None (COMPLETED) |
| 1 | Project Scaffolding | None |
| 2 | Domain Models & Database | Phase 1 |
| 3 | Configuration & Infrastructure | Phase 1 |
| 4 | REST API | Phases 2, 3 |
| 5 | CQRS Handlers | Phase 2 |
| 6 | gRPC Service | Phases 2, 5 |
| 7 | Validation Layer | Phases 4, 5 |
| 8 | Docker & Deployment | All above |
| 9 | Testing & Verification | All above |

**Solution Structure:**
```
Onlyspans.Variables.slnx
├── src/
│   └── Onlyspans.Variables.Api/
│       ├── Onlyspans.Variables.Api.csproj (net10.0)
│       ├── Program.cs
│       ├── Startup.cs, Startup.*.cs
│       ├── Data/ (Entities, Contexts, Options, Records)
│       ├── Endpoints/
│       ├── Features/
│       ├── gRPC/ (Protos, Services)
│       ├── Services/
│       └── Dockerfile
├── docker-compose.yml
└── init.sql
```

**Reference Patterns from fasoauth (structure only, NOT packages):**
- Startup pattern: `fasoauth/src/.../Startup*.cs`
- Entity pattern: `fasoauth/src/.../Data/Entities/*.cs` (without GraphQL, without Timestamp)
- DbContext: `fasoauth/src/.../Data/Contexts/ApplicationDbContext.cs`
- gRPC service: `fasoauth/src/.../gRPC/Services/FasOAuthService.cs`
- Proto structure: `fasoauth/src/.../gRPC/Protos/fasoauth.proto`
- Dockerfile: `fasoauth/src/.../Dockerfile`
- docker-compose: `fasoauth/docker-compose.yml`

**Key Differences from Reference:**
- .slnx format (not .sln)
- .NET 10 (not .NET 9)
- No GraphQL
- No OpenTelemetry
- No optimistic concurrency ([Timestamp])
- Strongly.Options (not P4m.* packages)
- Implicit M:M for Project↔VariableSet (no explicit join entity)
