# Marketplace API

Base route: `/api/marketplace/listings`

## Public endpoints

- `GET /api/marketplace/listings`: paginated active listings.
- `GET /api/marketplace/listings/{id}`: active listing details.

Supported query parameters include `search`, `make`, `model`, `minYear`, `maxYear`, `minPrice`, `maxPrice`, `maxMileage`, `fuelType`, `transmission`, `bodyStyle`, `condition`, `location`, `sort`, `page`, and `pageSize`.

Sort values: `newest`, `oldest`, `price_asc`, `price_desc`, `mileage_asc`, `mileage_desc`, `year_asc`, and `year_desc`. Page size is capped at 100.

## Authenticated seller endpoints

- `GET /api/marketplace/listings/mine`: current seller's listings in all states.
- `POST /api/marketplace/listings`: create a listing for owned inventory.
- `PUT /api/marketplace/listings/{id}`: edit an owned listing.
- `PATCH /api/marketplace/listings/{id}/status`: activate, pause, or mark sold.
- `DELETE /api/marketplace/listings/{id}`: soft-remove an owned listing.

Supply the existing JWT bearer token in the `Authorization` header. Cookie authentication is also supported by the application's smart authentication scheme.

Create/update bodies include the existing `inventoryItemId`, listing title and description, price, year, mileage, specification enums, location, a `publish` flag, and up to 20 image URLs.

Listings cannot be moved between inventory vehicles. Sold/removed listings cannot be edited or reactivated. Ownership is resolved from the existing inventory creator relationship.
