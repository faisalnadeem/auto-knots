using System.ComponentModel.DataAnnotations;

namespace AutoKnots.Models;

public class MarketplaceSearch : IValidatableObject
{
    [MaxLength(100)]
    public string? Search { get; set; }
    [MaxLength(100)]
    public string? Make { get; set; }
    [MaxLength(100)]
    public string? Model { get; set; }
    [Range(1886, 2100)]
    public int? MinYear { get; set; }
    [Range(1886, 2100)]
    public int? MaxYear { get; set; }
    [Range(0, double.MaxValue)]
    public decimal? MinPrice { get; set; }
    [Range(0, double.MaxValue)]
    public decimal? MaxPrice { get; set; }
    [Range(0, int.MaxValue)]
    public int? MaxMileage { get; set; }
    public FuelType? FuelType { get; set; }
    public TransmissionType? Transmission { get; set; }
    public BodyStyle? BodyStyle { get; set; }
    public VehicleCondition? Condition { get; set; }
    [MaxLength(160)]
    public string? Location { get; set; }
    [MaxLength(20)]
    public string Sort { get; set; } = "newest";
    [Range(1, int.MaxValue)]
    public int Page { get; set; } = 1;
    [Range(1, 100)]
    public int PageSize { get; set; } = 12;
    public bool FeaturedOnly { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (MinYear > MaxYear) yield return new ValidationResult("Minimum year cannot exceed maximum year.", new[] { nameof(MinYear), nameof(MaxYear) });
        if (MinPrice > MaxPrice) yield return new ValidationResult("Minimum price cannot exceed maximum price.", new[] { nameof(MinPrice), nameof(MaxPrice) });
        var supportedSorts = new[] { "newest", "oldest", "price_asc", "price_desc", "mileage_asc", "mileage_desc", "year_asc", "year_desc" };
        if (!supportedSorts.Contains(Sort, StringComparer.OrdinalIgnoreCase))
            yield return new ValidationResult("Unsupported marketplace sort value.", new[] { nameof(Sort) });
    }
}

public class MarketplaceListingInput
{
    [Required]
    public int InventoryItemId { get; set; }

    [Required, MaxLength(160)]
    public string Title { get; set; } = string.Empty;

    [Required, MaxLength(4000)]
    public string Description { get; set; } = string.Empty;

    [Range(0.01, double.MaxValue)]
    public decimal Price { get; set; }

    [Range(1886, 2100)]
    public int Year { get; set; }

    [Range(0, int.MaxValue)]
    public int Mileage { get; set; }

    public FuelType FuelType { get; set; }
    public TransmissionType Transmission { get; set; }
    public BodyStyle BodyStyle { get; set; }
    public VehicleCondition Condition { get; set; }

    [Required, MaxLength(160)]
    public string Location { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string? Features { get; set; }

    [MaxLength(4000)]
    public string? VehicleHistory { get; set; }

    public InspectionStatus InspectionStatus { get; set; }

    [MaxLength(2000)]
    public string? InspectionSummary { get; set; }

    public bool ShowSellerName { get; set; } = true;
    public DateTime? ExpiresAt { get; set; }

    public bool Publish { get; set; }
    public List<string> ImageUrls { get; set; } = new();
}

public class MarketplaceListingSummary
{
    public int Id { get; set; }
    public int InventoryItemId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Make { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Year { get; set; }
    public int Mileage { get; set; }
    public FuelType FuelType { get; set; }
    public TransmissionType Transmission { get; set; }
    public BodyStyle BodyStyle { get; set; }
    public VehicleCondition Condition { get; set; }
    public string Location { get; set; } = string.Empty;
    public ListingStatus Status { get; set; }
    public DateTime? PublishedAt { get; set; }
    public string? PrimaryImageUrl { get; set; }
}

public class MarketplaceListingDetails : MarketplaceListingSummary
{
    public string Slug { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? Variant { get; set; }
    public string SellerName { get; set; } = string.Empty;
    public IReadOnlyList<string> ImageUrls { get; set; } = Array.Empty<string>();
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? SoldAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public string? Features { get; set; }
    public string? VehicleHistory { get; set; }
    public InspectionStatus InspectionStatus { get; set; }
    public string? InspectionSummary { get; set; }
    public bool ShowSellerName { get; set; }
}

public class PublicListingSummary
{
    public string Slug { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Make { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Year { get; set; }
    public int Mileage { get; set; }
    public FuelType FuelType { get; set; }
    public TransmissionType Transmission { get; set; }
    public BodyStyle BodyStyle { get; set; }
    public VehicleCondition Condition { get; set; }
    public string Location { get; set; } = string.Empty;
    public DateTime PublishedAt { get; set; }
    public string? PrimaryImageUrl { get; set; }
    public InspectionStatus InspectionStatus { get; set; }
    public bool IsFeatured { get; set; }
}

public class PublicListingDetails : PublicListingSummary
{
    public string Description { get; set; } = string.Empty;
    public string? Variant { get; set; }
    public string? SellerName { get; set; }
    public IReadOnlyList<string> ImageUrls { get; set; } = Array.Empty<string>();
    public IReadOnlyList<string> Features { get; set; } = Array.Empty<string>();
    public string? VehicleHistory { get; set; }
    public string? InspectionSummary { get; set; }
}

public class PublicMarketplacePage
{
    public List<PublicListingSummary> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}

public class PublicSitemapListing
{
    public string Slug { get; set; } = string.Empty;
    public DateTime UpdatedAt { get; set; }
}

public class MarketplacePage
{
    public List<MarketplaceListingSummary> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}

public class MarketplaceResult<T>
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public T? Value { get; set; }
}

public class ListingStatusInput
{
    [EnumDataType(typeof(ListingStatus))]
    public ListingStatus Status { get; set; }
}

public class MarketplaceListingForm
{
    public int? Id { get; set; }

    [Required]
    public int InventoryItemId { get; set; }

    [Required, MaxLength(160)]
    public string Title { get; set; } = string.Empty;

    [Required, MaxLength(4000)]
    public string Description { get; set; } = string.Empty;

    [Range(0.01, double.MaxValue)]
    public decimal Price { get; set; }

    [Range(1886, 2100)]
    public int Year { get; set; }

    [Range(0, int.MaxValue)]
    public int Mileage { get; set; }

    public FuelType FuelType { get; set; }
    public TransmissionType Transmission { get; set; }
    public BodyStyle BodyStyle { get; set; }
    public VehicleCondition Condition { get; set; }

    [Required, MaxLength(160)]
    public string Location { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string? Features { get; set; }

    [MaxLength(4000)]
    public string? VehicleHistory { get; set; }

    public InspectionStatus InspectionStatus { get; set; }

    [MaxLength(2000)]
    public string? InspectionSummary { get; set; }

    public bool ShowSellerName { get; set; } = true;
    public DateTime? ExpiresAt { get; set; }

    public bool Publish { get; set; }
    public string? ImageUrls { get; set; }

    public MarketplaceListingInput ToInput() => new()
    {
        InventoryItemId = InventoryItemId,
        Title = Title,
        Description = Description,
        Price = Price,
        Year = Year,
        Mileage = Mileage,
        FuelType = FuelType,
        Transmission = Transmission,
        BodyStyle = BodyStyle,
        Condition = Condition,
        Location = Location,
        Features = Features,
        VehicleHistory = VehicleHistory,
        InspectionStatus = InspectionStatus,
        InspectionSummary = InspectionSummary,
        ShowSellerName = ShowSellerName,
        ExpiresAt = ExpiresAt,
        Publish = Publish,
        ImageUrls = (ImageUrls ?? string.Empty)
            .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList()
    };
}
