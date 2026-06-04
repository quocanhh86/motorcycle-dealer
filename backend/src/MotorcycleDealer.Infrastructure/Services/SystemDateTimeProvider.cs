using MotorcycleDealer.Application.Common.Interfaces;

namespace MotorcycleDealer.Infrastructure.Services;

public sealed class SystemDateTimeProvider : IDateTimeProvider
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
