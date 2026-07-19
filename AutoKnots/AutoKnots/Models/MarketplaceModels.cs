using System.ComponentModel.DataAnnotations;

namespace AutoKnots.Models;

public class MarketplaceSearch
{
    public string? Search { get; set; }
    public string? Make { get; set; }
    public string? Model { get; set; }
    public int? MinYear { get; set; }
    public int? MaxYear { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public int? MaxMileage { get; set; }
    public FuelType? FuelType { get; set; }
    public TransmissionType? Transmission { get; set; }
    public BodyStyle? BodyStyle { get; set; }
    public VehicleCondition? Condition { get; set; }
    public string? Location { get; set; }
    public string Sort { get; set; } = "newest";
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 12;
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
    public string Description { get; set; } = string.Empty;
    public string? Variant { get; set; }
    public string SellerName { get; set; } = string.Empty;
    public IReadOnlyList<string> ImageUrls { get; set; } = Array.Empty<string>();
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? SoldAt { get; set; }
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
