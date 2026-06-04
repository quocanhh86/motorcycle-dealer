namespace MotorcycleDealer.Domain.Common;

public sealed class NotFoundException : DomainException
{
    public NotFoundException(string message, string errorCode = "RESOURCE_NOT_FOUND")
        : base(errorCode, message)
    {
    }

    public static NotFoundException ForEntity(string entityName, object key) =>
        new($"{entityName} with identifier '{key}' was not found.", "RESOURCE_NOT_FOUND");
}
