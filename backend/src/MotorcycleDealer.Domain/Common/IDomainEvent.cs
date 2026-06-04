namespace MotorcycleDealer.Domain.Common;

public interface IDomainEvent
{
    Guid EventId { get; }
    DateTimeOffset OccurredAtUtc { get; }
}

public abstract record DomainEventBase : IDomainEvent
{
    public Guid EventId { get; init; } = Guid.CreateVersion7();
    public DateTimeOffset OccurredAtUtc { get; init; } = DateTimeOffset.UtcNow;
}
