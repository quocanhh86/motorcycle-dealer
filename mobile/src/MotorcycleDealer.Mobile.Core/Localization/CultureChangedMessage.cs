using CommunityToolkit.Mvvm.Messaging.Messages;

namespace MotorcycleDealer.Mobile.Core.Localization;

/// <summary>
/// Broadcast via WeakReferenceMessenger when the active UI culture switches live between vi-VN and en-US.
/// </summary>
public sealed class CultureChangedMessage(string cultureCode)
    : ValueChangedMessage<string>(cultureCode)
{
    public string CultureCode => Value;
}
