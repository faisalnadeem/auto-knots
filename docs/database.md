# Database

## Marketplace tables

### `VehicleListings`

One-to-zero/one with `InventoryItems`, enforced by a unique `InventoryItemId` index. The inventory record remains the source of truth for vehicle identity and seller ownership.

Listing lifecycle values are:

- `Draft`: visible only to its owner.
- `Active`: visible in public search and details.
- `Paused`: retained but hidden from public results.
- `Sold`: terminal marketplace state.
- `Removed`: terminal soft-removal state.

Indexed public sort paths include status with publication date, price, year, and mileage.

### `ListingImages`

Many-to-one with `VehicleListings`. Images have stable ordering and cascade when their listing is physically deleted. The current implementation stores validated HTTP(S) or application-relative URLs so object storage can be introduced without coupling listings to a provider.

## Referential behavior

Deleting an inventory item cascades to its listing and images. Marketplace removal is normally a state transition rather than a database delete. Future offers, purchases, inspections, and audit records should use restrictive deletion behavior where historical retention is required.

## Migrations

The marketplace foundation is introduced by `AddVehicleMarketplaceListings`. Apply migrations as a controlled deployment step; the web process does not automatically mutate production schema at startup.
