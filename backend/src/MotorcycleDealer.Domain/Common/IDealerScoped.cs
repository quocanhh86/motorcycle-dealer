namespace MotorcycleDealer.Domain.Common;

public interface IDealerScoped
{
    Guid DealerId { get; }
}
