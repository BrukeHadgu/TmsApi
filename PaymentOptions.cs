using System.ComponentModel.DataAnnotations;
public class PaymentOptions
{
    [Required]
    public required string GatewayUrl { get; init; }

    [Range(100, 100_000)]
    public decimal MaxDepositBirr { get; init; }
}










































/*
using System.ComponentModel.DataAnnotations;

// Strongly-typed options class
// [Required] and [Range] are Data Annotations —
// the framework reads these at startup and validates them
public class PaymentOptions
{
    // [Required] — this field must exist in appsettings.json
    // If missing → app refuses to start
    [Required]
    public required string GatewayUrl { get; init; }

    // [Range] — value must be between 100 and 100,000
    // If outside range → app refuses to start
    [Range(100, 100_000)]
    public decimal MaxDepositBirr { get; init; }
}*/