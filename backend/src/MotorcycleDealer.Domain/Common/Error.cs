namespace MotorcycleDealer.Domain.Common;

public sealed record Error(string Code, string Description)
{
    public static readonly Error None = new(string.Empty, string.Empty);
    public static readonly Error NullValue = new("VALIDATION_NULL_VALUE", "A required value was null.");

    public static Error Validation(string code, string description) => new(code, description);
    public static Error NotFound(string code, string description) => new(code, description);
    public static Error Conflict(string code, string description) => new(code, description);
    public static Error Forbidden(string code, string description) => new(code, description);
    public static Error Unprocessable(string code, string description) => new(code, description);
}
