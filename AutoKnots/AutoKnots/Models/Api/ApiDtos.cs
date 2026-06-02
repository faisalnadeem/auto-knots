using System.ComponentModel.DataAnnotations;

namespace AutoKnots.Models.Api;

public class LoginRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}

public class RegisterRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MinLength(6)]
    public string Password { get; set; } = string.Empty;

    public string? ConfirmPassword { get; set; }
}

public class AuthResponse
{
    public string Token { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

public class CreateInventoryRequest
{
    [Required]
    public string Name { get; set; } = string.Empty;

    [Required]
    public string Make { get; set; } = string.Empty;

    [Required]
    public string Model { get; set; } = string.Empty;

    public string? Variant { get; set; }

    [Required]
    public string EngineNumber { get; set; } = string.Empty;

    [Required]
    public string ChassisNumber { get; set; } = string.Empty;

    public DateTime? PurchaseDate { get; set; }

    [Range(0, double.MaxValue)]
    public decimal CostPrice { get; set; }

    public List<InvestmentAllocationRequest>? Investors { get; set; }
}

public class UpdateInventoryRequest
{
    [Required]
    public string Name { get; set; } = string.Empty;

    [Required]
    public string Make { get; set; } = string.Empty;

    [Required]
    public string Model { get; set; } = string.Empty;

    public string? Variant { get; set; }

    [Required]
    public string EngineNumber { get; set; } = string.Empty;

    [Required]
    public string ChassisNumber { get; set; } = string.Empty;

    public DateTime? PurchaseDate { get; set; }

    [Range(0, double.MaxValue)]
    public decimal CostPrice { get; set; }

    public List<InvestmentAllocationRequest>? Investors { get; set; }
}

public class InvestmentAllocationRequest
{
    [Required]
    public string InvestorUserId { get; set; } = string.Empty;

    public decimal? Amount { get; set; }

    [Range(0, 100)]
    public decimal? Percentage { get; set; }
}

public class AddCostRequest
{
    [Required]
    public string InvestorUserId { get; set; } = string.Empty;

    [Range(0.01, double.MaxValue)]
    public decimal Amount { get; set; }

    [Required]
    [MaxLength(128)]
    public string Type { get; set; } = string.Empty;

    [MaxLength(512)]
    public string? Notes { get; set; }
}

public class UpdateCostRequest
{
    [Required]
    public string InvestorUserId { get; set; } = string.Empty;

    [Range(0.01, double.MaxValue)]
    public decimal Amount { get; set; }

    [Required]
    [MaxLength(128)]
    public string Type { get; set; } = string.Empty;

    [MaxLength(512)]
    public string? Notes { get; set; }
}

public class SellVehicleRequest
{
    [Range(0, double.MaxValue)]
    public decimal SellingPrice { get; set; }
}

public class RejectInvestmentRequest
{
    [Required]
    public string Reason { get; set; } = string.Empty;
}

public class InvestorUserDto
{
    public string Id { get; set; } = string.Empty;
    public string? Email { get; set; }
}

public class ApiErrorResponse
{
    public string Error { get; set; } = string.Empty;
}
