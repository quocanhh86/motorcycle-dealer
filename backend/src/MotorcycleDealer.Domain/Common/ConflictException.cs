namespace MotorcycleDealer.Domain.Common;

public sealed class ConflictException : DomainException
{
    public ConflictException(string message, string errorCode = "CONCURRENCY_CONFLICT")
        : base(errorCode, message)
    {
    }
}
