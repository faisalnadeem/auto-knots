# Architecture

## Application structure

- `Controllers`: MVC pages and REST API adapters.
- `Services`: application workflows and authorization boundaries.
- `Models`: persistence entities, request models, and read projections.
- `Data`: EF Core context and migrations.
- `Views`: Razor UI using the existing bundled Bootstrap theme.
- `wwwroot`: static assets and legacy theme pages.
- `AutoKnots.Tests`: automated service and workflow tests.

`Program.cs` is the composition root. The application uses ASP.NET Identity with cookie and JWT authentication, EF Core with SQL Server, MVC/Razor views, and Swagger for development API discovery.

## Marketplace boundary

`VehicleListing` references the existing `InventoryItem`; it does not duplicate make, model, seller ownership, engine, chassis, costs, or investments. Listing-specific price, marketing copy, public specifications, location, state, and images remain in the marketplace model.

Both `MarketplaceController` and `MarketplaceApiController` call `IMarketplaceService`. This keeps ownership, visibility, filtering, and lifecycle rules identical across UI and API.

Public queries expose only active listings. Seller queries are scoped through `InventoryItem.CreatedByUserId`. Listings are soft-removed so later offers, purchases, inspections, and audit records can retain referential integrity.

## Planned increments

1. Favourites, recently viewed listings, seller contact, offers, and counter-offers.
2. Reservation and provider-neutral purchase orchestration.
3. Inspection requests, scheduling, assignments, checklists, defects, reports, certificates, and vehicle timeline.
4. Durable notifications and user dashboards.
5. Administrator roles, moderation, inspector management, audit logs, and analytics.

Each increment should add tests and a migration only when its normalized persistence model requires one.
