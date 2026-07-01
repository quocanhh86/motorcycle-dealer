using System.Text.Json;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MotorcycleDealer.Application.Common.Interfaces;
using MotorcycleDealer.Application.Features.Bookings;
using MotorcycleDealer.Domain.Entities.Garage;
using MotorcycleDealer.Domain.Entities.Notifications;
using MotorcycleDealer.Infrastructure.Persistence;

namespace MotorcycleDealer.Infrastructure.BackgroundJobs;

public sealed class VinMaintenanceReminderScanner : IVinMaintenanceReminderScanner
{
    private readonly MotorcycleDealerDbContext _dbContext;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly INotificationPublisher _notificationPublisher;
    private readonly IOptions<VinMaintenanceReminderOptions> _options;
    private readonly ILogger<VinMaintenanceReminderScanner> _logger;

    public VinMaintenanceReminderScanner(
        MotorcycleDealerDbContext dbContext,
        IDateTimeProvider dateTimeProvider,
        INotificationPublisher notificationPublisher,
        IOptions<VinMaintenanceReminderOptions> options,
        ILogger<VinMaintenanceReminderScanner> logger)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _dateTimeProvider = dateTimeProvider ?? throw new ArgumentNullException(nameof(dateTimeProvider));
        _notificationPublisher = notificationPublisher ?? throw new ArgumentNullException(nameof(notificationPublisher));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<VinMaintenanceReminderScanResult> ScanAndDispatchDueRemindersAsync(
        CancellationToken cancellationToken = default)
    {
        var utcNow = _dateTimeProvider.UtcNow;
        var workerOptions = _options.Value;
        var batchSize = Math.Clamp(workerOptions.BatchSize, 10, 1000);
        var defaultMileageLeadKm = Math.Max(0, workerOptions.MileageLeadKm);
        var defaultDaysLeadWindow = Math.Max(0, workerOptions.DaysLeadWindow);

        var activeRulesByModel = (await _dbContext.MaintenanceRules
                .IgnoreQueryFilters()
                .AsNoTracking()
                .Where(r => r.IsActive && r.IntervalKm > 0 && r.IntervalMonths > 0)
                .OrderBy(r => r.IntervalKm)
                .ToListAsync(cancellationToken))
            .GroupBy(r => r.MotorcycleModelId)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<MaintenanceRule>)g.ToList());

        if (activeRulesByModel.Count == 0)
        {
            return new VinMaintenanceReminderScanResult(0, 0, 0, utcNow);
        }

        var modelNamesById = await _dbContext.MotorcycleModels
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Select(m => new { m.Id, m.ModelName })
            .ToDictionaryAsync(m => m.Id, m => m.ModelName, cancellationToken);

        var vehiclesEvaluated = 0;
        var remindersCreated = 0;
        var remindersSkippedExisting = 0;
        var offset = 0;

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var vehicleBatch = await _dbContext.CustomerVehicles
                .IgnoreQueryFilters()
                .AsNoTracking()
                .Where(v => v.IsActive && !v.IsDeleted)
                .OrderBy(v => v.NextDueDateUtc)
                .ThenBy(v => v.NextDueMileageKm)
                .ThenBy(v => v.Id)
                .Skip(offset)
                .Take(batchSize)
                .ToListAsync(cancellationToken);

            if (vehicleBatch.Count == 0)
            {
                break;
            }

            offset += vehicleBatch.Count;
            var batchVehicleIds = vehicleBatch.Select(v => v.Id).ToArray();

            var vehiclesWithActiveBookings = (await _dbContext.ServiceBookings
                    .IgnoreQueryFilters()
                    .AsNoTracking()
                    .Where(b =>
                        batchVehicleIds.Contains(b.CustomerVehicleId) &&
                        b.Status != "Completed" &&
                        b.Status != "Cancelled" &&
                        b.Status != "NoShow")
                    .Select(b => b.CustomerVehicleId)
                    .Distinct()
                    .ToListAsync(cancellationToken))
                .ToHashSet();

            var existingDedupKeys = (await _dbContext.MaintenanceReminders
                    .IgnoreQueryFilters()
                    .AsNoTracking()
                    .Where(r => batchVehicleIds.Contains(r.CustomerVehicleId))
                    .Select(r => r.DeduplicationKey)
                    .ToListAsync(cancellationToken))
                .ToHashSet(StringComparer.Ordinal);

            foreach (var vehicle in vehicleBatch)
            {
                vehiclesEvaluated++;

                if (vehiclesWithActiveBookings.Contains(vehicle.Id))
                {
                    continue;
                }

                if (!activeRulesByModel.TryGetValue(vehicle.MotorcycleModelId, out var modelRules) ||
                    modelRules.Count == 0)
                {
                    continue;
                }

                var winningEvaluation = SelectNextDueMilestoneForVehicle(
                    vehicle,
                    modelRules,
                    utcNow,
                    defaultMileageLeadKm,
                    defaultDaysLeadWindow);

                if (winningEvaluation is null)
                {
                    continue;
                }

                if (!existingDedupKeys.Add(winningEvaluation.DeduplicationKey))
                {
                    remindersSkippedExisting++;
                    continue;
                }

                var modelName = modelNamesById.GetValueOrDefault(vehicle.MotorcycleModelId, "Motorcycle");
                var dispatchedEvent = await PersistSingleReminderIdempotentlyAsync(
                    vehicle,
                    modelName,
                    winningEvaluation.Rule,
                    winningEvaluation,
                    utcNow,
                    cancellationToken);

                if (dispatchedEvent is null)
                {
                    remindersSkippedExisting++;
                    continue;
                }

                remindersCreated++;
                await _notificationPublisher.PublishMaintenanceReminderAsync(
                    dispatchedEvent,
                    vehicle.CustomerId,
                    cancellationToken);
            }

            if (vehicleBatch.Count < batchSize)
            {
                break;
            }
        }

        _logger.LogInformation(
            "VinMaintenanceReminderScanner finished at {EvaluatedAtUtc}: Vehicles={VehiclesEvaluated}, Created={RemindersCreated}, SkippedExisting={RemindersSkipped}.",
            utcNow,
            vehiclesEvaluated,
            remindersCreated,
            remindersSkippedExisting);

        return new VinMaintenanceReminderScanResult(
            VehiclesEvaluated: vehiclesEvaluated,
            RemindersCreated: remindersCreated,
            RemindersSkippedExisting: remindersSkippedExisting,
            EvaluatedAtUtc: utcNow);
    }

    public static DueMilestoneEvaluation? SelectNextDueMilestoneForVehicle(
        CustomerVehicle vehicle,
        IReadOnlyList<MaintenanceRule> modelRules,
        DateTimeOffset utcNow,
        int defaultMileageLeadKm,
        int defaultDaysLeadWindow)
    {
        var baselineKm = Math.Max(0, vehicle.LastServiceMileageKm);
        var baselineDateUtc = vehicle.LastServiceDateUtc ?? vehicle.OwnershipStartDateUtc;

        var candidates = new List<(MaintenanceRule Rule, int TargetMileageKm, DateTimeOffset DueDateUtc)>(modelRules.Count);
        foreach (var rule in modelRules)
        {
            int targetMileageKm;
            if (!rule.IsRecurring)
            {
                if (baselineKm >= rule.IntervalKm || vehicle.CurrentMileageKm >= rule.IntervalKm * 2)
                {
                    continue;
                }

                targetMileageKm = rule.IntervalKm;
            }
            else
            {
                targetMileageKm = ((baselineKm / rule.IntervalKm) + 1) * rule.IntervalKm;
            }

            var computedDueDateUtc = baselineDateUtc.AddMonths(rule.IntervalMonths);
            var dueDateUtc = vehicle.NextDueMileageKm == targetMileageKm &&
                             vehicle.NextDueDateUtc != default &&
                             vehicle.NextDueDateUtc < computedDueDateUtc
                ? vehicle.NextDueDateUtc
                : computedDueDateUtc;

            candidates.Add((rule, targetMileageKm, dueDateUtc));
        }

        if (candidates.Count == 0)
        {
            return null;
        }

        // Select the single next actionable milestone; when recurring rules overlap at the exact
        // same target km (e.g., 4,000 km periodic + 12,000 km major both hitting 12,000 km),
        // prefer the higher-interval comprehensive rule (MAJOR_12000) per MOD_03 §5.2 #2.
        var winningCandidate = candidates
            .OrderBy(c => c.TargetMileageKm)
            .ThenByDescending(c => c.Rule.IntervalKm)
            .First();

        var effectiveLeadKm = Math.Max(winningCandidate.Rule.LeadMileageKm, defaultMileageLeadKm);
        var effectiveLeadDays = Math.Max(winningCandidate.Rule.LeadTimeDays, defaultDaysLeadWindow);

        string? triggerReason = null;
        if (vehicle.CurrentMileageKm >= winningCandidate.TargetMileageKm || utcNow >= winningCandidate.DueDateUtc)
        {
            triggerReason = "Overdue";
        }
        else if (vehicle.CurrentMileageKm >= winningCandidate.TargetMileageKm - effectiveLeadKm)
        {
            triggerReason = "MileageThreshold";
        }
        else if (utcNow >= winningCandidate.DueDateUtc.AddDays(-effectiveLeadDays))
        {
            triggerReason = "DateThreshold";
        }

        if (triggerReason is null)
        {
            return null;
        }

        var deduplicationKey = $"{vehicle.VIN}:{winningCandidate.Rule.Id}:{winningCandidate.TargetMileageKm}";
        return new DueMilestoneEvaluation(
            winningCandidate.Rule,
            winningCandidate.TargetMileageKm,
            winningCandidate.DueDateUtc,
            triggerReason,
            deduplicationKey);
    }

    private async Task<MaintenanceReminderRealtimeEvent?> PersistSingleReminderIdempotentlyAsync(
        CustomerVehicle vehicle,
        string modelName,
        MaintenanceRule rule,
        DueMilestoneEvaluation evaluation,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken)
    {
        var executionStrategy = _dbContext.Database.CreateExecutionStrategy();

        try
        {
            return await executionStrategy.ExecuteAsync(async ct =>
            {
                _dbContext.ChangeTracker.Clear();
                await using var transaction = await _dbContext.Database.BeginTransactionAsync(ct);

                var alreadyExists = await _dbContext.MaintenanceReminders
                    .IgnoreQueryFilters()
                    .AnyAsync(r => r.DeduplicationKey == evaluation.DeduplicationKey, ct);

                if (alreadyExists)
                {
                    return null;
                }

                var reminder = new MaintenanceReminder
                {
                    CustomerVehicleId = vehicle.Id,
                    MaintenanceRuleId = rule.Id,
                    TargetMileageKm = evaluation.TargetMileageKm,
                    DueDateUtc = evaluation.DueDateUtc,
                    TriggerReason = evaluation.TriggerReason,
                    DeduplicationKey = evaluation.DeduplicationKey,
                    Status = "Sent",
                    SentAtUtc = utcNow,
                    CreatedAtUtc = utcNow
                };

                var notification = new Notification
                {
                    RecipientUserId = vehicle.CustomerId,
                    EventType = "MaintenanceReminderTriggered",
                    Title = $"{rule.MilestoneName} Approaching ({evaluation.TargetMileageKm:N0} km)",
                    Message = $"Your {modelName} ({vehicle.VIN}) at {vehicle.CurrentMileageKm:N0} km is due for {rule.MilestoneName} at {evaluation.TargetMileageKm:N0} km.",
                    PayloadJson = JsonSerializer.Serialize(new
                    {
                        reminderId = reminder.Id,
                        customerVehicleId = vehicle.Id,
                        vin = vehicle.VIN,
                        motorcycleModelName = modelName,
                        milestoneCode = rule.MilestoneCode,
                        milestoneName = rule.MilestoneName,
                        currentMileageKm = vehicle.CurrentMileageKm,
                        targetMileageKm = evaluation.TargetMileageKm,
                        dueDateUtc = evaluation.DueDateUtc,
                        triggerReason = evaluation.TriggerReason,
                        deduplicationKey = evaluation.DeduplicationKey
                    }),
                    IsRead = false,
                    CreatedAtUtc = utcNow
                };

                _dbContext.MaintenanceReminders.Add(reminder);
                _dbContext.Notifications.Add(notification);

                await _dbContext.SaveChangesAsync(ct);
                await transaction.CommitAsync(ct);

                return new MaintenanceReminderRealtimeEvent(
                    NotificationId: notification.Id,
                    EventType: "MaintenanceReminderTriggered",
                    ReminderId: reminder.Id,
                    CustomerVehicleId: vehicle.Id,
                    Vin: vehicle.VIN,
                    MotorcycleModelName: modelName,
                    MilestoneName: rule.MilestoneName,
                    CurrentMileageKm: vehicle.CurrentMileageKm,
                    TargetMileageKm: evaluation.TargetMileageKm,
                    DueDateUtc: evaluation.DueDateUtc,
                    DeduplicationKey: evaluation.DeduplicationKey,
                    OccurredAtUtc: utcNow);
            }, cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 2601 or 2627 })
        {
            _dbContext.ChangeTracker.Clear();
            _logger.LogDebug(
                "Skipped concurrent duplicate MaintenanceReminder for DeduplicationKey={DeduplicationKey}.",
                evaluation.DeduplicationKey);
            return null;
        }
    }

    public sealed record DueMilestoneEvaluation(
        MaintenanceRule Rule,
        int TargetMileageKm,
        DateTimeOffset DueDateUtc,
        string TriggerReason,
        string DeduplicationKey);
}
