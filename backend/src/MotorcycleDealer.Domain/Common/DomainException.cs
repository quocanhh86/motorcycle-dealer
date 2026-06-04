namespace MotorcycleDealer.Domain.Common;

public class DomainException : Exception
{
    public DomainException(string errorCode, string message)
        : base(message)
    {
        ErrorCode = string.IsNullOrWhiteSpace(errorCode) ? "DOMAIN_RULE_VIOLATION" : errorCode;
    }

    public DomainException(string errorCode, string message, Exception innerException)
        : base(message, innerException)
    {
        ErrorCode = string.IsNullOrWhiteSpace(errorCode) ? "DOMAIN_RULE_VIOLATION" : errorCode;
    }

    public string ErrorCode { get; }
}
