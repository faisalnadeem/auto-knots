using System.ComponentModel.DataAnnotations;

namespace AutoKnots.Models;

public class VehicleListing
{
    public int Id { get; set; }

    [Required]
    public int InventoryItemId { get; set; }

    public InventoryItem InventoryItem { get; set; } = null!;

    [Required, MaxLength(200)]
    public string Slug { get; set; } = string.Empty;

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
    public bool IsFeatured { get; set; }

    public ListingStatus Status { get; set; } = ListingStatus.Draft;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? PublishedAt { get; set; }
    public DateTime? SoldAt { get; set; }
    public DateTime? ExpiresAt { get; set; }

    public ICollection<ListingImage> Images { get; set; } = new List<ListingImage>();
}
