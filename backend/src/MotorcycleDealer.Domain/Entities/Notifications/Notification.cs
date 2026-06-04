using MotorcycleDealer.Domain.Common;

namespace MotorcycleDealer.Domain.Entities.Notifications;

public sealed class Notification : BaseEntity
{
    public Notification()
    {
    }

    public Notification(Guid id)
    {
        if (id != Guid.Empty)
        {
            Id = id;
        }
    }

    public Guid? RecipientUserId { get; set; }
    public Guid? TargetDealerId { get; set; }
    public string? TargetRole { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? PayloadJson { get; set; }
    public bool IsRead { get; set; }
    public DateTimeOffset? ReadAtUtc { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }

    public Identity.User? RecipientUser { get; set; }
    public Showroom.Dealer? TargetDealer { get; set; }
}
