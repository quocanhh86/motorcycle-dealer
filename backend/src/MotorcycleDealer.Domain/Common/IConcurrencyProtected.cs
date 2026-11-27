namespace MotorcycleDealer.Domain.Common;

public interface IConcurrencyProtected
{
    byte[] RowVersion { get; }
}
