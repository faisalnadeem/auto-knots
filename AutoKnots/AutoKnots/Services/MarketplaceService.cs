using AutoKnots.Data;
using AutoKnots.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using System.Text.RegularExpressions;

namespace AutoKnots.Services;

public class MarketplaceService : IMarketplaceService
{
    private const int MaximumImages = 20;
    private readonly ApplicationDbContext _db;

    public MarketplaceService(ApplicationDbContext db) => _db = db;

    public Task<PublicMarketplacePage> SearchAsync(MarketplaceSearch search, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        return SearchPublicAsync(_db.VehicleListings.Where(x => x.Status == ListingStatus.Active &&
            (x.ExpiresAt == null || x.ExpiresAt > now)), search, cancellationToken);
    }

    public Task<MarketplacePage> GetSellerListingsAsync(string sellerUserId, MarketplaceSearch search, CancellationToken cancellationToken = default) =>
        SearchCoreAsync(_db.VehicleListings.Where(x => x.InventoryItem.CreatedByUserId == sellerUserId), search, cancellationToken);

    public async Task<PublicListingDetails?> GetPublicDetailsAsync(string slug, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var listing = await _db.VehicleListings.AsNoTracking()
            .Include(x => x.InventoryItem)
            .Include(x => x.Images)
            .Where(x => x.Slug == slug && x.Status == ListingStatus.Active &&
                        (x.ExpiresAt == null || x.ExpiresAt > now))
            .FirstOrDefaultAsync(cancellationToken);
        if (listing == null) return null;

        // UserName is currently the account email, so it must never be exposed
        // through the anonymous marketplace contract.
        string? sellerName = listing.ShowSellerName ? "Verified seller" : null;

        return new PublicListingDetails
        {
            Slug = listing.Slug,
            Title = listing.Title,
            Description = listing.Description,
            Make = listing.InventoryItem.Make,
            Model = listing.InventoryItem.Model,
            Variant = listing.InventoryItem.Variant,
            Price = listing.Price,
            Year = listing.Year,
            Mileage = listing.Mileage,
            FuelType = listing.FuelType,
            Transmission = listing.Transmission,
            BodyStyle = listing.BodyStyle,
            Condition = listing.Condition,
            Location = listing.Location,
            PublishedAt = listing.PublishedAt ?? listing.CreatedAt,
            PrimaryImageUrl = listing.Images.OrderBy(image => image.SortOrder).Select(image => image.Url).FirstOrDefault(),
            ImageUrls = listing.Images.OrderBy(image => image.SortOrder).Select(image => image.Url).ToList(),
            InspectionStatus = listing.InspectionStatus,
            InspectionSummary = listing.InspectionSummary,
            VehicleHistory = listing.VehicleHistory,
            Features = (listing.Features ?? string.Empty).Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
            SellerName = sellerName,
            IsFeatured = listing.IsFeatured
        };
    }

    public async Task<IReadOnlyList<string>> GetPopularMakesAsync(int count, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        return await _db.VehicleListings.AsNoTracking()
            .Where(x => x.Status == ListingStatus.Active && (x.ExpiresAt == null || x.ExpiresAt > now))
            .GroupBy(x => x.InventoryItem.Make)
            .OrderByDescending(group => group.Count())
            .ThenBy(group => group.Key)
            .Select(group => group.Key)
            .Take(Math.Clamp(count, 1, 20))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PublicSitemapListing>> GetSitemapListingsAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        return await _db.VehicleListings.AsNoTracking()
            .Where(x => x.Status == ListingStatus.Active && (x.ExpiresAt == null || x.ExpiresAt > now))
            .OrderByDescending(x => x.UpdatedAt)
            .Take(50000)
            .Select(x => new PublicSitemapListing { Slug = x.Slug, UpdatedAt = x.UpdatedAt })
            .ToListAsync(cancellationToken);
    }

    public Task<MarketplaceListingDetails?> GetOwnerDetailsAsync(int id, string sellerUserId, CancellationToken cancellationToken = default) =>
        GetDetailsAsync(id, query => query.Where(x => x.InventoryItem.CreatedByUserId == sellerUserId), cancellationToken);

    public async Task<MarketplaceResult<MarketplaceListingDetails>> CreateAsync(
        string sellerUserId,
        MarketplaceListingInput input,
        CancellationToken cancellationToken = default)
    {
        var error = ValidateInput(input);
        if (error != null) return Failure(error);

        var inventory = await _db.InventoryItems
            .Include(x => x.Listing)
            .FirstOrDefaultAsync(x => x.Id == input.InventoryItemId && x.CreatedByUserId == sellerUserId, cancellationToken);

        if (inventory == null) return Failure("Inventory item not found.");
        if (inventory.Status == InventoryStatus.Sold) return Failure("A sold inventory item cannot be listed.");
        if (inventory.Listing != null) return Failure("This vehicle already has a marketplace listing.");

        var now = DateTime.UtcNow;
        var listing = new VehicleListing
        {
            InventoryItemId = inventory.Id,
            Slug = await CreateUniqueSlugAsync(input.Title, input.Year, cancellationToken),
            CreatedAt = now,
            UpdatedAt = now
        };
        ApplyInput(listing, input, now);
        _db.VehicleListings.Add(listing);
        await _db.SaveChangesAsync(cancellationToken);

        return Success((await GetOwnerDetailsAsync(listing.Id, sellerUserId, cancellationToken))!);
    }

    public async Task<MarketplaceResult<MarketplaceListingDetails>> UpdateAsync(
        int id,
        string sellerUserId,
        MarketplaceListingInput input,
        CancellationToken cancellationToken = default)
    {
        var error = ValidateInput(input);
        if (error != null) return Failure(error);

        var listing = await _db.VehicleListings
            .Include(x => x.InventoryItem)
            .Include(x => x.Images)
            .FirstOrDefaultAsync(x => x.Id == id && x.InventoryItem.CreatedByUserId == sellerUserId, cancellationToken);

        if (listing == null) return Failure("Listing not found.");
        if (listing.InventoryItemId != input.InventoryItemId) return Failure("A listing cannot be moved to another vehicle.");
        if (listing.Status is ListingStatus.Sold or ListingStatus.Removed) return Failure("A sold or removed listing cannot be edited.");

        ApplyInput(listing, input, DateTime.UtcNow);
        await _db.SaveChangesAsync(cancellationToken);
        return Success((await GetOwnerDetailsAsync(id, sellerUserId, cancellationToken))!);
    }

    public async Task<MarketplaceResult<MarketplaceListingDetails>> SetStatusAsync(
        int id,
        string sellerUserId,
        ListingStatus status,
        CancellationToken cancellationToken = default)
    {
        var listing = await _db.VehicleListings
            .Include(x => x.InventoryItem)
            .FirstOrDefaultAsync(x => x.Id == id && x.InventoryItem.CreatedByUserId == sellerUserId, cancellationToken);

        if (listing == null) return Failure("Listing not found.");
        if (!CanTransition(listing.Status, status)) return Failure($"Cannot move a listing from {listing.Status} to {status}.");

        var now = DateTime.UtcNow;
        listing.Status = status;
        listing.UpdatedAt = now;
        if (status == ListingStatus.Active && listing.PublishedAt == null) listing.PublishedAt = now;
        if (status == ListingStatus.Sold) listing.SoldAt = now;
        await _db.SaveChangesAsync(cancellationToken);

        return Success((await GetOwnerDetailsAsync(id, sellerUserId, cancellationToken))!);
    }

    private async Task<PublicMarketplacePage> SearchPublicAsync(
        IQueryable<VehicleListing> query,
        MarketplaceSearch search,
        CancellationToken cancellationToken)
    {
        var page = Math.Max(1, search.Page);
        var pageSize = Math.Clamp(search.PageSize, 1, 100);
        query = ApplyFilters(query.AsNoTracking(), search);
        if (search.FeaturedOnly) query = query.Where(x => x.IsFeatured);
        query = ApplySort(query, search.Sort);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query.Select(PublicSummaryProjection)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
        return new PublicMarketplacePage { Items = items, TotalCount = totalCount, Page = page, PageSize = pageSize };
    }

    private async Task<MarketplacePage> SearchCoreAsync(
        IQueryable<VehicleListing> query,
        MarketplaceSearch search,
        CancellationToken cancellationToken)
    {
        var page = Math.Max(1, search.Page);
        var pageSize = Math.Clamp(search.PageSize, 1, 100);
        query = ApplyFilters(query.AsNoTracking(), search);
        query = ApplySort(query, search.Sort);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query.Select(SummaryProjection)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new MarketplacePage { Items = items, TotalCount = totalCount, Page = page, PageSize = pageSize };
    }

    private static IQueryable<VehicleListing> ApplyFilters(IQueryable<VehicleListing> query, MarketplaceSearch search)
    {
        if (!string.IsNullOrWhiteSpace(search.Search))
        {
            var term = search.Search.Trim();
            query = query.Where(x => x.Title.Contains(term) || x.Description.Contains(term) ||
                                     x.InventoryItem.Make.Contains(term) || x.InventoryItem.Model.Contains(term));
        }
        if (!string.IsNullOrWhiteSpace(search.Make)) query = query.Where(x => x.InventoryItem.Make == search.Make.Trim());
        if (!string.IsNullOrWhiteSpace(search.Model)) query = query.Where(x => x.InventoryItem.Model == search.Model.Trim());
        if (search.MinYear.HasValue) query = query.Where(x => x.Year >= search.MinYear);
        if (search.MaxYear.HasValue) query = query.Where(x => x.Year <= search.MaxYear);
        if (search.MinPrice.HasValue) query = query.Where(x => x.Price >= search.MinPrice);
        if (search.MaxPrice.HasValue) query = query.Where(x => x.Price <= search.MaxPrice);
        if (search.MaxMileage.HasValue) query = query.Where(x => x.Mileage <= search.MaxMileage);
        if (search.FuelType.HasValue) query = query.Where(x => x.FuelType == search.FuelType);
        if (search.Transmission.HasValue) query = query.Where(x => x.Transmission == search.Transmission);
        if (search.BodyStyle.HasValue) query = query.Where(x => x.BodyStyle == search.BodyStyle);
        if (search.Condition.HasValue) query = query.Where(x => x.Condition == search.Condition);
        if (!string.IsNullOrWhiteSpace(search.Location)) query = query.Where(x => x.Location.Contains(search.Location.Trim()));

        return query;
    }

    private static IQueryable<VehicleListing> ApplySort(IQueryable<VehicleListing> query, string sort)
    {
        return sort.ToLowerInvariant() switch
        {
            "price_asc" => query.OrderBy(x => x.Price),
            "price_desc" => query.OrderByDescending(x => x.Price),
            "mileage_asc" => query.OrderBy(x => x.Mileage),
            "mileage_desc" => query.OrderByDescending(x => x.Mileage),
            "year_asc" => query.OrderBy(x => x.Year),
            "year_desc" => query.OrderByDescending(x => x.Year),
            "oldest" => query.OrderBy(x => x.PublishedAt),
            _ => query.OrderByDescending(x => x.PublishedAt)
        };
    }

    private async Task<MarketplaceListingDetails?> GetDetailsAsync(
        int id,
        Func<IQueryable<VehicleListing>, IQueryable<VehicleListing>> scope,
        CancellationToken cancellationToken)
    {
        var query = scope(_db.VehicleListings.AsNoTracking().Where(x => x.Id == id));
        return await query.Select(x => new MarketplaceListingDetails
        {
            Id = x.Id,
            InventoryItemId = x.InventoryItemId,
            Title = x.Title,
            Description = x.Description,
            Make = x.InventoryItem.Make,
            Model = x.InventoryItem.Model,
            Variant = x.InventoryItem.Variant,
            Price = x.Price,
            Year = x.Year,
            Mileage = x.Mileage,
            FuelType = x.FuelType,
            Transmission = x.Transmission,
            BodyStyle = x.BodyStyle,
            Condition = x.Condition,
            Location = x.Location,
            Status = x.Status,
            CreatedAt = x.CreatedAt,
            UpdatedAt = x.UpdatedAt,
            PublishedAt = x.PublishedAt,
            SoldAt = x.SoldAt,
            ExpiresAt = x.ExpiresAt,
            Slug = x.Slug,
            Features = x.Features,
            VehicleHistory = x.VehicleHistory,
            InspectionStatus = x.InspectionStatus,
            InspectionSummary = x.InspectionSummary,
            ShowSellerName = x.ShowSellerName,
            SellerName = _db.Users.Where(u => u.Id == x.InventoryItem.CreatedByUserId)
                .Select(u => u.UserName ?? u.Email ?? "Seller").FirstOrDefault() ?? "Seller",
            PrimaryImageUrl = x.Images.OrderBy(i => i.SortOrder).Select(i => i.Url).FirstOrDefault(),
            ImageUrls = x.Images.OrderBy(i => i.SortOrder).Select(i => i.Url).ToList()
        }).FirstOrDefaultAsync(cancellationToken);
    }

    private static readonly Expression<Func<VehicleListing, MarketplaceListingSummary>> SummaryProjection = x => new()
    {
        Id = x.Id,
        InventoryItemId = x.InventoryItemId,
        Title = x.Title,
        Make = x.InventoryItem.Make,
        Model = x.InventoryItem.Model,
        Price = x.Price,
        Year = x.Year,
        Mileage = x.Mileage,
        FuelType = x.FuelType,
        Transmission = x.Transmission,
        BodyStyle = x.BodyStyle,
        Condition = x.Condition,
        Location = x.Location,
        Status = x.Status,
        PublishedAt = x.PublishedAt,
        PrimaryImageUrl = x.Images.OrderBy(i => i.SortOrder).Select(i => i.Url).FirstOrDefault()
    };

    private static readonly Expression<Func<VehicleListing, PublicListingSummary>> PublicSummaryProjection = x => new()
    {
        Slug = x.Slug,
        Title = x.Title,
        Make = x.InventoryItem.Make,
        Model = x.InventoryItem.Model,
        Price = x.Price,
        Year = x.Year,
        Mileage = x.Mileage,
        FuelType = x.FuelType,
        Transmission = x.Transmission,
        BodyStyle = x.BodyStyle,
        Condition = x.Condition,
        Location = x.Location,
        PublishedAt = x.PublishedAt ?? x.CreatedAt,
        PrimaryImageUrl = x.Images.OrderBy(i => i.SortOrder).Select(i => i.Url).FirstOrDefault(),
        InspectionStatus = x.InspectionStatus,
        IsFeatured = x.IsFeatured
    };

    private static void ApplyInput(VehicleListing listing, MarketplaceListingInput input, DateTime now)
    {
        listing.Title = input.Title.Trim();
        listing.Description = input.Description.Trim();
        listing.Price = input.Price;
        listing.Year = input.Year;
        listing.Mileage = input.Mileage;
        listing.FuelType = input.FuelType;
        listing.Transmission = input.Transmission;
        listing.BodyStyle = input.BodyStyle;
        listing.Condition = input.Condition;
        listing.Location = input.Location.Trim();
        listing.Features = string.IsNullOrWhiteSpace(input.Features) ? null : input.Features.Trim();
        listing.VehicleHistory = string.IsNullOrWhiteSpace(input.VehicleHistory) ? null : input.VehicleHistory.Trim();
        listing.InspectionStatus = input.InspectionStatus;
        listing.InspectionSummary = string.IsNullOrWhiteSpace(input.InspectionSummary) ? null : input.InspectionSummary.Trim();
        listing.ShowSellerName = input.ShowSellerName;
        listing.ExpiresAt = input.ExpiresAt;
        listing.UpdatedAt = now;

        if (input.Publish && listing.Status is ListingStatus.Draft or ListingStatus.Paused)
        {
            listing.Status = ListingStatus.Active;
            listing.PublishedAt ??= now;
        }
        else if (!input.Publish && listing.Status == ListingStatus.Active)
        {
            listing.Status = ListingStatus.Paused;
        }

        listing.Images.Clear();
        var urls = input.ImageUrls.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).Distinct().ToList();
        for (var index = 0; index < urls.Count; index++)
            listing.Images.Add(new ListingImage { Url = urls[index], SortOrder = index, AltText = listing.Title });
    }

    private static string? ValidateInput(MarketplaceListingInput input)
    {
        if (input.ImageUrls.Count > MaximumImages) return $"A listing can contain at most {MaximumImages} images.";
        foreach (var url in input.ImageUrls.Where(x => !string.IsNullOrWhiteSpace(x)))
        {
            if (url.StartsWith('/')) continue;
            if (!Uri.TryCreate(url, UriKind.Absolute, out var parsed) || parsed.Scheme is not ("http" or "https"))
                return "Image URLs must be HTTP(S) or application-relative URLs.";
        }
        return null;
    }

    private static bool CanTransition(ListingStatus from, ListingStatus to) => (from, to) switch
    {
        (ListingStatus.Draft, ListingStatus.Active or ListingStatus.Removed) => true,
        (ListingStatus.Active, ListingStatus.Paused or ListingStatus.Sold or ListingStatus.Removed) => true,
        (ListingStatus.Paused, ListingStatus.Active or ListingStatus.Sold or ListingStatus.Removed) => true,
        _ => false
    };

    private async Task<string> CreateUniqueSlugAsync(string title, int year, CancellationToken cancellationToken)
    {
        var baseSlug = Regex.Replace($"{title}-{year}".ToLowerInvariant(), "[^a-z0-9]+", "-").Trim('-');
        if (string.IsNullOrEmpty(baseSlug)) baseSlug = "vehicle";
        if (baseSlug.Length > 180) baseSlug = baseSlug[..180].TrimEnd('-');

        var slug = baseSlug;
        for (var suffix = 2; await _db.VehicleListings.AnyAsync(x => x.Slug == slug, cancellationToken); suffix++)
            slug = $"{baseSlug}-{suffix}";
        return slug;
    }

    private static MarketplaceResult<MarketplaceListingDetails> Success(MarketplaceListingDetails value) => new() { Success = true, Value = value };
    private static MarketplaceResult<MarketplaceListingDetails> Failure(string error) => new() { Error = error };
}
