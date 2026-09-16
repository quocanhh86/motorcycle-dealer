using CommunityToolkit.Mvvm.Messaging.Messages;

namespace MotorcycleDealer.Mobile.Core.Messaging;

/// <summary>
/// Broadcast after a service booking is created, cancelled, checked in, updated, or settled.
/// </summary>
public sealed class BookingMutatedMessage(Guid? bookingId = null)
    : ValueChangedMessage<Guid?>(bookingId)
{
    public Guid? BookingId => Value;
}

/// <summary>
/// Broadcast after a loyalty wallet or voucher balance changes (voucher redemption, CSAT bonus, invoice/sale earn).
/// </summary>
public sealed class WalletMutatedMessage(string reason)
    : ValueChangedMessage<string>(reason)
{
    public string Reason => Value;
}

/// <summary>
/// Broadcast after a customer VIN vehicle is registered/updated or showroom VIN stock is received/sold.
/// </summary>
public sealed class VehicleMutatedMessage(string? vin = null)
    : ValueChangedMessage<string?>(vin)
{
    public string? Vin => Value;
}

/// <summary>
/// Broadcast after a VIN warranty claim is submitted or transitioned by dealer staff.
/// </summary>
public sealed class WarrantyMutatedMessage(Guid? claimId = null)
    : ValueChangedMessage<Guid?>(claimId)
{
    public Guid? ClaimId => Value;
}

/// <summary>
/// Broadcast after a showroom test-drive reservation is requested, confirmed, completed, or cancelled.
/// </summary>
public sealed class TestDriveMutatedMessage(Guid? testDriveId = null)
    : ValueChangedMessage<Guid?>(testDriveId)
{
    public Guid? TestDriveId => Value;
}
