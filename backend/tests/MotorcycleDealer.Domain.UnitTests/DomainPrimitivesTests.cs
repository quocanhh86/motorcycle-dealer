using MotorcycleDealer.Domain.Common;
using Xunit;

namespace MotorcycleDealer.Domain.UnitTests;

public sealed class DomainPrimitivesTests
{
    private sealed record SampleDomainEvent(string Name) : DomainEventBase;

    private sealed class SampleAggregate : AuditableEntity
    {
        public void RaiseSampleEvent(string name) => AddDomainEvent(new SampleDomainEvent(name));
    }

    [Fact]
    public void BaseEntity_ShouldGenerateVersion7Uuid_AndManageDomainEvents()
    {
        var entity = new SampleAggregate();

        Assert.NotEqual(Guid.Empty, entity.Id);
        Assert.Equal(7, entity.Id.Version);

        entity.RaiseSampleEvent("Created");
        Assert.Single(entity.DomainEvents);

        entity.ClearDomainEvents();
        Assert.Empty(entity.DomainEvents);
    }

    [Fact]
    public void Result_ShouldEnforceSuccessAndFailureInvariants()
    {
        var ok = Result.Success(42);
        Assert.True(ok.IsSuccess);
        Assert.False(ok.IsFailure);
        Assert.Equal(42, ok.Value);
        Assert.Equal(Error.None, ok.Error);

        var fail = Result.Failure<int>(Error.Conflict("SLOT_CAPACITY_EXHAUSTED", "Slot is full."));
        Assert.True(fail.IsFailure);
        Assert.False(fail.IsSuccess);
        Assert.Equal("SLOT_CAPACITY_EXHAUSTED", fail.Error.Code);
        Assert.Throws<InvalidOperationException>(() => _ = fail.Value);
    }
}
