using System.Reflection;
using Microsoft.EntityFrameworkCore;
using MotorcycleDealer.Application.Common.Interfaces;
using MotorcycleDealer.Domain.Common;
using MotorcycleDealer.Domain.Constants;
using MotorcycleDealer.Domain.Entities.Booking;
using MotorcycleDealer.Domain.Entities.Garage;
using MotorcycleDealer.Domain.Entities.Identity;
using MotorcycleDealer.Domain.Entities.Loyalty;
using MotorcycleDealer.Domain.Entities.Notifications;
using MotorcycleDealer.Domain.Entities.Showroom;

namespace MotorcycleDealer.Infrastructure.Persistence;

public sealed class MotorcycleDealerDbContext : DbContext, IApplicationDbContext
{
    private readonly ICurrentUserContext? _currentUserContext;

    public MotorcycleDealerDbContext(
        DbContextOptions<MotorcycleDealerDbContext> options,
        ICurrentUserContext? currentUserContext = null)
        : base(options)
    {
        _currentUserContext = currentUserContext;
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    public DbSet<Dealer> Dealers => Set<Dealer>();
    public DbSet<MotorcycleModel> MotorcycleModels => Set<MotorcycleModel>();
    public DbSet<DealerInventory> DealerInventories => Set<DealerInventory>();
    public DbSet<SalesOrder> SalesOrders => Set<SalesOrder>();
    public DbSet<TestDriveBooking> TestDriveBookings => Set<TestDriveBooking>();

    public DbSet<CustomerVehicle> CustomerVehicles => Set<CustomerVehicle>();
    public DbSet<MaintenanceRule> MaintenanceRules => Set<MaintenanceRule>();
    public DbSet<MaintenanceReminder> MaintenanceReminders => Set<MaintenanceReminder>();
    public DbSet<WarrantyClaim> WarrantyClaims => Set<WarrantyClaim>();

    public DbSet<ServiceTimeSlot> ServiceTimeSlots => Set<ServiceTimeSlot>();
    public DbSet<ServiceBooking> ServiceBookings => Set<ServiceBooking>();
    public DbSet<SparePart> SpareParts => Set<SparePart>();
    public DbSet<ServiceInvoice> ServiceInvoices => Set<ServiceInvoice>();
    public DbSet<ServiceInvoiceLineItem> ServiceInvoiceLineItems => Set<ServiceInvoiceLineItem>();
    public DbSet<ServiceReview> ServiceReviews => Set<ServiceReview>();

    public DbSet<LoyaltyTier> LoyaltyTiers => Set<LoyaltyTier>();
    public DbSet<LoyaltyAccount> LoyaltyAccounts => Set<LoyaltyAccount>();
    public DbSet<PointLedgerEntry> PointLedgerEntries => Set<PointLedgerEntry>();
    public DbSet<VoucherCatalog> VoucherCatalogs => Set<VoucherCatalog>();
    public DbSet<CustomerVoucher> CustomerVouchers => Set<CustomerVoucher>();

    public DbSet<Notification> Notifications => Set<Notification>();

    internal bool IsDealerStaffScopeActive =>
        string.Equals(_currentUserContext?.Role, RoleNames.DealerStaff, StringComparison.Ordinal);

    internal Guid CurrentDealerIdOrEmpty => _currentUserContext?.DealerId ?? Guid.Empty;

    public async Task<bool> CanConnectAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await Database.CanConnectAsync(cancellationToken);
        }
        catch
        {
            return false;
        }
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        EnforceDealerScopeWriteGuard();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default)
    {
        EnforceDealerScopeWriteGuard();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        base.ConfigureConventions(configurationBuilder);

        configurationBuilder.Properties<decimal>().HavePrecision(18, 2);
        configurationBuilder.Properties<decimal?>().HavePrecision(18, 2);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasDefaultSchema("dbo");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(MotorcycleDealerDbContext).Assembly);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (entityType.IsOwned() || entityType.BaseType is not null)
            {
                continue;
            }

            var clrType = entityType.ClrType;
            var isDealerScoped = typeof(IDealerScoped).IsAssignableFrom(clrType);
            var isSoftDeletable = typeof(ISoftDeletable).IsAssignableFrom(clrType);
            var isConcurrencyProtected = typeof(IConcurrencyProtected).IsAssignableFrom(clrType);

            if (isConcurrencyProtected)
            {
                modelBuilder.Entity(clrType)
                    .Property(nameof(IConcurrencyProtected.RowVersion))
                    .IsRowVersion();
            }

            if (isDealerScoped && isSoftDeletable)
            {
                InvokeGenericFilterRegistration(nameof(ApplyDealerAndSoftDeleteQueryFilter), modelBuilder, clrType);
            }
            else if (isDealerScoped)
            {
                InvokeGenericFilterRegistration(nameof(ApplyDealerScopedQueryFilter), modelBuilder, clrType);
            }
            else if (isSoftDeletable)
            {
                InvokeGenericFilterRegistration(nameof(ApplySoftDeleteQueryFilter), modelBuilder, clrType);
            }
        }
    }

    private void InvokeGenericFilterRegistration(string methodName, ModelBuilder modelBuilder, Type entityClrType)
    {
        var method = typeof(MotorcycleDealerDbContext)
            .GetMethod(methodName, BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic)!
            .MakeGenericMethod(entityClrType);

        method.Invoke(method.IsStatic ? null : this, [modelBuilder]);
    }

    private void ApplyDealerScopedQueryFilter<TEntity>(ModelBuilder modelBuilder)
        where TEntity : class, IDealerScoped
    {
        modelBuilder.Entity<TEntity>().HasQueryFilter(e =>
            !IsDealerStaffScopeActive ||
            (CurrentDealerIdOrEmpty != Guid.Empty && e.DealerId == CurrentDealerIdOrEmpty));
    }

    private static void ApplySoftDeleteQueryFilter<TEntity>(ModelBuilder modelBuilder)
        where TEntity : class, ISoftDeletable
    {
        modelBuilder.Entity<TEntity>().HasQueryFilter(e => !e.IsDeleted);
    }

    private void ApplyDealerAndSoftDeleteQueryFilter<TEntity>(ModelBuilder modelBuilder)
        where TEntity : class, IDealerScoped, ISoftDeletable
    {
        modelBuilder.Entity<TEntity>().HasQueryFilter(e =>
            !e.IsDeleted &&
            (!IsDealerStaffScopeActive ||
             (CurrentDealerIdOrEmpty != Guid.Empty && e.DealerId == CurrentDealerIdOrEmpty)));
    }

    private void EnforceDealerScopeWriteGuard()
    {
        if (!IsDealerStaffScopeActive)
        {
            return;
        }

        var assignedDealerId = _currentUserContext?.DealerId;
        if (assignedDealerId is null || assignedDealerId == Guid.Empty)
        {
            throw new ForbiddenAccessException(
                "DealerStaff caller lacks an assigned DealerId scope.",
                "FORBIDDEN_DEALER_SCOPE");
        }

        foreach (var entry in ChangeTracker.Entries<IDealerScoped>())
        {
            if (entry.State is EntityState.Added or EntityState.Modified or EntityState.Deleted &&
                entry.Entity.DealerId != assignedDealerId.Value)
            {
                throw new ForbiddenAccessException(
                    "DealerStaff is strictly prohibited from mutating records belonging to another dealer.",
                    "FORBIDDEN_DEALER_SCOPE");
            }
        }
    }
}
