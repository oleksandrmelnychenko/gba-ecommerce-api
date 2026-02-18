# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build and Run Commands

```bash
# Build the entire solution
dotnet build Global.Business.Assistant.sln

# Build a specific project
dotnet build src/GBA.Core/GBA.Core.csproj

# Run the main API server (GBA.Core)
dotnet run --project src/GBA.Core/GBA.Core.csproj
# Runs on http://localhost:35981

# Run the ecommerce API server
dotnet run --project src/GBA.Ecommerce/GBA.Ecommerce.csproj
# Runs on http://localhost:15050

# Run the analytics API server
dotnet run --project src/GBA.Analytics/GBA.Analytics.csproj

# Add EF Core migration (from solution root)
dotnet ef migrations add <MigrationName> --project src/GBA.Data --startup-project src/GBA.Core --context ConcordContext

# Update database
dotnet ef database update --project src/GBA.Data --startup-project src/GBA.Core --context ConcordContext
```

## Architecture Overview

This is an ERP/CRM system built on .NET 10 with an **Akka.NET actor model** for business logic orchestration.

### Project Structure

- **GBA.Core** - Main Web API host. Contains controllers, middleware, and Startup configuration. Runs on port 35981.
- **GBA.Ecommerce** - E-commerce API host. Separate web application for e-commerce functionality. Runs on port 15050.
- **GBA.Analytics** - Analytics API host.
- **GBA.Services** - Business logic layer using **Akka.NET actors**. All business operations are processed through actor messages.
- **GBA.Domain** - Domain entities, repositories (with Factory pattern), and message definitions for actor communication.
- **GBA.Data** - Entity Framework Core DbContexts and migrations. Contains `ConcordContext`, `ConcordIdentityContext`, and `ConcordDataAnalyticContext`.
- **GBA.Common** - Shared utilities, helpers, and configuration.

### Actor Model Pattern

The application uses Akka.NET with Autofac DI. Controllers do not call repositories directly. Instead:

1. Controllers send **messages** to actors via `ActorReferenceManager`
2. Actors process messages and interact with repositories
3. Responses are returned asynchronously via `Ask<T>` pattern

Example flow:
```csharp
// In Controller:
var result = await ActorReferenceManager.Instance.Get(ProductsActorNames.PRODUCTS_ACTOR)
    .Ask<Tuple<Product, string>>(new AddProductMessage(product, userNetId));
```

Key actor organization:
- **MasterActor** (`GBA.Services/Actors/MasterActor.cs`) - Root actor that initializes all child actors
- **Management actors** (e.g., `SalesManagementActor`, `SupplyManagementActor`) - Coordinate domain operations
- **Get actors** (e.g., `BaseSalesGetActor`, `BaseProductsGetActor`) - Handle read operations with RoundRobinPool routing

### Database Contexts

- **ConcordContext** - Main business data
- **ConcordIdentityContext** - ASP.NET Identity for authentication
- **ConcordDataAnalyticContext** - Analytics data

Connection strings are environment-dependent:
- `#if DEBUG` uses `LocalConnectionString`
- `#else` uses `RemoteConnectionString`

### SignalR Hubs

Real-time communication hubs in `GBA.Services/Hubs/`:
- `/hubs/products/reservation` - Product reservation updates
- `/hubs/supplies/tasks` - Supply task notifications
- `/hubs/supplies/orders` - Supply order updates
- `/hubs/exchangerates` - Exchange rate updates
- `/hubs/data/sync` - Data synchronization status
- `/hubs/resale` - Resale notifications

### Localization

Supports Ukrainian (uk) and Polish (pl) locales. Resource files are in `Resources/` folders within each project. Default locale is Ukrainian.

### Key Patterns

- **Repository Factory Pattern** - All repositories are created via `I*RepositoryFactory` interfaces
- **Response Builder** - `IResponseFactory` creates standardized API responses
- **Message-based Communication** - Domain messages in `GBA.Domain/Messages/` define actor communication contracts
- **Table Maps** - EF Core entity configurations in `GBA.Data/TableMaps/`
