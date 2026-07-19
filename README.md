# AutoKnots

AutoKnots is an ASP.NET Core 8 application for vehicle inventory, investment and cost tracking, profit allocation, and public marketplace listings.

## Prerequisites

- .NET 8 SDK
- SQL Server or SQL Server LocalDB
- EF Core CLI (`dotnet-ef`)

## Local setup

Configure development-only values with user secrets or environment variables:

```powershell
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=(localdb)\mssqllocaldb;Database=AutoKnots;Trusted_Connection=True" --project AutoKnots/AutoKnots/AutoKnots.csproj
dotnet user-secrets set "Jwt:Key" "a-development-key-containing-at-least-32-bytes" --project AutoKnots/AutoKnots/AutoKnots.csproj
```

Apply migrations and run the application:

```powershell
dotnet restore AutoKnots/AutoKnots.sln
dotnet ef database update --project AutoKnots/AutoKnots/AutoKnots.csproj --startup-project AutoKnots/AutoKnots/AutoKnots.csproj
dotnet run --project AutoKnots/AutoKnots/AutoKnots.csproj
```

Swagger is available at `/swagger` in Development. Marketplace pages are available at `/Marketplace`.

## Tests

```powershell
dotnet test AutoKnots/AutoKnots.sln
```

## Documentation

- [Architecture](docs/architecture.md)
- [Database](docs/database.md)
- [Marketplace API](docs/api.md)

## Current marketplace scope

The first marketplace increment supports creating listings from owned inventory, draft/active/paused/sold/removed lifecycle states, photos by URL, public browsing, search, filters, sorting, and seller listing management.

Offers, favourites, messaging, purchase orchestration, inspections, notifications, administration, and analytics are planned as separate normalized increments.
