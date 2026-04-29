namespace MotorcycleDealer.Domain.Common;

public sealed class BusinessRuleViolationException : DomainException
{
    public BusinessRuleViolationException(string errorCode, string message)
        : base(errorCode, message)
    {
    }
}
