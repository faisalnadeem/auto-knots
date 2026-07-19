using System.ComponentModel.DataAnnotations;

namespace AutoKnots.Models;

public class ListingImage
{
    public int Id { get; set; }
    public int VehicleListingId { get; set; }
    public VehicleListing VehicleListing { get; set; } = null!;

    [Required, MaxLength(2048)]
    public string Url { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? AltText { get; set; }

    public int SortOrder { get; set; }
}
