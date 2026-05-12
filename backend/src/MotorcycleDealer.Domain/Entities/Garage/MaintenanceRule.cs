using MotorcycleDealer.Domain.Common;

namespace MotorcycleDealer.Domain.Entities.Garage;

public sealed class MaintenanceRule : AuditableEntity
{
    public MaintenanceRule()
    {
    }

    public MaintenanceRule(Guid id)
    {
        if (id != Guid.Empty)
        {
            Id = id;
        }
    }

    public Guid MotorcycleModelId { get; set; }
    public string MilestoneCode { get; set; } = string.Empty;
    public string MilestoneName { get; set; } = string.Empty;
    public int IntervalKm { get; set; }
    public int IntervalMonths { get; set; }
    public bool IsRecurring { get; set; } = true;
    public int LeadTimeDays { get; set; } = 7;
    public int LeadMileageKm { get; set; } = 250;
    public int EstimatedDurationMinutes { get; set; } = 60;
    public string ChecklistSummary { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;

    public Showroom.MotorcycleModel? MotorcycleModel { get; set; }
    public ICollection<MaintenanceReminder> MaintenanceReminders { get; set; } = new List<MaintenanceReminder>();
}
