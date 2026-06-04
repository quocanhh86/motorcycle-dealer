namespace MotorcycleDealer.Domain.Common;

public sealed class ForbiddenAccessException : DomainException
{
    public ForbiddenAccessException(string message, string errorCode = "FORBIDDEN_PERMISSION")
        : base(errorCode, message)
    {
    }
}
