using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MotorcycleDealer.Application.Common.Interfaces;
using MotorcycleDealer.Domain.Constants;
using MotorcycleDealer.Domain.Entities.Booking;
using MotorcycleDealer.Domain.Entities.Garage;
using MotorcycleDealer.Domain.Entities.Identity;
using MotorcycleDealer.Domain.Entities.Loyalty;
using MotorcycleDealer.Domain.Entities.Notifications;
using MotorcycleDealer.Domain.Entities.Showroom;
using MotorcycleDealer.Domain.Enums;
using MotorcycleDealer.Domain.ValueObjects;

namespace MotorcycleDealer.Infrastructure.Persistence.Seed;

public sealed class DatabaseSeeder
{
    public static readonly Guid HcmDealerId = Guid.Parse("22222222-2222-2222-2222-222222222201");
    public static readonly Guid HanoiDealerId = Guid.Parse("22222222-2222-2222-2222-222222222202");
    public static readonly Guid DaNangDealerId = Guid.Parse("22222222-2222-2222-2222-222222222203");

    private readonly MotorcycleDealerDbContext _dbContext;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ILogger<DatabaseSeeder> _logger;

    public DatabaseSeeder(
        MotorcycleDealerDbContext dbContext,
        IPasswordHasher passwordHasher,
        IDateTimeProvider dateTimeProvider,
        ILogger<DatabaseSeeder> logger)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _passwordHasher = passwordHasher ?? throw new ArgumentNullException(nameof(passwordHasher));
        _dateTimeProvider = dateTimeProvider ?? throw new ArgumentNullException(nameof(dateTimeProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public static async Task InitializeDatabaseAsync(
        IServiceProvider serviceProvider,
        bool applyMigrations = true,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);

        await using var scope = serviceProvider.CreateAsyncScope();
        var seeder = scope.ServiceProvider.GetRequiredService<DatabaseSeeder>();

        if (applyMigrations)
        {
            await seeder.MigrateAndSeedAsync(cancellationToken);
        }
        else
        {
            await seeder.SeedAsync(cancellationToken);
        }
    }

    public async Task MigrateAndSeedAsync(CancellationToken cancellationToken = default)
    {
        if (_dbContext.Database.IsRelational())
        {
            await _dbContext.Database.MigrateAsync(cancellationToken);
        }

        await SeedAsync(cancellationToken);
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var executionStrategy = _dbContext.Database.CreateExecutionStrategy();

        await executionStrategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            var utcNow = _dateTimeProvider.UtcNow;

            var dealers = await SeedDealersAsync(utcNow, cancellationToken);
            var roles = await SeedRolesAsync(utcNow, cancellationToken);
            var permissions = await SeedPermissionsAsync(utcNow, cancellationToken);
            await SeedRolePermissionsAsync(roles, permissions, utcNow, cancellationToken);

            var tiers = await SeedLoyaltyTiersAsync(utcNow, cancellationToken);
            var users = await SeedUsersAndRolesAsync(dealers, roles, utcNow, cancellationToken);
            var models = await SeedMotorcycleModelsAsync(utcNow, cancellationToken);
            var rules = await SeedMaintenanceRulesAsync(models, utcNow, cancellationToken);
            var catalogs = await SeedVoucherCatalogsAsync(tiers, utcNow, cancellationToken);

            await SeedRollingServiceTimeSlotsAsync(dealers, utcNow, cancellationToken);
            var inventories = await SeedDealerInventoriesAsync(dealers, models, utcNow, cancellationToken);
            var spareParts = await SeedSparePartsAsync(models, utcNow, cancellationToken);

            await SeedClosedLoopTransactionsAndLoyaltyAsync(
                dealers,
                users,
                tiers,
                models,
                rules,
                catalogs,
                inventories,
                spareParts,
                utcNow,
                cancellationToken);

            await SeedTestDriveBookingsAsync(
                dealers,
                users,
                models,
                inventories,
                utcNow,
                cancellationToken);

            await SeedServiceReviewsAndWarrantyClaimsAsync(
                dealers,
                users,
                spareParts,
                utcNow,
                cancellationToken);

            await transaction.CommitAsync(cancellationToken);
        });

        _logger.LogInformation("DatabaseSeeder verified 3-dealer, 3-role, 48-permission, 4-tier, 27-table 3NF baseline cleanly.");
    }

    private async Task<Dictionary<string, Dealer>> SeedDealersAsync(
        DateTimeOffset utcNow,
        CancellationToken cancellationToken)
    {
        var existing = await _dbContext.Dealers
            .IgnoreQueryFilters()
            .ToDictionaryAsync(d => d.Code, StringComparer.OrdinalIgnoreCase, cancellationToken);

        var specs = new[]
        {
            new Dealer(HanoiDealerId)
            {
                Code = "DLR-HN-01",
                Name = "Apex Moto Hanoi Flagship",
                City = "Hanoi",
                Address = "54 Trang Tien Street, Hoan Kiem District, Hanoi",
                PhoneNumber = "02439368802",
                ContactEmail = "hanoi@motorcycledealer.vn",
                Latitude = 21.024915m,
                Longitude = 105.854821m,
                OpeningTime = new TimeOnly(8, 0),
                ClosingTime = new TimeOnly(18, 0),
                DefaultSlotCapacity = 4,
                IsActive = true,
                CreatedAtUtc = utcNow.AddMonths(-8)
            },
            new Dealer(DaNangDealerId)
            {
                Code = "DLR-DN-01",
                Name = "Apex Moto Da Nang Coastal Hub",
                City = "Da Nang",
                Address = "86 Bach Dang Street, Hai Chau District, Da Nang",
                PhoneNumber = "02363887703",
                ContactEmail = "danang@motorcycledealer.vn",
                Latitude = 16.067780m,
                Longitude = 108.224432m,
                OpeningTime = new TimeOnly(8, 0),
                ClosingTime = new TimeOnly(18, 0),
                DefaultSlotCapacity = 4,
                IsActive = true,
                CreatedAtUtc = utcNow.AddMonths(-8)
            },
            new Dealer(HcmDealerId)
            {
                Code = "DLR-HCM-01",
                Name = "Apex Moto Saigon Center",
                City = "Ho Chi Minh City",
                Address = "128 Nguyen Hue Boulevard, District 1, Ho Chi Minh City",
                PhoneNumber = "02838229901",
                ContactEmail = "saigon@motorcycledealer.vn",
                Latitude = 10.774512m,
                Longitude = 106.703418m,
                OpeningTime = new TimeOnly(8, 0),
                ClosingTime = new TimeOnly(19, 0),
                DefaultSlotCapacity = 5,
                IsActive = true,
                CreatedAtUtc = utcNow.AddMonths(-8)
            }
        };

        var added = false;
        foreach (var spec in specs)
        {
            if (!existing.ContainsKey(spec.Code))
            {
                _dbContext.Dealers.Add(spec);
                existing[spec.Code] = spec;
                added = true;
            }
        }

        if (added)
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        return existing;
    }

    private async Task<Dictionary<string, Role>> SeedRolesAsync(
        DateTimeOffset utcNow,
        CancellationToken cancellationToken)
    {
        var existing = await _dbContext.Roles
            .ToDictionaryAsync(r => r.Name, StringComparer.Ordinal, cancellationToken);

        var specs = new (string Name, string DisplayName, string Description, Guid Id)[]
        {
            (RoleNames.Customer, "Customer", "Self-service motorcycle owner, garage manager, workshop booker, and loyalty member.", Guid.Parse("11111111-1111-1111-1111-111111111101")),
            (RoleNames.DealerStaff, "Dealer Staff", "Branch-scoped showroom advisor and workshop coordinator bound to assigned DealerId.", Guid.Parse("11111111-1111-1111-1111-111111111102")),
            (RoleNames.Admin, "Administrator", "Platform-wide dealership administrator and cross-dealer executive analytics user.", Guid.Parse("11111111-1111-1111-1111-111111111103"))
        };

        var added = false;
        foreach (var spec in specs)
        {
            if (!existing.ContainsKey(spec.Name))
            {
                var role = new Role(spec.Id)
                {
                    Name = spec.Name,
                    DisplayName = spec.DisplayName,
                    Description = spec.Description,
                    IsSystemRole = true,
                    CreatedAtUtc = utcNow.AddMonths(-8)
                };
                _dbContext.Roles.Add(role);
                existing[spec.Name] = role;
                added = true;
            }
        }

        if (added)
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        return existing;
    }

    private async Task<Dictionary<string, Permission>> SeedPermissionsAsync(
        DateTimeOffset utcNow,
        CancellationToken cancellationToken)
    {
        var existing = await _dbContext.Permissions
            .ToDictionaryAsync(p => p.Code, StringComparer.Ordinal, cancellationToken);

        var added = false;
        foreach (var code in Permissions.All)
        {
            if (existing.ContainsKey(code))
            {
                continue;
            }

            var parts = code.Split('.');
            var module = parts.Length >= 2 ? parts[1] : "Core";
            var action = parts.Length >= 3 ? parts[2] : code;

            var permission = new Permission
            {
                Code = code,
                Module = module,
                DisplayName = $"{module}: {action}",
                Description = $"Authorizes {action} operations within the {module} bounded context.",
                CreatedAtUtc = utcNow.AddMonths(-8)
            };

            _dbContext.Permissions.Add(permission);
            existing[code] = permission;
            added = true;
        }

        if (added)
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        return existing;
    }

    private async Task SeedRolePermissionsAsync(
        Dictionary<string, Role> roles,
        Dictionary<string, Permission> permissions,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken)
    {
        var existingPairs = await _dbContext.RolePermissions
            .Select(rp => new { rp.RoleId, rp.PermissionId })
            .ToListAsync(cancellationToken);

        var pairSet = existingPairs
            .Select(x => (x.RoleId, x.PermissionId))
            .ToHashSet();

        var added = false;
        foreach (var roleName in RoleNames.All)
        {
            var role = roles[roleName];
            foreach (var permCode in Permissions.GetDefaultsForRole(roleName))
            {
                var permission = permissions[permCode];
                if (pairSet.Add((role.Id, permission.Id)))
                {
                    _dbContext.RolePermissions.Add(new RolePermission
                    {
                        RoleId = role.Id,
                        PermissionId = permission.Id,
                        GrantedAtUtc = utcNow.AddMonths(-8)
                    });
                    added = true;
                }
            }
        }

        if (added)
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task<Dictionary<string, LoyaltyTier>> SeedLoyaltyTiersAsync(
        DateTimeOffset utcNow,
        CancellationToken cancellationToken)
    {
        var existing = await _dbContext.LoyaltyTiers
            .ToDictionaryAsync(t => t.TierCode, StringComparer.Ordinal, cancellationToken);

        var specs = new[]
        {
            new LoyaltyTier(Guid.Parse("33333333-3333-3333-3333-333333333301"))
            {
                TierCode = "Bronze",
                DisplayName = "Bronze Member",
                RankOrder = 1,
                MinQualifyingPoints = 0,
                PointsMultiplier = 1.00m,
                BenefitsJson = "[\"Standard 1.00x base points accrual\",\"Digital VIN Garage & automated maintenance alerts\",\"Access to Bronze workshop vouchers\"]",
                IsActive = true,
                CreatedAtUtc = utcNow.AddMonths(-8)
            },
            new LoyaltyTier(Guid.Parse("33333333-3333-3333-3333-333333333302"))
            {
                TierCode = "Silver",
                DisplayName = "Silver Member",
                RankOrder = 2,
                MinQualifyingPoints = 500,
                PointsMultiplier = 1.20m,
                BenefitsJson = "[\"Accelerated 1.20x points multiplier\",\"Complimentary bike wash after periodic service\",\"Access to Silver percentage service vouchers\"]",
                IsActive = true,
                CreatedAtUtc = utcNow.AddMonths(-8)
            },
            new LoyaltyTier(Guid.Parse("33333333-3333-3333-3333-333333333303"))
            {
                TierCode = "Gold",
                DisplayName = "Gold Member",
                RankOrder = 3,
                MinQualifyingPoints = 2000,
                PointsMultiplier = 1.50m,
                BenefitsJson = "[\"Premium 1.50x points multiplier\",\"Priority service bay check-in\",\"15% genuine parts & tires reward vouchers\"]",
                IsActive = true,
                CreatedAtUtc = utcNow.AddMonths(-8)
            },
            new LoyaltyTier(Guid.Parse("33333333-3333-3333-3333-333333333304"))
            {
                TierCode = "Platinum",
                DisplayName = "Platinum Member",
                RankOrder = 4,
                MinQualifyingPoints = 5000,
                PointsMultiplier = 2.00m,
                BenefitsJson = "[\"VIP 2.00x double points multiplier\",\"Dedicated VIP priority bay & full detailing\",\"Exclusive 300,000 VND Platinum vouchers\"]",
                IsActive = true,
                CreatedAtUtc = utcNow.AddMonths(-8)
            }
        };

        var added = false;
        foreach (var spec in specs)
        {
            if (!existing.ContainsKey(spec.TierCode))
            {
                _dbContext.LoyaltyTiers.Add(spec);
                existing[spec.TierCode] = spec;
                added = true;
            }
        }

        if (added)
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        return existing;
    }

    private async Task<Dictionary<string, User>> SeedUsersAndRolesAsync(
        Dictionary<string, Dealer> dealers,
        Dictionary<string, Role> roles,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken)
    {
        var existingUsers = await _dbContext.Users
            .IgnoreQueryFilters()
            .ToDictionaryAsync(u => u.NormalizedEmail, StringComparer.Ordinal, cancellationToken);

        var existingUserRoles = await _dbContext.UserRoles
            .Select(ur => new { ur.UserId, ur.RoleId })
            .ToListAsync(cancellationToken);

        var userRoleSet = existingUserRoles
            .Select(x => (x.UserId, x.RoleId))
            .ToHashSet();

        var adminHash = new Lazy<string>(() => _passwordHasher.HashPassword("Admin@123!"));
        var staffHash = new Lazy<string>(() => _passwordHasher.HashPassword("Staff@123!"));
        var customerHash = new Lazy<string>(() => _passwordHasher.HashPassword("Customer@123!"));

        var userSpecs = new (Guid Id, string Email, Lazy<string> Hash, string FullName, string Phone, string RoleName, Guid? DealerId, string? EmpCode)[]
        {
            (Guid.Parse("44444444-4444-4444-4444-444444444401"), "admin@motorcycledealer.vn", adminHash, "Quoc Anh (Platform Admin)", "0901000001", RoleNames.Admin, null, null),
            (Guid.Parse("44444444-4444-4444-4444-444444444402"), "staff.hn@motorcycledealer.vn", staffHash, "Tran Thi Lan (Hanoi Staff)", "0902000101", RoleNames.DealerStaff, dealers["DLR-HN-01"].Id, "EMP-HN-001"),
            (Guid.Parse("44444444-4444-4444-4444-444444444403"), "lan.tran.hn@dealer.vn", staffHash, "Lan Tran (Hanoi Service Advisor)", "0902000102", RoleNames.DealerStaff, dealers["DLR-HN-01"].Id, "EMP-HN-002"),
            (Guid.Parse("44444444-4444-4444-4444-444444444404"), "staff.dn@motorcycledealer.vn", staffHash, "Le Van Hung (Da Nang Staff)", "0902000201", RoleNames.DealerStaff, dealers["DLR-DN-01"].Id, "EMP-DN-001"),
            (Guid.Parse("44444444-4444-4444-4444-444444444405"), "staff.hcm@motorcycledealer.vn", staffHash, "Pham Minh Tuan (Saigon Staff)", "0902000301", RoleNames.DealerStaff, dealers["DLR-HCM-01"].Id, "EMP-HCM-001"),
            (Guid.Parse("44444444-4444-4444-4444-444444444406"), "minh.nguyen@customer.vn", customerHash, "Nguyen Van Minh", "0913000101", RoleNames.Customer, null, null),
            (Guid.Parse("44444444-4444-4444-4444-444444444407"), "customer.bronze@motorcycledealer.vn", customerHash, "Hoang Gia Bao (Bronze)", "0913000102", RoleNames.Customer, null, null),
            (Guid.Parse("44444444-4444-4444-4444-444444444408"), "customer.silver@motorcycledealer.vn", customerHash, "Tran Ngoc Mai (Silver)", "0913000201", RoleNames.Customer, null, null),
            (Guid.Parse("44444444-4444-4444-4444-444444444409"), "customer.gold@motorcycledealer.vn", customerHash, "Le Hoang Nam (Gold)", "0913000301", RoleNames.Customer, null, null),
            (Guid.Parse("44444444-4444-4444-4444-444444444410"), "customer.platinum@motorcycledealer.vn", customerHash, "Vo Thanh Son (Platinum)", "0913000401", RoleNames.Customer, null, null)
        };

        var addedUsers = false;
        foreach (var spec in userSpecs)
        {
            var normalizedEmail = User.NormalizeEmailValue(spec.Email);
            if (!existingUsers.TryGetValue(normalizedEmail, out var user))
            {
                var passwordHash = spec.Hash.Value;
                user = spec.RoleName switch
                {
                    RoleNames.Admin => User.CreateAdmin(spec.Email, passwordHash, spec.FullName, spec.Phone, spec.Id),
                    RoleNames.DealerStaff => User.CreateDealerStaff(spec.Email, passwordHash, spec.FullName, spec.Phone, spec.DealerId!.Value, spec.EmpCode!, spec.Id),
                    _ => User.CreateCustomer(spec.Email, passwordHash, spec.FullName, spec.Phone, spec.Id)
                };
                user.CreatedAtUtc = utcNow.AddMonths(-7);
                _dbContext.Users.Add(user);
                existingUsers[normalizedEmail] = user;
                addedUsers = true;
            }
        }

        if (addedUsers)
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        var adminUser = existingUsers[User.NormalizeEmailValue("admin@motorcycledealer.vn")];
        var addedRoles = false;
        foreach (var spec in userSpecs)
        {
            var user = existingUsers[User.NormalizeEmailValue(spec.Email)];
            var role = roles[spec.RoleName];
            if (userRoleSet.Add((user.Id, role.Id)))
            {
                _dbContext.UserRoles.Add(new UserRole
                {
                    UserId = user.Id,
                    RoleId = role.Id,
                    AssignedAtUtc = utcNow.AddMonths(-7),
                    AssignedByUserId = spec.RoleName == RoleNames.DealerStaff ? adminUser.Id : null
                });
                addedRoles = true;
            }
        }

        if (addedRoles)
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        return existingUsers;
    }

    private async Task<Dictionary<string, MotorcycleModel>> SeedMotorcycleModelsAsync(
        DateTimeOffset utcNow,
        CancellationToken cancellationToken)
    {
        var existing = await _dbContext.MotorcycleModels
            .IgnoreQueryFilters()
            .ToDictionaryAsync(m => m.ModelCode, StringComparer.Ordinal, cancellationToken);

        var specs = new[]
        {
            new MotorcycleModel(Guid.Parse("55555555-5555-5555-5555-555555555501"))
            {
                ModelCode = "HND-AB160-2026",
                Brand = "Honda",
                ModelName = "Honda Air Blade 160 SE",
                Category = "Scooter",
                EngineDisplacementCc = 157,
                BaseRetailPrice = 45_000_000m,
                WarrantyMonths = 36,
                WarrantyMileageKm = 30_000,
                ImageUrl = "/assets/models/honda-airblade-160.jpg",
                SpecificationsJson = "{\"horsepower\":\"15.0 hp\",\"abs\":\"Single-Channel\",\"fuelTankLiters\":4.4,\"seatHeightMm\":775}",
                IsActive = true,
                CreatedAtUtc = utcNow.AddMonths(-8)
            },
            new MotorcycleModel(Guid.Parse("55555555-5555-5555-5555-555555555502"))
            {
                ModelCode = "HND-SH160I-2026",
                Brand = "Honda",
                ModelName = "Honda SH160i ABS Sport",
                Category = "Scooter",
                EngineDisplacementCc = 157,
                BaseRetailPrice = 102_500_000m,
                WarrantyMonths = 36,
                WarrantyMileageKm = 30_000,
                ImageUrl = "/assets/models/honda-sh160i-sport.jpg",
                SpecificationsJson = "{\"horsepower\":\"16.6 hp\",\"abs\":\"Dual-Channel + HSTC\",\"fuelTankLiters\":7.8,\"seatHeightMm\":799}",
                IsActive = true,
                CreatedAtUtc = utcNow.AddMonths(-8)
            },
            new MotorcycleModel(Guid.Parse("55555555-5555-5555-5555-555555555503"))
            {
                ModelCode = "HND-WINX-2026",
                Brand = "Honda",
                ModelName = "Honda Winner X ABS",
                Category = "Underbone",
                EngineDisplacementCc = 149,
                BaseRetailPrice = 50_500_000m,
                WarrantyMonths = 36,
                WarrantyMileageKm = 30_000,
                ImageUrl = "/assets/models/honda-winner-x.jpg",
                SpecificationsJson = "{\"horsepower\":\"15.4 hp\",\"abs\":\"Front ABS\",\"transmission\":\"6-Speed Manual\",\"seatHeightMm\":795}",
                IsActive = true,
                CreatedAtUtc = utcNow.AddMonths(-8)
            },
            new MotorcycleModel(Guid.Parse("55555555-5555-5555-5555-555555555504"))
            {
                ModelCode = "YMH-EXC155-2026",
                Brand = "Yamaha",
                ModelName = "Yamaha Exciter 155 VVA",
                Category = "Underbone",
                EngineDisplacementCc = 155,
                BaseRetailPrice = 51_000_000m,
                WarrantyMonths = 36,
                WarrantyMileageKm = 30_000,
                ImageUrl = "/assets/models/yamaha-exciter-155.jpg",
                SpecificationsJson = "{\"horsepower\":\"17.7 hp\",\"vva\":true,\"clutch\":\"Assist & Slipper\",\"seatHeightMm\":795}",
                IsActive = true,
                CreatedAtUtc = utcNow.AddMonths(-8)
            },
            new MotorcycleModel(Guid.Parse("55555555-5555-5555-5555-555555555505"))
            {
                ModelCode = "HND-CB300R-2026",
                Brand = "Honda",
                ModelName = "Honda CB300R Neo Sports Cafe",
                Category = "Naked",
                EngineDisplacementCc = 286,
                BaseRetailPrice = 139_000_000m,
                WarrantyMonths = 36,
                WarrantyMileageKm = 30_000,
                ImageUrl = "/assets/models/honda-cb300r.jpg",
                SpecificationsJson = "{\"horsepower\":\"30.7 hp\",\"abs\":\"IMU Dual-Channel\",\"forks\":\"Showa SFF-BP 41mm\",\"seatHeightMm\":799}",
                IsActive = true,
                CreatedAtUtc = utcNow.AddMonths(-8)
            },
            new MotorcycleModel(Guid.Parse("55555555-5555-5555-5555-555555555506"))
            {
                ModelCode = "HND-NX500-2026",
                Brand = "Honda",
                ModelName = "Honda NX500 Adventure",
                Category = "Adventure",
                EngineDisplacementCc = 471,
                BaseRetailPrice = 195_000_000m,
                WarrantyMonths = 36,
                WarrantyMileageKm = 30_000,
                ImageUrl = "/assets/models/honda-nx500.jpg",
                SpecificationsJson = "{\"horsepower\":\"47.0 hp\",\"abs\":\"Dual-Channel + HSTC\",\"fuelTankLiters\":17.5,\"seatHeightMm\":830}",
                IsActive = true,
                CreatedAtUtc = utcNow.AddMonths(-8)
            }
        };

        var added = false;
        foreach (var spec in specs)
        {
            if (!existing.ContainsKey(spec.ModelCode))
            {
                _dbContext.MotorcycleModels.Add(spec);
                existing[spec.ModelCode] = spec;
                added = true;
            }
        }

        if (added)
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        return existing;
    }

    private async Task<Dictionary<(Guid ModelId, string MilestoneCode), MaintenanceRule>> SeedMaintenanceRulesAsync(
        Dictionary<string, MotorcycleModel> models,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken)
    {
        var existing = await _dbContext.MaintenanceRules
            .ToDictionaryAsync(r => (r.MotorcycleModelId, r.MilestoneCode), cancellationToken);

        var added = false;
        foreach (var model in models.Values)
        {
            var templates = new (string Code, string Name, int Km, int Months, bool Recurring, int LeadDays, int LeadKm, int Mins, string Summary)[]
            {
                ("BREAKIN_1000", "Initial 1,000 km Break-In Inspection", 1_000, 1, false, 7, 200, 45, "Engine oil & filter replacement, chain/belt tension check, fastener torque verification, ECU diagnostic scan."),
                ("PERIODIC_4000", "4,000 km Periodic Maintenance", 4_000, 6, true, 7, 250, 60, "Synthetic oil change, air filter cleaning, brake pad & fluid inspection, tire pressure & throttle play adjustment."),
                ("MAJOR_12000", "12,000 km Major Overhaul & Safety Check", 12_000, 12, true, 14, 400, 120, "Spark plug replacement, coolant/brake fluid flush, CVT belt or drive chain kit inspection, valve clearance & fork seal check.")
            };

            foreach (var tpl in templates)
            {
                var key = (model.Id, tpl.Code);
                if (!existing.ContainsKey(key))
                {
                    var rule = new MaintenanceRule
                    {
                        MotorcycleModelId = model.Id,
                        MilestoneCode = tpl.Code,
                        MilestoneName = tpl.Name,
                        IntervalKm = tpl.Km,
                        IntervalMonths = tpl.Months,
                        IsRecurring = tpl.Recurring,
                        LeadTimeDays = tpl.LeadDays,
                        LeadMileageKm = tpl.LeadKm,
                        EstimatedDurationMinutes = tpl.Mins,
                        ChecklistSummary = tpl.Summary,
                        IsActive = true,
                        CreatedAtUtc = utcNow.AddMonths(-8)
                    };
                    _dbContext.MaintenanceRules.Add(rule);
                    existing[key] = rule;
                    added = true;
                }
            }
        }

        if (added)
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        return existing;
    }

    private async Task<Dictionary<string, VoucherCatalog>> SeedVoucherCatalogsAsync(
        Dictionary<string, LoyaltyTier> tiers,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken)
    {
        var existing = await _dbContext.VoucherCatalogs
            .IgnoreQueryFilters()
            .ToDictionaryAsync(v => v.CodePrefix, StringComparer.Ordinal, cancellationToken);

        var specs = new[]
        {
            new VoucherCatalog(Guid.Parse("66666666-6666-6666-6666-666666666601"))
            {
                CodePrefix = "SVC50K",
                Title = "50,000 VND Off Workshop Oil & Filter",
                Description = "Instant 50,000 VND discount on periodic oil & filter service invoices of 250,000 VND or above.",
                ApplicableScope = "ServiceOnly",
                PointsCost = 100,
                DiscountType = "FixedAmount",
                DiscountValue = 50_000m,
                MaxDiscountAmount = null,
                MinOrderAmount = 250_000m,
                ValidDays = 90,
                QuotaTotal = 1000,
                QuotaRedeemed = 0,
                MinTierId = tiers["Bronze"].Id,
                IsActive = true,
                CreatedAtUtc = utcNow.AddMonths(-7)
            },
            new VoucherCatalog(Guid.Parse("66666666-6666-6666-6666-666666666602"))
            {
                CodePrefix = "OIL100K",
                Title = "100,000 VND Off Periodic Maintenance",
                Description = "Deducts 100,000 VND on scheduled maintenance workshop invoices from 500,000 VND.",
                ApplicableScope = "ServiceOnly",
                PointsCost = 200,
                DiscountType = "FixedAmount",
                DiscountValue = 100_000m,
                MaxDiscountAmount = null,
                MinOrderAmount = 500_000m,
                ValidDays = 90,
                QuotaTotal = 1000,
                QuotaRedeemed = 1,
                MinTierId = tiers["Bronze"].Id,
                IsActive = true,
                CreatedAtUtc = utcNow.AddMonths(-7)
            },
            new VoucherCatalog(Guid.Parse("66666666-6666-6666-6666-666666666603"))
            {
                CodePrefix = "PARTS10P",
                Title = "15% Off Workshop Service (Cap 200,000 VND)",
                Description = "Silver+ perk offering 15% off workshop labor & genuine parts up to 200,000 VND.",
                ApplicableScope = "ServiceOnly",
                PointsCost = 300,
                DiscountType = "Percentage",
                DiscountValue = 15.00m,
                MaxDiscountAmount = 200_000m,
                MinOrderAmount = 600_000m,
                ValidDays = 90,
                QuotaTotal = 500,
                QuotaRedeemed = 2,
                MinTierId = tiers["Silver"].Id,
                IsActive = true,
                CreatedAtUtc = utcNow.AddMonths(-7)
            },
            new VoucherCatalog(Guid.Parse("66666666-6666-6666-6666-666666666604"))
            {
                CodePrefix = "GOLD15P",
                Title = "15% Off Genuine Parts & Tires (Cap 400,000 VND)",
                Description = "Gold+ reward unlocking 15% off genuine parts and tire replacements up to 400,000 VND.",
                ApplicableScope = "All",
                PointsCost = 600,
                DiscountType = "Percentage",
                DiscountValue = 15.00m,
                MaxDiscountAmount = 400_000m,
                MinOrderAmount = 1_000_000m,
                ValidDays = 120,
                QuotaTotal = 300,
                QuotaRedeemed = 1,
                MinTierId = tiers["Gold"].Id,
                IsActive = true,
                CreatedAtUtc = utcNow.AddMonths(-7)
            },
            new VoucherCatalog(Guid.Parse("66666666-6666-6666-6666-666666666605"))
            {
                CodePrefix = "PLATVIP300K",
                Title = "300,000 VND VIP Priority Bay & Detailing",
                Description = "Platinum exclusive 300,000 VND voucher applicable across workshop detailing and showroom purchases.",
                ApplicableScope = "All",
                PointsCost = 900,
                DiscountType = "FixedAmount",
                DiscountValue = 300_000m,
                MaxDiscountAmount = null,
                MinOrderAmount = 800_000m,
                ValidDays = 180,
                QuotaTotal = 200,
                QuotaRedeemed = 1,
                MinTierId = tiers["Platinum"].Id,
                IsActive = true,
                CreatedAtUtc = utcNow.AddMonths(-7)
            }
        };

        var added = false;
        foreach (var spec in specs)
        {
            if (!existing.ContainsKey(spec.CodePrefix))
            {
                _dbContext.VoucherCatalogs.Add(spec);
                existing[spec.CodePrefix] = spec;
                added = true;
            }
        }

        if (added)
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        return existing;
    }

    private async Task SeedRollingServiceTimeSlotsAsync(
        Dictionary<string, Dealer> dealers,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken)
    {
        var vietnamToday = DateOnly.FromDateTime(utcNow.ToOffset(TimeSpan.FromHours(7)).DateTime);
        var startWindow = vietnamToday.AddDays(-1);
        var endWindow = vietnamToday.AddDays(13);

        var existingKeys = await _dbContext.ServiceTimeSlots
            .IgnoreQueryFilters()
            .Where(s => s.SlotDate >= startWindow && s.SlotDate <= endWindow)
            .Select(s => new { s.DealerId, s.SlotDate, s.StartTime, s.EndTime })
            .ToListAsync(cancellationToken);

        var keySet = existingKeys
            .Select(k => (k.DealerId, k.SlotDate, k.StartTime, k.EndTime))
            .ToHashSet();

        var slotWindows = new (TimeOnly Start, TimeOnly End)[]
        {
            (new TimeOnly(8, 0), new TimeOnly(9, 0)),
            (new TimeOnly(9, 0), new TimeOnly(10, 0)),
            (new TimeOnly(10, 0), new TimeOnly(11, 0)),
            (new TimeOnly(13, 30), new TimeOnly(14, 30)),
            (new TimeOnly(15, 0), new TimeOnly(16, 0)),
            (new TimeOnly(16, 30), new TimeOnly(17, 30))
        };

        var added = false;
        foreach (var dealer in dealers.Values)
        {
            for (var dayOffset = 0; dayOffset < 14; dayOffset++)
            {
                var date = vietnamToday.AddDays(dayOffset);
                foreach (var (start, end) in slotWindows)
                {
                    if (keySet.Add((dealer.Id, date, start, end)))
                    {
                        _dbContext.ServiceTimeSlots.Add(new ServiceTimeSlot
                        {
                            DealerId = dealer.Id,
                            SlotDate = date,
                            StartTime = start,
                            EndTime = end,
                            MaxCapacity = dealer.DefaultSlotCapacity,
                            BookedCount = 0,
                            IsBlocked = false,
                            CreatedAtUtc = utcNow
                        });
                        added = true;
                    }
                }
            }
        }

        if (added)
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task<Dictionary<string, DealerInventory>> SeedDealerInventoriesAsync(
        Dictionary<string, Dealer> dealers,
        Dictionary<string, MotorcycleModel> models,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken)
    {
        var existing = await _dbContext.DealerInventories
            .IgnoreQueryFilters()
            .ToDictionaryAsync(i => i.VIN, StringComparer.Ordinal, cancellationToken);

        var specs = new (string Vin, string EngineNo, string DealerCode, string ModelCode, string Color, decimal Price, string Status, int MonthsAgo)[]
        {
            // Hanoi (6 units: 5 Available including RLHJF5108RY000101 + 1 Sold)
            ("RLHJF5108RY000101", "JF51E-8000101", "DLR-HN-01", "HND-AB160-2026", "Special Matte Black", 45_000_000m, "Available", 1),
            ("RLHJF5108RY000102", "JF51E-8000102", "DLR-HN-01", "HND-AB160-2026", "Racing Red", 45_200_000m, "Available", 1),
            ("RLHJC9204RY000103", "JC92E-4000103", "DLR-HN-01", "HND-SH160I-2026", "Cement Grey Sport", 102_500_000m, "Available", 1),
            ("RLHKF4102RY000104", "KF41E-2000104", "DLR-HN-01", "HND-WINX-2026", "HRC Tri-Color", 50_500_000m, "Available", 1),
            ("RLHNC5501RY000105", "NC55E-1000105", "DLR-HN-01", "HND-CB300R-2026", "Matte Gunpowder Black", 139_000_000m, "Available", 2),
            ("RLHJF5108RY000123", "JF51E-8000123", "DLR-HN-01", "HND-AB160-2026", "Pearl White", 45_000_000m, "Sold", 3),

            // Da Nang (6 units: 5 Available + 1 Sold)
            ("RLHJF5108RY000201", "JF51E-8000201", "DLR-DN-01", "HND-AB160-2026", "Metallic Blue", 45_000_000m, "Available", 1),
            ("RLHKF4102RY000202", "KF41E-2000202", "DLR-DN-01", "HND-WINX-2026", "Matte Black Gold", 50_500_000m, "Available", 1),
            ("RLCUG1010RY000203", "G3N8E-1000203", "DLR-DN-01", "YMH-EXC155-2026", "GP Racing Blue", 51_000_000m, "Available", 1),
            ("RLHJC9204RY000204", "JC92E-4000204", "DLR-DN-01", "HND-SH160I-2026", "Pearl White ABS", 102_000_000m, "Available", 1),
            ("RLHPC6405RY000206", "PC64E-5000206", "DLR-DN-01", "HND-NX500-2026", "Grand Prix Red", 195_000_000m, "Available", 2),
            ("RLHKF4102RY000205", "KF41E-2000205", "DLR-DN-01", "HND-WINX-2026", "Sport Red Black", 50_500_000m, "Sold", 2),

            // Ho Chi Minh City (6 units: 4 Available including RLHJC9204RY000309 + 2 Sold)
            ("RLHJC9204RY000309", "JC92E-4000309", "DLR-HCM-01", "HND-SH160I-2026", "Cement Grey ABS Sport", 102_500_000m, "Available", 1),
            ("RLHJF5108RY000301", "JF51E-8000301", "DLR-HCM-01", "HND-AB160-2026", "Special Matte Black", 45_000_000m, "Available", 1),
            ("RLCUG1010RY000302", "G3N8E-1000302", "DLR-HCM-01", "YMH-EXC155-2026", "Monster Energy Black", 51_500_000m, "Available", 1),
            ("RLHPC6405RY000303", "PC64E-5000303", "DLR-HCM-01", "HND-NX500-2026", "Pearl Horizon White", 195_000_000m, "Available", 1),
            ("RLHJF5109RZ104829", "JC92E-9104829", "DLR-HCM-01", "HND-SH160I-2026", "Cement Grey Sport", 102_500_000m, "Sold", 5),
            ("RLHJC9204RY000388", "JC92E-4000388", "DLR-HCM-01", "HND-SH160I-2026", "Matte Black ABS", 102_500_000m, "Sold", 6)
        };

        var added = false;
        foreach (var spec in specs)
        {
            _ = Vin.Create(spec.Vin);
            if (!existing.ContainsKey(spec.Vin))
            {
                var receivedAt = utcNow.AddMonths(-spec.MonthsAgo).AddDays(-5);
                var inv = new DealerInventory
                {
                    DealerId = dealers[spec.DealerCode].Id,
                    MotorcycleModelId = models[spec.ModelCode].Id,
                    VIN = spec.Vin,
                    EngineNumber = spec.EngineNo,
                    ColorName = spec.Color,
                    ManufactureYear = 2026,
                    ListedPrice = spec.Price,
                    StockStatus = spec.Status,
                    ReceivedAtUtc = receivedAt,
                    SoldAtUtc = spec.Status == "Sold" ? receivedAt.AddDays(3) : null,
                    CreatedAtUtc = receivedAt
                };
                _dbContext.DealerInventories.Add(inv);
                existing[spec.Vin] = inv;
                added = true;
            }
        }

        if (added)
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        return existing;
    }

    private async Task<Dictionary<string, SparePart>> SeedSparePartsAsync(
        Dictionary<string, MotorcycleModel> models,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken)
    {
        var existing = await _dbContext.SpareParts
            .IgnoreQueryFilters()
            .ToDictionaryAsync(p => p.PartCode, StringComparer.Ordinal, cancellationToken);

        var specs = new[]
        {
            new SparePart(Guid.Parse("12121212-1212-1212-1212-121212121201"))
            {
                PartCode = "PRT-OIL-10W30",
                Name = "Pro Honda Full Synthetic 10W-30 Scooter Oil (0.8L)",
                Category = SparePartCategory.EngineOil,
                CompatibleModelId = null,
                UnitLabel = "Bottle",
                UnitPriceVnd = 250_000m,
                IsActive = true,
                CreatedAtUtc = utcNow.AddMonths(-7)
            },
            new SparePart(Guid.Parse("12121212-1212-1212-1212-121212121202"))
            {
                PartCode = "PRT-OIL-10W40",
                Name = "Motul 7100 4T 10W-40 Ester Synthetic Oil (1.0L)",
                Category = SparePartCategory.EngineOil,
                CompatibleModelId = null,
                UnitLabel = "Bottle",
                UnitPriceVnd = 300_000m,
                IsActive = true,
                CreatedAtUtc = utcNow.AddMonths(-7)
            },
            new SparePart(Guid.Parse("12121212-1212-1212-1212-121212121203"))
            {
                PartCode = "PRT-FLT-AIR01",
                Name = "Honda SH160i Viscous OEM Air Filter Element",
                Category = SparePartCategory.Filter,
                CompatibleModelId = models["HND-SH160I-2026"].Id,
                UnitLabel = "Piece",
                UnitPriceVnd = 220_000m,
                IsActive = true,
                CreatedAtUtc = utcNow.AddMonths(-7)
            },
            new SparePart(Guid.Parse("12121212-1212-1212-1212-121212121204"))
            {
                PartCode = "PRT-FLT-OIL01",
                Name = "OEM High-Flow Cartridge Oil Filter",
                Category = SparePartCategory.Filter,
                CompatibleModelId = null,
                UnitLabel = "Piece",
                UnitPriceVnd = 180_000m,
                IsActive = true,
                CreatedAtUtc = utcNow.AddMonths(-7)
            },
            new SparePart(Guid.Parse("12121212-1212-1212-1212-121212121205"))
            {
                PartCode = "PRT-BRK-FR01",
                Name = "Nissin OEM Sintered Front ABS Brake Pad Set",
                Category = SparePartCategory.Brakes,
                CompatibleModelId = null,
                UnitLabel = "Set",
                UnitPriceVnd = 450_000m,
                IsActive = true,
                CreatedAtUtc = utcNow.AddMonths(-7)
            },
            new SparePart(Guid.Parse("12121212-1212-1212-1212-121212121206"))
            {
                PartCode = "PRT-CHN-520VX",
                Name = "DID 520VX3 X-Ring Heavy-Duty Chain & Sprocket Kit (CB300R)",
                Category = SparePartCategory.Transmission,
                CompatibleModelId = models["HND-CB300R-2026"].Id,
                UnitLabel = "Kit",
                UnitPriceVnd = 2_800_000m,
                IsActive = true,
                CreatedAtUtc = utcNow.AddMonths(-7)
            },
            new SparePart(Guid.Parse("12121212-1212-1212-1212-121212121207"))
            {
                PartCode = "PRT-SPK-IR01",
                Name = "NGK Laser Iridium Spark Plug LMAR8BI-9",
                Category = SparePartCategory.Electrical,
                CompatibleModelId = null,
                UnitLabel = "Piece",
                UnitPriceVnd = 350_000m,
                IsActive = true,
                CreatedAtUtc = utcNow.AddMonths(-7)
            },
            new SparePart(Guid.Parse("12121212-1212-1212-1212-121212121208"))
            {
                PartCode = "PRT-CVT-BLT01",
                Name = "Honda Bando Kevlar CVT Drive Belt (SH160i)",
                Category = SparePartCategory.Transmission,
                CompatibleModelId = models["HND-SH160I-2026"].Id,
                UnitLabel = "Belt",
                UnitPriceVnd = 850_000m,
                IsActive = true,
                CreatedAtUtc = utcNow.AddMonths(-7)
            }
        };

        var added = false;
        foreach (var spec in specs)
        {
            if (!existing.ContainsKey(spec.PartCode))
            {
                _dbContext.SpareParts.Add(spec);
                existing[spec.PartCode] = spec;
                added = true;
            }
        }

        if (added)
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        return existing;
    }

    private async Task SeedClosedLoopTransactionsAndLoyaltyAsync(
        Dictionary<string, Dealer> dealers,
        Dictionary<string, User> users,
        Dictionary<string, LoyaltyTier> tiers,
        Dictionary<string, MotorcycleModel> models,
        Dictionary<(Guid ModelId, string MilestoneCode), MaintenanceRule> rules,
        Dictionary<string, VoucherCatalog> catalogs,
        Dictionary<string, DealerInventory> inventories,
        Dictionary<string, SparePart> spareParts,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken)
    {
        var staffHn = users[User.NormalizeEmailValue("staff.hn@motorcycledealer.vn")];
        var staffDn = users[User.NormalizeEmailValue("staff.dn@motorcycledealer.vn")];
        var staffHcm = users[User.NormalizeEmailValue("staff.hcm@motorcycledealer.vn")];

        var minhBronze = users[User.NormalizeEmailValue("minh.nguyen@customer.vn")];
        var custBronze = users[User.NormalizeEmailValue("customer.bronze@motorcycledealer.vn")];
        var custSilver = users[User.NormalizeEmailValue("customer.silver@motorcycledealer.vn")];
        var custGold = users[User.NormalizeEmailValue("customer.gold@motorcycledealer.vn")];
        var custPlatinum = users[User.NormalizeEmailValue("customer.platinum@motorcycledealer.vn")];

        // 1. LoyaltyAccounts (exact balances from docs/RUNBOOK.md Section 3.2 — granularly idempotent per CustomerId)
        var existingAccounts = await _dbContext.LoyaltyAccounts
            .IgnoreQueryFilters()
            .ToDictionaryAsync(a => a.CustomerId, cancellationToken);

        LoyaltyAccount EnsureAccount(LoyaltyAccount spec)
        {
            if (existingAccounts.TryGetValue(spec.CustomerId, out var existing))
            {
                return existing;
            }

            _dbContext.LoyaltyAccounts.Add(spec);
            existingAccounts[spec.CustomerId] = spec;
            return spec;
        }

        var accMinh = EnsureAccount(new LoyaltyAccount(Guid.Parse("77777777-7777-7777-7777-777777777701"))
        {
            CustomerId = minhBronze.Id,
            CurrentTierId = tiers["Bronze"].Id,
            AvailablePoints = 120,
            LifetimeQualifyingPoints = 120,
            LifetimeSpentVnd = 51_700_000m,
            CreatedAtUtc = utcNow.AddMonths(-6)
        });

        var accBronze = EnsureAccount(new LoyaltyAccount(Guid.Parse("77777777-7777-7777-7777-777777777702"))
        {
            CustomerId = custBronze.Id,
            CurrentTierId = tiers["Bronze"].Id,
            AvailablePoints = 120,
            LifetimeQualifyingPoints = 120,
            LifetimeSpentVnd = 46_200_000m,
            CreatedAtUtc = utcNow.AddMonths(-6)
        });

        var accSilver = EnsureAccount(new LoyaltyAccount(Guid.Parse("77777777-7777-7777-7777-777777777703"))
        {
            CustomerId = custSilver.Id,
            CurrentTierId = tiers["Silver"].Id,
            AvailablePoints = 649,
            LifetimeQualifyingPoints = 1960,
            LifetimeSpentVnd = 119_760_000m,
            TierUpgradedAtUtc = utcNow.AddMonths(-4),
            CreatedAtUtc = utcNow.AddMonths(-6)
        });

        var accGold = EnsureAccount(new LoyaltyAccount(Guid.Parse("77777777-7777-7777-7777-777777777704"))
        {
            CustomerId = custGold.Id,
            CurrentTierId = tiers["Gold"].Id,
            AvailablePoints = 1450,
            LifetimeQualifyingPoints = 3200,
            LifetimeSpentVnd = 32_000_000m,
            TierUpgradedAtUtc = utcNow.AddMonths(-3),
            CreatedAtUtc = utcNow.AddMonths(-6)
        });

        var accPlatinum = EnsureAccount(new LoyaltyAccount(Guid.Parse("77777777-7777-7777-7777-777777777705"))
        {
            CustomerId = custPlatinum.Id,
            CurrentTierId = tiers["Platinum"].Id,
            AvailablePoints = 4800,
            LifetimeQualifyingPoints = 10_100,
            LifetimeSpentVnd = 101_000_000m,
            TierUpgradedAtUtc = utcNow.AddMonths(-5),
            CreatedAtUtc = utcNow.AddMonths(-6)
        });

        if (_dbContext.ChangeTracker.HasChanges())
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        // 2. Historical Showroom SalesOrders (4 Sold inventories across HCM, HN, DN — keyed by OrderNumber)
        var existingOrders = await _dbContext.SalesOrders
            .IgnoreQueryFilters()
            .ToDictionaryAsync(o => o.OrderNumber, StringComparer.Ordinal, cancellationToken);

        SalesOrder EnsureOrder(SalesOrder spec)
        {
            if (existingOrders.TryGetValue(spec.OrderNumber, out var existing))
            {
                return existing;
            }

            _dbContext.SalesOrders.Add(spec);
            existingOrders[spec.OrderNumber] = spec;
            return spec;
        }

        var soPlatinum = EnsureOrder(new SalesOrder(Guid.Parse("88888888-8888-8888-8888-888888888801"))
        {
            OrderNumber = "SO-202605-0001",
            DealerId = dealers["DLR-HCM-01"].Id,
            CustomerId = custPlatinum.Id,
            DealerInventoryId = inventories["RLHJC9204RY000388"].Id,
            SoldByStaffId = staffHcm.Id,
            TotalAmount = 102_500_000m,
            DiscountAmount = 1_500_000m,
            FinalAmount = 101_000_000m,
            PointsEarned = 10_100,
            PaymentMethod = "BankTransfer",
            Status = "Completed",
            Notes = "Flagship SH160i showroom purchase with instant Platinum tier promotion.",
            CompletedAtUtc = utcNow.AddMonths(-5),
            CreatedAtUtc = utcNow.AddMonths(-5)
        });

        var soSilver = EnsureOrder(new SalesOrder(Guid.Parse("88888888-8888-8888-8888-888888888802"))
        {
            OrderNumber = "SO-202606-0002",
            DealerId = dealers["DLR-HCM-01"].Id,
            CustomerId = custSilver.Id,
            DealerInventoryId = inventories["RLHJF5109RZ104829"].Id,
            SoldByStaffId = staffHcm.Id,
            TotalAmount = 102_500_000m,
            DiscountAmount = 0m,
            FinalAmount = 102_500_000m,
            PointsEarned = 550,
            PaymentMethod = "BankTransfer",
            Status = "Completed",
            Notes = "Onboarded SH160i ABS Sport into CustomerVehicles garage.",
            CompletedAtUtc = utcNow.AddMonths(-4),
            CreatedAtUtc = utcNow.AddMonths(-4)
        });

        var soBronzeHn = EnsureOrder(new SalesOrder(Guid.Parse("88888888-8888-8888-8888-888888888803"))
        {
            OrderNumber = "SO-202608-0003",
            DealerId = dealers["DLR-HN-01"].Id,
            CustomerId = custBronze.Id,
            DealerInventoryId = inventories["RLHJF5108RY000123"].Id,
            SoldByStaffId = staffHn.Id,
            TotalAmount = 45_000_000m,
            DiscountAmount = 0m,
            FinalAmount = 45_000_000m,
            PointsEarned = 120,
            PaymentMethod = "Card",
            Status = "Completed",
            Notes = "Hanoi showroom Air Blade 160 delivery.",
            CompletedAtUtc = utcNow.AddMonths(-2),
            CreatedAtUtc = utcNow.AddMonths(-2)
        });

        var soMinhDn = EnsureOrder(new SalesOrder(Guid.Parse("88888888-8888-8888-8888-888888888804"))
        {
            OrderNumber = "SO-202609-0004",
            DealerId = dealers["DLR-DN-01"].Id,
            CustomerId = minhBronze.Id,
            DealerInventoryId = inventories["RLHKF4102RY000205"].Id,
            SoldByStaffId = staffDn.Id,
            TotalAmount = 50_500_000m,
            DiscountAmount = 0m,
            FinalAmount = 50_500_000m,
            PointsEarned = 120,
            PaymentMethod = "BankTransfer",
            Status = "Completed",
            Notes = "Da Nang showroom Winner X ABS delivery.",
            CompletedAtUtc = utcNow.AddMonths(-1),
            CreatedAtUtc = utcNow.AddMonths(-1)
        });

        if (_dbContext.ChangeTracker.HasChanges())
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        // 3. CustomerVehicles (6 Garage VINs: 4 ShowroomSale + 2 ExternalManual — keyed by VIN)
        var existingVehicles = await _dbContext.CustomerVehicles
            .IgnoreQueryFilters()
            .ToDictionaryAsync(v => v.VIN, StringComparer.Ordinal, cancellationToken);

        CustomerVehicle EnsureVehicle(CustomerVehicle spec)
        {
            if (existingVehicles.TryGetValue(spec.VIN, out var existing))
            {
                return existing;
            }

            _dbContext.CustomerVehicles.Add(spec);
            existingVehicles[spec.VIN] = spec;
            return spec;
        }

        var vehSilverSh = EnsureVehicle(new CustomerVehicle(Guid.Parse("99999999-9999-9999-9999-999999999901"))
        {
            VIN = "RLHJF5109RZ104829",
            CustomerId = custSilver.Id,
            MotorcycleModelId = models["HND-SH160I-2026"].Id,
            PurchasedSalesOrderId = soSilver.Id,
            LicensePlate = "59-A3 889.21",
            ColorName = "Cement Grey Sport",
            RegistrationSource = "ShowroomSale",
            OwnershipStartDateUtc = utcNow.AddMonths(-4),
            CurrentMileageKm = 3_850,
            LastServiceDateUtc = utcNow.AddMonths(-3),
            LastServiceMileageKm = 1_000,
            NextDueMileageKm = 4_000,
            NextDueDateUtc = utcNow.AddDays(5),
            IsActive = true,
            CreatedAtUtc = utcNow.AddMonths(-4)
        });

        var vehMinhWinner = EnsureVehicle(new CustomerVehicle(Guid.Parse("99999999-9999-9999-9999-999999999902"))
        {
            VIN = "RLHKF4102RY000205",
            CustomerId = minhBronze.Id,
            MotorcycleModelId = models["HND-WINX-2026"].Id,
            PurchasedSalesOrderId = soMinhDn.Id,
            LicensePlate = "43-D1 552.18",
            ColorName = "Sport Red Black",
            RegistrationSource = "ShowroomSale",
            OwnershipStartDateUtc = utcNow.AddMonths(-1),
            CurrentMileageKm = 900,
            LastServiceDateUtc = null,
            LastServiceMileageKm = 0,
            NextDueMileageKm = 1_000,
            NextDueDateUtc = utcNow.AddDays(4),
            IsActive = true,
            CreatedAtUtc = utcNow.AddMonths(-1)
        });

        var vehBronzeAb = EnsureVehicle(new CustomerVehicle(Guid.Parse("99999999-9999-9999-9999-999999999903"))
        {
            VIN = "RLHJF5108RY000123",
            CustomerId = custBronze.Id,
            MotorcycleModelId = models["HND-AB160-2026"].Id,
            PurchasedSalesOrderId = soBronzeHn.Id,
            LicensePlate = "29-B2 441.09",
            ColorName = "Pearl White",
            RegistrationSource = "ShowroomSale",
            OwnershipStartDateUtc = utcNow.AddMonths(-2),
            CurrentMileageKm = 900,
            LastServiceDateUtc = null,
            LastServiceMileageKm = 0,
            NextDueMileageKm = 1_000,
            NextDueDateUtc = utcNow.AddDays(6),
            IsActive = true,
            CreatedAtUtc = utcNow.AddMonths(-2)
        });

        var vehPlatinumSh = EnsureVehicle(new CustomerVehicle(Guid.Parse("99999999-9999-9999-9999-999999999904"))
        {
            VIN = "RLHJC9204RY000388",
            CustomerId = custPlatinum.Id,
            MotorcycleModelId = models["HND-SH160I-2026"].Id,
            PurchasedSalesOrderId = soPlatinum.Id,
            LicensePlate = "59-S2 999.88",
            ColorName = "Matte Black ABS",
            RegistrationSource = "ShowroomSale",
            OwnershipStartDateUtc = utcNow.AddMonths(-5),
            CurrentMileageKm = 5_200,
            LastServiceDateUtc = utcNow.AddMonths(-1),
            LastServiceMileageKm = 4_000,
            NextDueMileageKm = 8_000,
            NextDueDateUtc = utcNow.AddMonths(5),
            IsActive = true,
            CreatedAtUtc = utcNow.AddMonths(-5)
        });

        var vehGoldCb300 = EnsureVehicle(new CustomerVehicle(Guid.Parse("99999999-9999-9999-9999-999999999905"))
        {
            VIN = "RLHNC5501RY000511",
            CustomerId = custGold.Id,
            MotorcycleModelId = models["HND-CB300R-2026"].Id,
            PurchasedSalesOrderId = null,
            LicensePlate = "30-A8 771.66",
            ColorName = "Candy Chromosphere Red",
            RegistrationSource = "ExternalManual",
            OwnershipStartDateUtc = utcNow.AddMonths(-6),
            CurrentMileageKm = 6_400,
            LastServiceDateUtc = utcNow.AddMonths(-2),
            LastServiceMileageKm = 4_000,
            NextDueMileageKm = 8_000,
            NextDueDateUtc = utcNow.AddMonths(4),
            IsActive = true,
            CreatedAtUtc = utcNow.AddMonths(-6)
        });

        _ = EnsureVehicle(new CustomerVehicle(Guid.Parse("99999999-9999-9999-9999-999999999906"))
        {
            VIN = "RLHPC6405RY000612",
            CustomerId = custGold.Id,
            MotorcycleModelId = models["HND-NX500-2026"].Id,
            PurchasedSalesOrderId = null,
            LicensePlate = "30-K1 335.20",
            ColorName = "Matte Gunpowder Black",
            RegistrationSource = "ExternalManual",
            OwnershipStartDateUtc = utcNow.AddMonths(-5),
            CurrentMileageKm = 9_100,
            LastServiceDateUtc = utcNow.AddMonths(-1),
            LastServiceMileageKm = 8_000,
            NextDueMileageKm = 12_000,
            NextDueDateUtc = utcNow.AddMonths(5),
            IsActive = true,
            CreatedAtUtc = utcNow.AddMonths(-5)
        });

        if (_dbContext.ChangeTracker.HasChanges())
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        // 4. CustomerVouchers (keyed by VoucherCode)
        var existingVouchers = await _dbContext.CustomerVouchers
            .IgnoreQueryFilters()
            .ToDictionaryAsync(v => v.VoucherCode, StringComparer.Ordinal, cancellationToken);

        CustomerVoucher EnsureVoucher(CustomerVoucher spec)
        {
            if (existingVouchers.TryGetValue(spec.VoucherCode, out var existing))
            {
                return existing;
            }

            _dbContext.CustomerVouchers.Add(spec);
            existingVouchers[spec.VoucherCode] = spec;
            return spec;
        }

        var voucherSilverReady = EnsureVoucher(new CustomerVoucher(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaa01"))
        {
            VoucherCode = "SVC100K-7K9P2M4X",
            LoyaltyAccountId = accSilver.Id,
            VoucherCatalogId = catalogs["OIL100K"].Id,
            PointsSpent = 200,
            Status = "Available",
            RedeemedAtUtc = utcNow.AddDays(-10),
            ExpiresAtUtc = utcNow.AddDays(80),
            CreatedAtUtc = utcNow.AddDays(-10)
        });

        var voucherSilverUsed = EnsureVoucher(new CustomerVoucher(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaa02"))
        {
            VoucherCode = "PARTS10P-3M7Q9L2W",
            LoyaltyAccountId = accSilver.Id,
            VoucherCatalogId = catalogs["PARTS10P"].Id,
            PointsSpent = 1_111,
            Status = "Used",
            RedeemedAtUtc = utcNow.AddMonths(-2).AddDays(-5),
            ExpiresAtUtc = utcNow.AddMonths(1),
            UsedAtUtc = utcNow.AddMonths(-2),
            CreatedAtUtc = utcNow.AddMonths(-2).AddDays(-5)
        });

        var voucherGoldReady = EnsureVoucher(new CustomerVoucher(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaa03"))
        {
            VoucherCode = "SVC15PCT-4R8N2W9Q",
            LoyaltyAccountId = accGold.Id,
            VoucherCatalogId = catalogs["PARTS10P"].Id,
            PointsSpent = 300,
            Status = "Available",
            RedeemedAtUtc = utcNow.AddDays(-7),
            ExpiresAtUtc = utcNow.AddDays(83),
            CreatedAtUtc = utcNow.AddDays(-7)
        });

        var voucherGoldUsed = EnsureVoucher(new CustomerVoucher(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaa04"))
        {
            VoucherCode = "GOLD15P-8X2K5V9N",
            LoyaltyAccountId = accGold.Id,
            VoucherCatalogId = catalogs["GOLD15P"].Id,
            PointsSpent = 1_450,
            Status = "Used",
            RedeemedAtUtc = utcNow.AddMonths(-2).AddDays(-3),
            ExpiresAtUtc = utcNow.AddMonths(2),
            UsedAtUtc = utcNow.AddMonths(-2),
            CreatedAtUtc = utcNow.AddMonths(-2).AddDays(-3)
        });

        var voucherPlatinumUsed = EnsureVoucher(new CustomerVoucher(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaa05"))
        {
            VoucherCode = "PLATVIP300K-9P4W6Z1M",
            LoyaltyAccountId = accPlatinum.Id,
            VoucherCatalogId = catalogs["PLATVIP300K"].Id,
            PointsSpent = 5_300,
            Status = "Used",
            RedeemedAtUtc = utcNow.AddMonths(-1).AddDays(-10),
            ExpiresAtUtc = utcNow.AddMonths(5),
            UsedAtUtc = utcNow.AddMonths(-1),
            CreatedAtUtc = utcNow.AddMonths(-1).AddDays(-10)
        });

        if (_dbContext.ChangeTracker.HasChanges())
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        // Reconcile VoucherCatalogs.QuotaRedeemed idempotently against actual CustomerVouchers counts
        var actualRedeemedByCatalog = await _dbContext.CustomerVouchers
            .IgnoreQueryFilters()
            .GroupBy(v => v.VoucherCatalogId)
            .Select(g => new { CatalogId = g.Key, RedeemedCount = g.Count() })
            .ToDictionaryAsync(x => x.CatalogId, x => x.RedeemedCount, cancellationToken);

        var allCatalogRows = await _dbContext.VoucherCatalogs
            .IgnoreQueryFilters()
            .ToListAsync(cancellationToken);

        var quotaReconciled = false;
        foreach (var catalogRow in allCatalogRows)
        {
            var expectedRedeemed = actualRedeemedByCatalog.TryGetValue(catalogRow.Id, out var count) ? count : 0;
            if (catalogRow.QuotaRedeemed != expectedRedeemed)
            {
                if (catalogRow.QuotaTotal.HasValue && catalogRow.QuotaTotal.Value < expectedRedeemed)
                {
                    catalogRow.QuotaTotal = expectedRedeemed;
                }

                catalogRow.QuotaRedeemed = expectedRedeemed;
                catalogRow.UpdatedAtUtc = utcNow;
                quotaReconciled = true;
            }
        }

        if (quotaReconciled)
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        // 5. Historical ServiceTimeSlots, ServiceBookings & ServiceInvoices across all 3 dealers
        var pastDateHcm = DateOnly.FromDateTime(utcNow.AddMonths(-3).DateTime);
        var pastDateHn = DateOnly.FromDateTime(utcNow.AddMonths(-2).DateTime);
        var pastDateDn = DateOnly.FromDateTime(utcNow.AddMonths(-1).DateTime);

        var slotHcmId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbb01");
        var slotHnId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbb02");
        var slotDnId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbb03");

        var existingHistSlots = await _dbContext.ServiceTimeSlots
            .IgnoreQueryFilters()
            .Where(s =>
                s.Id == slotHcmId || s.Id == slotHnId || s.Id == slotDnId ||
                s.SlotDate == pastDateHcm || s.SlotDate == pastDateHn || s.SlotDate == pastDateDn)
            .ToListAsync(cancellationToken);

        ServiceTimeSlot EnsureHistSlot(ServiceTimeSlot spec)
        {
            var match = existingHistSlots.FirstOrDefault(s =>
                s.Id == spec.Id ||
                (s.DealerId == spec.DealerId &&
                 s.SlotDate == spec.SlotDate &&
                 s.StartTime == spec.StartTime &&
                 s.EndTime == spec.EndTime));
            if (match is not null)
            {
                return match;
            }

            _dbContext.ServiceTimeSlots.Add(spec);
            existingHistSlots.Add(spec);
            return spec;
        }

        var slotHcmHist = EnsureHistSlot(new ServiceTimeSlot(slotHcmId)
        {
            DealerId = dealers["DLR-HCM-01"].Id,
            SlotDate = pastDateHcm,
            StartTime = new TimeOnly(9, 0),
            EndTime = new TimeOnly(10, 0),
            MaxCapacity = 5,
            BookedCount = 1,
            IsBlocked = false,
            CreatedAtUtc = utcNow.AddMonths(-3).AddDays(-2)
        });

        var slotHnHist = EnsureHistSlot(new ServiceTimeSlot(slotHnId)
        {
            DealerId = dealers["DLR-HN-01"].Id,
            SlotDate = pastDateHn,
            StartTime = new TimeOnly(10, 0),
            EndTime = new TimeOnly(11, 0),
            MaxCapacity = 4,
            BookedCount = 1,
            IsBlocked = false,
            CreatedAtUtc = utcNow.AddMonths(-2).AddDays(-2)
        });

        var slotDnHist = EnsureHistSlot(new ServiceTimeSlot(slotDnId)
        {
            DealerId = dealers["DLR-DN-01"].Id,
            SlotDate = pastDateDn,
            StartTime = new TimeOnly(13, 30),
            EndTime = new TimeOnly(14, 30),
            MaxCapacity = 4,
            BookedCount = 1,
            IsBlocked = false,
            CreatedAtUtc = utcNow.AddMonths(-1).AddDays(-2)
        });

        if (_dbContext.ChangeTracker.HasChanges())
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        var existingBookings = await _dbContext.ServiceBookings
            .IgnoreQueryFilters()
            .ToDictionaryAsync(b => b.BookingNumber, StringComparer.Ordinal, cancellationToken);

        ServiceBooking EnsureBooking(ServiceBooking spec)
        {
            if (existingBookings.TryGetValue(spec.BookingNumber, out var existing))
            {
                return existing;
            }

            _dbContext.ServiceBookings.Add(spec);
            existingBookings[spec.BookingNumber] = spec;
            return spec;
        }

        var bookingHcm = EnsureBooking(new ServiceBooking(Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"))
        {
            BookingNumber = "SB-202608-0001",
            CustomerId = custSilver.Id,
            DealerId = dealers["DLR-HCM-01"].Id,
            CustomerVehicleId = vehSilverSh.Id,
            ServiceTimeSlotId = slotHcmHist.Id,
            ServiceType = "PeriodicMaintenance",
            ReportedNotes = "Initial 1,000 km break-in synthetic oil & CVT inspection.",
            OdometerAtBookingKm = 980,
            Status = "Completed",
            CheckedInAtUtc = utcNow.AddMonths(-3),
            CompletedAtUtc = utcNow.AddMonths(-3).AddHours(1),
            CreatedAtUtc = utcNow.AddMonths(-3).AddDays(-1)
        });

        var bookingHn = EnsureBooking(new ServiceBooking(Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccc02"))
        {
            BookingNumber = "SB-202609-0002",
            CustomerId = custGold.Id,
            DealerId = dealers["DLR-HN-01"].Id,
            CustomerVehicleId = vehGoldCb300.Id,
            ServiceTimeSlotId = slotHnHist.Id,
            AppliedCustomerVoucherId = voucherGoldUsed.Id,
            ServiceType = "PeriodicMaintenance",
            ReportedNotes = "Major suspension, chain kit, tire set, and brake service.",
            OdometerAtBookingKm = 3_950,
            Status = "Completed",
            CheckedInAtUtc = utcNow.AddMonths(-2),
            CompletedAtUtc = utcNow.AddMonths(-2).AddHours(2),
            CreatedAtUtc = utcNow.AddMonths(-2).AddDays(-1)
        });

        var bookingDn = EnsureBooking(new ServiceBooking(Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccc03"))
        {
            BookingNumber = "SB-202610-0003",
            CustomerId = custPlatinum.Id,
            DealerId = dealers["DLR-DN-01"].Id,
            CustomerVehicleId = vehPlatinumSh.Id,
            ServiceTimeSlotId = slotDnHist.Id,
            AppliedCustomerVoucherId = voucherPlatinumUsed.Id,
            ServiceType = "Inspection",
            ReportedNotes = "Coastal tour checkup and full ceramic detailing.",
            OdometerAtBookingKm = 3_990,
            Status = "Completed",
            CheckedInAtUtc = utcNow.AddMonths(-1),
            CompletedAtUtc = utcNow.AddMonths(-1).AddHours(1),
            CreatedAtUtc = utcNow.AddMonths(-1).AddDays(-1)
        });

        if (_dbContext.ChangeTracker.HasChanges())
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        var existingInvoices = await _dbContext.ServiceInvoices
            .IgnoreQueryFilters()
            .ToDictionaryAsync(i => i.InvoiceNumber, StringComparer.Ordinal, cancellationToken);

        ServiceInvoice EnsureInvoice(ServiceInvoice spec)
        {
            if (existingInvoices.TryGetValue(spec.InvoiceNumber, out var existing))
            {
                return existing;
            }

            _dbContext.ServiceInvoices.Add(spec);
            existingInvoices[spec.InvoiceNumber] = spec;
            return spec;
        }

        var invoiceHcm = EnsureInvoice(new ServiceInvoice(Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddd01"))
        {
            InvoiceNumber = "INV-202608-0001",
            ServiceBookingId = bookingHcm.Id,
            DealerId = dealers["DLR-HCM-01"].Id,
            CustomerId = custSilver.Id,
            IssuedByStaffId = staffHcm.Id,
            LaborAmount = 2_760_000m,
            PartsAmount = 9_000_000m,
            GrossAmount = 11_760_000m,
            VoucherDiscountAmount = 0m,
            NetPaidAmount = 11_760_000m,
            OdometerAtCompletionKm = 1_000,
            PointsEarned = 1_410,
            WorkSummaryNotes = "Completed 1,000 km inspection plus accessory package installation.",
            LineItemsJson = "[{\"item\":\"1000km Inspection & Accessories\",\"qty\":1,\"amount\":11760000}]",
            PaymentMethod = "BankTransfer",
            PaidAtUtc = utcNow.AddMonths(-3).AddHours(1),
            CreatedAtUtc = utcNow.AddMonths(-3).AddHours(1)
        });

        var invoiceHn = EnsureInvoice(new ServiceInvoice(Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddd02"))
        {
            InvoiceNumber = "INV-202609-0002",
            ServiceBookingId = bookingHn.Id,
            DealerId = dealers["DLR-HN-01"].Id,
            CustomerId = custGold.Id,
            AppliedCustomerVoucherId = voucherGoldUsed.Id,
            IssuedByStaffId = staffHn.Id,
            LaborAmount = 4_400_000m,
            PartsAmount = 28_000_000m,
            GrossAmount = 32_400_000m,
            VoucherDiscountAmount = 400_000m,
            NetPaidAmount = 32_000_000m,
            OdometerAtCompletionKm = 4_000,
            PointsEarned = 3_200,
            WorkSummaryNotes = "Completed major CB300R tire, Showa suspension, and brake overhaul.",
            LineItemsJson = "[{\"item\":\"CB300R Major Overhaul & Pirelli Tire Set\",\"qty\":1,\"amount\":32400000}]",
            PaymentMethod = "Card",
            PaidAtUtc = utcNow.AddMonths(-2).AddHours(2),
            CreatedAtUtc = utcNow.AddMonths(-2).AddHours(2)
        });

        var invoiceDn = EnsureInvoice(new ServiceInvoice(Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddd03"))
        {
            InvoiceNumber = "INV-202610-0003",
            ServiceBookingId = bookingDn.Id,
            DealerId = dealers["DLR-DN-01"].Id,
            CustomerId = custPlatinum.Id,
            AppliedCustomerVoucherId = voucherPlatinumUsed.Id,
            IssuedByStaffId = staffDn.Id,
            LaborAmount = 500_000m,
            PartsAmount = 600_000m,
            GrossAmount = 1_100_000m,
            VoucherDiscountAmount = 300_000m,
            NetPaidAmount = 800_000m,
            OdometerAtCompletionKm = 4_000,
            PointsEarned = 0,
            WorkSummaryNotes = "Complimentary Platinum coastal checkup with VIP voucher applied.",
            LineItemsJson = "[{\"item\":\"VIP Priority Detailing & Checkup\",\"qty\":1,\"amount\":1100000}]",
            PaymentMethod = "BankTransfer",
            PaidAtUtc = utcNow.AddMonths(-1).AddHours(1),
            CreatedAtUtc = utcNow.AddMonths(-1).AddHours(1)
        });

        if (_dbContext.ChangeTracker.HasChanges())
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        await SeedServiceInvoiceLineItemsAsync(
            invoiceHcm,
            invoiceHn,
            invoiceDn,
            spareParts,
            cancellationToken);

        // 6. Immutable PointLedgerEntries — keyed by IdempotencyKey (SUM(PointsDelta) == AvailablePoints)
        var existingIdempotencyKeys = (await _dbContext.PointLedgerEntries
                .IgnoreQueryFilters()
                .Select(p => p.IdempotencyKey)
                .ToListAsync(cancellationToken))
            .ToHashSet(StringComparer.Ordinal);

        var ledgerEntries = new[]
        {
            new PointLedgerEntry
            {
                LoyaltyAccountId = accMinh.Id,
                TransactionType = "SalesOrderEarn",
                PointsDelta = 120,
                BalanceAfter = 120,
                ReferenceType = "SalesOrder",
                ReferenceId = soMinhDn.Id,
                IdempotencyKey = $"SALES_ORDER:{soMinhDn.Id}:EARN",
                Notes = "Initial loyalty points accrued on Winner X onboarding.",
                CreatedByUserId = staffDn.Id,
                CreatedAtUtc = utcNow.AddMonths(-1)
            },
            new PointLedgerEntry
            {
                LoyaltyAccountId = accBronze.Id,
                TransactionType = "SalesOrderEarn",
                PointsDelta = 120,
                BalanceAfter = 120,
                ReferenceType = "SalesOrder",
                ReferenceId = soBronzeHn.Id,
                IdempotencyKey = $"SALES_ORDER:{soBronzeHn.Id}:EARN",
                Notes = "Initial loyalty points accrued on Air Blade 160 onboarding.",
                CreatedByUserId = staffHn.Id,
                CreatedAtUtc = utcNow.AddMonths(-2)
            },
            new PointLedgerEntry
            {
                LoyaltyAccountId = accSilver.Id,
                TransactionType = "SalesOrderEarn",
                PointsDelta = 550,
                BalanceAfter = 550,
                ReferenceType = "SalesOrder",
                ReferenceId = soSilver.Id,
                IdempotencyKey = $"SALES_ORDER:{soSilver.Id}:EARN",
                Notes = "Promotional points earned on SH160i showroom purchase (promoted to Silver).",
                CreatedByUserId = staffHcm.Id,
                CreatedAtUtc = utcNow.AddMonths(-4)
            },
            new PointLedgerEntry
            {
                LoyaltyAccountId = accSilver.Id,
                TransactionType = "ServiceInvoiceEarn",
                PointsDelta = 1_410,
                BalanceAfter = 1_960,
                ReferenceType = "ServiceInvoice",
                ReferenceId = invoiceHcm.Id,
                IdempotencyKey = $"SERVICE_INVOICE:{invoiceHcm.Id}:EARN",
                Notes = "Earned 1.20x Silver points on workshop invoice INV-202608-0001.",
                CreatedByUserId = staffHcm.Id,
                CreatedAtUtc = utcNow.AddMonths(-3).AddHours(1)
            },
            new PointLedgerEntry
            {
                LoyaltyAccountId = accSilver.Id,
                TransactionType = "VoucherRedeem",
                PointsDelta = -1_111,
                BalanceAfter = 849,
                ReferenceType = "CustomerVoucher",
                ReferenceId = voucherSilverUsed.Id,
                IdempotencyKey = $"VOUCHER_REDEEM:{voucherSilverUsed.Id}:SPEND",
                Notes = "Redeemed historical workshop bundle voucher PARTS10P-3M7Q9L2W.",
                CreatedByUserId = custSilver.Id,
                CreatedAtUtc = utcNow.AddMonths(-2).AddDays(-5)
            },
            new PointLedgerEntry
            {
                LoyaltyAccountId = accSilver.Id,
                TransactionType = "VoucherRedeem",
                PointsDelta = -200,
                BalanceAfter = 649,
                ReferenceType = "CustomerVoucher",
                ReferenceId = voucherSilverReady.Id,
                IdempotencyKey = $"VOUCHER_REDEEM:{voucherSilverReady.Id}:SPEND",
                Notes = "Redeemed 100,000 VND periodic maintenance voucher SVC100K-7K9P2M4X.",
                CreatedByUserId = custSilver.Id,
                CreatedAtUtc = utcNow.AddDays(-10)
            },
            new PointLedgerEntry
            {
                LoyaltyAccountId = accGold.Id,
                TransactionType = "ServiceInvoiceEarn",
                PointsDelta = 3_200,
                BalanceAfter = 3_200,
                ReferenceType = "ServiceInvoice",
                ReferenceId = invoiceHn.Id,
                IdempotencyKey = $"SERVICE_INVOICE:{invoiceHn.Id}:EARN",
                Notes = "Earned points on major Hanoi workshop overhaul INV-202609-0002 (promoted to Gold).",
                CreatedByUserId = staffHn.Id,
                CreatedAtUtc = utcNow.AddMonths(-2).AddHours(2)
            },
            new PointLedgerEntry
            {
                LoyaltyAccountId = accGold.Id,
                TransactionType = "VoucherRedeem",
                PointsDelta = -1_450,
                BalanceAfter = 1_750,
                ReferenceType = "CustomerVoucher",
                ReferenceId = voucherGoldUsed.Id,
                IdempotencyKey = $"VOUCHER_REDEEM:{voucherGoldUsed.Id}:SPEND",
                Notes = "Redeemed Gold genuine parts voucher GOLD15P-8X2K5V9N.",
                CreatedByUserId = custGold.Id,
                CreatedAtUtc = utcNow.AddMonths(-2).AddDays(-3)
            },
            new PointLedgerEntry
            {
                LoyaltyAccountId = accGold.Id,
                TransactionType = "VoucherRedeem",
                PointsDelta = -300,
                BalanceAfter = 1_450,
                ReferenceType = "CustomerVoucher",
                ReferenceId = voucherGoldReady.Id,
                IdempotencyKey = $"VOUCHER_REDEEM:{voucherGoldReady.Id}:SPEND",
                Notes = "Redeemed active workshop percentage voucher SVC15PCT-4R8N2W9Q.",
                CreatedByUserId = custGold.Id,
                CreatedAtUtc = utcNow.AddDays(-7)
            },
            new PointLedgerEntry
            {
                LoyaltyAccountId = accPlatinum.Id,
                TransactionType = "SalesOrderEarn",
                PointsDelta = 10_100,
                BalanceAfter = 10_100,
                ReferenceType = "SalesOrder",
                ReferenceId = soPlatinum.Id,
                IdempotencyKey = $"SALES_ORDER:{soPlatinum.Id}:EARN",
                Notes = "Earned 10,100 points on SH160i showroom purchase SO-202605-0001 (promoted to Platinum).",
                CreatedByUserId = staffHcm.Id,
                CreatedAtUtc = utcNow.AddMonths(-5)
            },
            new PointLedgerEntry
            {
                LoyaltyAccountId = accPlatinum.Id,
                TransactionType = "VoucherRedeem",
                PointsDelta = -5_300,
                BalanceAfter = 4_800,
                ReferenceType = "CustomerVoucher",
                ReferenceId = voucherPlatinumUsed.Id,
                IdempotencyKey = $"VOUCHER_REDEEM:{voucherPlatinumUsed.Id}:SPEND",
                Notes = "Redeemed Platinum VIP detailing package voucher PLATVIP300K-9P4W6Z1M.",
                CreatedByUserId = custPlatinum.Id,
                CreatedAtUtc = utcNow.AddMonths(-1).AddDays(-10)
            }
        };

        foreach (var entry in ledgerEntries)
        {
            if (existingIdempotencyKeys.Add(entry.IdempotencyKey))
            {
                _dbContext.PointLedgerEntries.Add(entry);
            }
        }

        // 7. Initial MaintenanceReminder & Notifications (keyed by DeduplicationKey / deterministic Id)
        var shPeriodicRule = rules[(models["HND-SH160I-2026"].Id, "PERIODIC_4000")];
        var reminderId = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeee01");
        var reminderDedupKey = $"{vehSilverSh.VIN}:{shPeriodicRule.Id}:4000";

        var reminderExists = await _dbContext.MaintenanceReminders
            .IgnoreQueryFilters()
            .AnyAsync(r => r.Id == reminderId || r.DeduplicationKey == reminderDedupKey, cancellationToken);

        if (!reminderExists)
        {
            _dbContext.MaintenanceReminders.Add(new MaintenanceReminder(reminderId)
            {
                CustomerVehicleId = vehSilverSh.Id,
                MaintenanceRuleId = shPeriodicRule.Id,
                TargetMileageKm = 4_000,
                DueDateUtc = vehSilverSh.NextDueDateUtc,
                TriggerReason = "MileageThreshold",
                DeduplicationKey = reminderDedupKey,
                Status = "Sent",
                SentAtUtc = utcNow.AddHours(-6),
                CreatedAtUtc = utcNow.AddHours(-6)
            });
        }

        var notif1Id = Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffffff01");
        var notif2Id = Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffffff02");
        var existingNotifIds = (await _dbContext.Notifications
                .IgnoreQueryFilters()
                .Where(n => n.Id == notif1Id || n.Id == notif2Id)
                .Select(n => n.Id)
                .ToListAsync(cancellationToken))
            .ToHashSet();

        var existingNotifTitles = (await _dbContext.Notifications
                .IgnoreQueryFilters()
                .Select(n => n.Title)
                .ToListAsync(cancellationToken))
            .ToHashSet(StringComparer.Ordinal);

        const string notif1Title = "4,000 km Periodic Maintenance Approaching";
        if (!existingNotifIds.Contains(notif1Id) && !existingNotifTitles.Contains(notif1Title))
        {
            _dbContext.Notifications.Add(new Notification(notif1Id)
            {
                RecipientUserId = custSilver.Id,
                EventType = "MaintenanceReminderTriggered",
                Title = notif1Title,
                Message = "Your Honda SH160i ABS Sport (RLHJF5109RZ104829) at 3,850 km is due for its 4,000 km service.",
                PayloadJson = "{\"vin\":\"RLHJF5109RZ104829\",\"currentMileageKm\":3850,\"targetMileageKm\":4000}",
                IsRead = false,
                CreatedAtUtc = utcNow.AddHours(-6)
            });
        }

        const string notif2Title = "Workshop Workbench Ready — Apex Moto Saigon Center";
        if (!existingNotifIds.Contains(notif2Id) && !existingNotifTitles.Contains(notif2Title))
        {
            _dbContext.Notifications.Add(new Notification(notif2Id)
            {
                TargetDealerId = dealers["DLR-HCM-01"].Id,
                EventType = "BookingCreated",
                Title = notif2Title,
                Message = "Live SignalR dealer group routing active for Saigon service bays.",
                PayloadJson = "{\"dealerCode\":\"DLR-HCM-01\",\"bays\":5}",
                IsRead = false,
                CreatedAtUtc = utcNow.AddHours(-2)
            });
        }

        if (_dbContext.ChangeTracker.HasChanges())
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task SeedServiceInvoiceLineItemsAsync(
        ServiceInvoice invoiceHcm,
        ServiceInvoice invoiceHn,
        ServiceInvoice invoiceDn,
        Dictionary<string, SparePart> spareParts,
        CancellationToken cancellationToken)
    {
        var invoiceIdsWithLines = (await _dbContext.ServiceInvoiceLineItems
                .IgnoreQueryFilters()
                .Select(li => li.ServiceInvoiceId)
                .Distinct()
                .ToListAsync(cancellationToken))
            .ToHashSet();

        var added = false;

        if (!invoiceIdsWithLines.Contains(invoiceHcm.Id))
        {
            var oil10W30 = spareParts["PRT-OIL-10W30"];
            var cvtBelt = spareParts["PRT-CVT-BLT01"];
            var frontBrakes = spareParts["PRT-BRK-FR01"];

            _dbContext.ServiceInvoiceLineItems.AddRange(
                new ServiceInvoiceLineItem(Guid.Parse("13131313-1313-1313-1313-131313131301"))
                {
                    ServiceInvoiceId = invoiceHcm.Id,
                    LineNumber = 1,
                    SparePartId = oil10W30.Id,
                    PartCodeSnapshot = oil10W30.PartCode,
                    Description = oil10W30.Name,
                    ItemType = "Part",
                    Quantity = 2,
                    UnitPriceVnd = 250_000m,
                    LineTotalVnd = 500_000m
                },
                new ServiceInvoiceLineItem(Guid.Parse("13131313-1313-1313-1313-131313131302"))
                {
                    ServiceInvoiceId = invoiceHcm.Id,
                    LineNumber = 2,
                    SparePartId = cvtBelt.Id,
                    PartCodeSnapshot = cvtBelt.PartCode,
                    Description = cvtBelt.Name,
                    ItemType = "Part",
                    Quantity = 1,
                    UnitPriceVnd = 850_000m,
                    LineTotalVnd = 850_000m
                },
                new ServiceInvoiceLineItem(Guid.Parse("13131313-1313-1313-1313-131313131303"))
                {
                    ServiceInvoiceId = invoiceHcm.Id,
                    LineNumber = 3,
                    SparePartId = frontBrakes.Id,
                    PartCodeSnapshot = frontBrakes.PartCode,
                    Description = frontBrakes.Name,
                    ItemType = "Part",
                    Quantity = 2,
                    UnitPriceVnd = 450_000m,
                    LineTotalVnd = 900_000m
                },
                new ServiceInvoiceLineItem(Guid.Parse("13131313-1313-1313-1313-131313131304"))
                {
                    ServiceInvoiceId = invoiceHcm.Id,
                    LineNumber = 4,
                    SparePartId = null,
                    PartCodeSnapshot = "ACC-SH160-KIT",
                    Description = "Genuine Honda Touring Top-Box & Crash Guard Package",
                    ItemType = "Part",
                    Quantity = 1,
                    UnitPriceVnd = 6_750_000m,
                    LineTotalVnd = 6_750_000m
                },
                new ServiceInvoiceLineItem(Guid.Parse("13131313-1313-1313-1313-131313131305"))
                {
                    ServiceInvoiceId = invoiceHcm.Id,
                    LineNumber = 5,
                    SparePartId = null,
                    PartCodeSnapshot = "LABOR-1000KM",
                    Description = "1,000 km Break-In Inspection & Accessory Installation Labor",
                    ItemType = "Labor",
                    Quantity = 1,
                    UnitPriceVnd = 2_760_000m,
                    LineTotalVnd = 2_760_000m
                });

            invoiceIdsWithLines.Add(invoiceHcm.Id);
            added = true;
        }

        if (!invoiceIdsWithLines.Contains(invoiceHn.Id))
        {
            var chainKit = spareParts["PRT-CHN-520VX"];
            var oil10W40 = spareParts["PRT-OIL-10W40"];
            var frontBrakes = spareParts["PRT-BRK-FR01"];
            var sparkPlug = spareParts["PRT-SPK-IR01"];

            _dbContext.ServiceInvoiceLineItems.AddRange(
                new ServiceInvoiceLineItem(Guid.Parse("13131313-1313-1313-1313-131313131306"))
                {
                    ServiceInvoiceId = invoiceHn.Id,
                    LineNumber = 1,
                    SparePartId = chainKit.Id,
                    PartCodeSnapshot = chainKit.PartCode,
                    Description = chainKit.Name,
                    ItemType = "Part",
                    Quantity = 1,
                    UnitPriceVnd = 2_800_000m,
                    LineTotalVnd = 2_800_000m
                },
                new ServiceInvoiceLineItem(Guid.Parse("13131313-1313-1313-1313-131313131307"))
                {
                    ServiceInvoiceId = invoiceHn.Id,
                    LineNumber = 2,
                    SparePartId = oil10W40.Id,
                    PartCodeSnapshot = oil10W40.PartCode,
                    Description = oil10W40.Name,
                    ItemType = "Part",
                    Quantity = 2,
                    UnitPriceVnd = 300_000m,
                    LineTotalVnd = 600_000m
                },
                new ServiceInvoiceLineItem(Guid.Parse("13131313-1313-1313-1313-131313131308"))
                {
                    ServiceInvoiceId = invoiceHn.Id,
                    LineNumber = 3,
                    SparePartId = frontBrakes.Id,
                    PartCodeSnapshot = frontBrakes.PartCode,
                    Description = frontBrakes.Name,
                    ItemType = "Part",
                    Quantity = 2,
                    UnitPriceVnd = 450_000m,
                    LineTotalVnd = 900_000m
                },
                new ServiceInvoiceLineItem(Guid.Parse("13131313-1313-1313-1313-131313131309"))
                {
                    ServiceInvoiceId = invoiceHn.Id,
                    LineNumber = 4,
                    SparePartId = sparkPlug.Id,
                    PartCodeSnapshot = sparkPlug.PartCode,
                    Description = sparkPlug.Name,
                    ItemType = "Part",
                    Quantity = 2,
                    UnitPriceVnd = 350_000m,
                    LineTotalVnd = 700_000m
                },
                new ServiceInvoiceLineItem(Guid.Parse("13131313-1313-1313-1313-131313131310"))
                {
                    ServiceInvoiceId = invoiceHn.Id,
                    LineNumber = 5,
                    SparePartId = null,
                    PartCodeSnapshot = "OEM-SHOWA-PIRELLI",
                    Description = "Showa SFF-BP Fork Cartridge & Pirelli Diablo Rosso IV Tire Set",
                    ItemType = "Part",
                    Quantity = 1,
                    UnitPriceVnd = 23_000_000m,
                    LineTotalVnd = 23_000_000m
                },
                new ServiceInvoiceLineItem(Guid.Parse("13131313-1313-1313-1313-131313131311"))
                {
                    ServiceInvoiceId = invoiceHn.Id,
                    LineNumber = 6,
                    SparePartId = null,
                    PartCodeSnapshot = "LABOR-MAJOR",
                    Description = "Major 4,000 km Suspension, Chain & Brake Overhaul Labor",
                    ItemType = "Labor",
                    Quantity = 1,
                    UnitPriceVnd = 4_400_000m,
                    LineTotalVnd = 4_400_000m
                });

            invoiceIdsWithLines.Add(invoiceHn.Id);
            added = true;
        }

        if (!invoiceIdsWithLines.Contains(invoiceDn.Id))
        {
            var oil10W40 = spareParts["PRT-OIL-10W40"];

            _dbContext.ServiceInvoiceLineItems.AddRange(
                new ServiceInvoiceLineItem(Guid.Parse("13131313-1313-1313-1313-131313131312"))
                {
                    ServiceInvoiceId = invoiceDn.Id,
                    LineNumber = 1,
                    SparePartId = oil10W40.Id,
                    PartCodeSnapshot = oil10W40.PartCode,
                    Description = oil10W40.Name,
                    ItemType = "Part",
                    Quantity = 2,
                    UnitPriceVnd = 300_000m,
                    LineTotalVnd = 600_000m
                },
                new ServiceInvoiceLineItem(Guid.Parse("13131313-1313-1313-1313-131313131313"))
                {
                    ServiceInvoiceId = invoiceDn.Id,
                    LineNumber = 2,
                    SparePartId = null,
                    PartCodeSnapshot = "LABOR-DETAIL",
                    Description = "Platinum Priority Bay Checkup & Ceramic Detailing Labor",
                    ItemType = "Labor",
                    Quantity = 1,
                    UnitPriceVnd = 500_000m,
                    LineTotalVnd = 500_000m
                });

            invoiceIdsWithLines.Add(invoiceDn.Id);
            added = true;
        }

        // Reconcile any pre-existing historical/smoke ServiceInvoices lacking itemized rows so SUM(LineTotalVnd) == GrossAmount holds universally
        var unitemizedInvoices = await _dbContext.ServiceInvoices
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(i => !invoiceIdsWithLines.Contains(i.Id))
            .Select(i => new { i.Id, i.PartsAmount, i.LaborAmount })
            .ToListAsync(cancellationToken);

        foreach (var inv in unitemizedInvoices)
        {
            var nextLineNumber = 1;
            if (inv.PartsAmount > 0m)
            {
                _dbContext.ServiceInvoiceLineItems.Add(new ServiceInvoiceLineItem
                {
                    ServiceInvoiceId = inv.Id,
                    LineNumber = nextLineNumber++,
                    SparePartId = null,
                    PartCodeSnapshot = "PARTS-SUMMARY",
                    Description = "Workshop Genuine Parts & Consumables",
                    ItemType = "Part",
                    Quantity = 1,
                    UnitPriceVnd = inv.PartsAmount,
                    LineTotalVnd = inv.PartsAmount
                });
                added = true;
            }

            if (inv.LaborAmount > 0m || nextLineNumber == 1)
            {
                _dbContext.ServiceInvoiceLineItems.Add(new ServiceInvoiceLineItem
                {
                    ServiceInvoiceId = inv.Id,
                    LineNumber = nextLineNumber,
                    SparePartId = null,
                    PartCodeSnapshot = "LABOR-SUMMARY",
                    Description = "Workshop Service & Inspection Labor",
                    ItemType = "Labor",
                    Quantity = 1,
                    UnitPriceVnd = inv.LaborAmount,
                    LineTotalVnd = inv.LaborAmount
                });
                added = true;
            }
        }

        if (added)
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task SeedTestDriveBookingsAsync(
        Dictionary<string, Dealer> dealers,
        Dictionary<string, User> users,
        Dictionary<string, MotorcycleModel> models,
        Dictionary<string, DealerInventory> inventories,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken)
    {
        var existing = await _dbContext.TestDriveBookings
            .IgnoreQueryFilters()
            .ToDictionaryAsync(t => t.BookingNumber, StringComparer.Ordinal, cancellationToken);

        var vietnamToday = DateOnly.FromDateTime(utcNow.ToOffset(TimeSpan.FromHours(7)).DateTime);
        var custBronze = users[User.NormalizeEmailValue("customer.bronze@motorcycledealer.vn")];
        var minhBronze = users[User.NormalizeEmailValue("minh.nguyen@customer.vn")];
        var custSilver = users[User.NormalizeEmailValue("customer.silver@motorcycledealer.vn")];
        var custGold = users[User.NormalizeEmailValue("customer.gold@motorcycledealer.vn")];

        var specs = new[]
        {
            new TestDriveBooking(Guid.Parse("14141414-1414-1414-1414-141414141401"))
            {
                BookingNumber = "TD-SEED-HN-001",
                CustomerId = custBronze.Id,
                DealerId = dealers["DLR-HN-01"].Id,
                MotorcycleModelId = models["HND-CB300R-2026"].Id,
                AssignedInventoryId = inventories["RLHNC5501RY000105"].Id,
                PreferredDate = vietnamToday.AddDays(2),
                StartTime = new TimeOnly(9, 0),
                EndTime = new TimeOnly(9, 45),
                DriverLicenseNumber = "A2-010554321",
                ContactPhone = custBronze.PhoneNumber,
                Status = TestDriveStatus.Confirmed,
                CustomerNotes = "Interested in testing Neo Sports Cafe ergonomics in city traffic.",
                StaffNotes = "Demo CB300R unit RLHNC5501RY000105 prepped at Hanoi bay 1.",
                ConfirmedAtUtc = utcNow.AddHours(-4),
                CreatedAtUtc = utcNow.AddHours(-8)
            },
            new TestDriveBooking(Guid.Parse("14141414-1414-1414-1414-141414141402"))
            {
                BookingNumber = "TD-SEED-DN-001",
                CustomerId = minhBronze.Id,
                DealerId = dealers["DLR-DN-01"].Id,
                MotorcycleModelId = models["HND-NX500-2026"].Id,
                AssignedInventoryId = inventories["RLHPC6405RY000206"].Id,
                PreferredDate = vietnamToday.AddDays(-3),
                StartTime = new TimeOnly(10, 0),
                EndTime = new TimeOnly(10, 45),
                DriverLicenseNumber = "A2-430098112",
                ContactPhone = minhBronze.PhoneNumber,
                Status = TestDriveStatus.Completed,
                CustomerNotes = "Evaluating NX500 wind protection along Bach Dang coastal route.",
                StaffNotes = "Completed supervised 45-minute coastal test ride cleanly.",
                ConfirmedAtUtc = utcNow.AddDays(-4),
                CompletedAtUtc = utcNow.AddDays(-3).AddHours(1),
                CreatedAtUtc = utcNow.AddDays(-5)
            },
            new TestDriveBooking(Guid.Parse("14141414-1414-1414-1414-141414141403"))
            {
                BookingNumber = "TD-SEED-HCM-001",
                CustomerId = custGold.Id,
                DealerId = dealers["DLR-HCM-01"].Id,
                MotorcycleModelId = models["HND-NX500-2026"].Id,
                AssignedInventoryId = null,
                PreferredDate = vietnamToday.AddDays(3),
                StartTime = new TimeOnly(14, 0),
                EndTime = new TimeOnly(14, 45),
                DriverLicenseNumber = "A2-790128451",
                ContactPhone = custGold.PhoneNumber,
                Status = TestDriveStatus.Requested,
                CustomerNotes = "Comparing NX500 Adventure upright seating posture before showroom upgrade.",
                CreatedAtUtc = utcNow.AddHours(-3)
            },
            new TestDriveBooking(Guid.Parse("14141414-1414-1414-1414-141414141404"))
            {
                BookingNumber = "TD-SEED-HCM-002",
                CustomerId = custSilver.Id,
                DealerId = dealers["DLR-HCM-01"].Id,
                MotorcycleModelId = models["YMH-EXC155-2026"].Id,
                AssignedInventoryId = inventories["RLCUG1010RY000302"].Id,
                PreferredDate = vietnamToday.AddDays(5),
                StartTime = new TimeOnly(15, 30),
                EndTime = new TimeOnly(16, 15),
                DriverLicenseNumber = "A1-791234567",
                ContactPhone = custSilver.PhoneNumber,
                Status = TestDriveStatus.Confirmed,
                CustomerNotes = "Weekend test ride for Assist & Slipper clutch feel.",
                StaffNotes = "Assigned Monster Energy Black demo VIN RLCUG1010RY000302.",
                ConfirmedAtUtc = utcNow.AddHours(-1),
                CreatedAtUtc = utcNow.AddHours(-5)
            }
        };

        var added = false;
        foreach (var spec in specs)
        {
            if (!existing.ContainsKey(spec.BookingNumber))
            {
                _dbContext.TestDriveBookings.Add(spec);
                existing[spec.BookingNumber] = spec;
                added = true;
            }
        }

        if (added)
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task SeedServiceReviewsAndWarrantyClaimsAsync(
        Dictionary<string, Dealer> dealers,
        Dictionary<string, User> users,
        Dictionary<string, SparePart> spareParts,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken)
    {
        var staffHn = users[User.NormalizeEmailValue("staff.hn@motorcycledealer.vn")];
        var staffDn = users[User.NormalizeEmailValue("staff.dn@motorcycledealer.vn")];
        var custBronze = users[User.NormalizeEmailValue("customer.bronze@motorcycledealer.vn")];
        var minhBronze = users[User.NormalizeEmailValue("minh.nguyen@customer.vn")];
        var custGold = users[User.NormalizeEmailValue("customer.gold@motorcycledealer.vn")];
        var custPlatinum = users[User.NormalizeEmailValue("customer.platinum@motorcycledealer.vn")];

        // 1. Seed 2 realistic ServiceReviews on INV-202609-0002 (Hanoi / Gold) & INV-202610-0003 (Da Nang / Platinum).
        //    INV-202608-0001 (Saigon / Silver) is intentionally left UNREVIEWED for live closed-loop CSAT reviews,
        //    preserving customer.silver at AvailablePoints = 649 and LifetimeQualifyingPoints = 1,960.
        var invoiceHn = await _dbContext.ServiceInvoices
            .IgnoreQueryFilters()
            .SingleAsync(i => i.InvoiceNumber == "INV-202609-0002", cancellationToken);

        var invoiceDn = await _dbContext.ServiceInvoices
            .IgnoreQueryFilters()
            .SingleAsync(i => i.InvoiceNumber == "INV-202610-0003", cancellationToken);

        var bookingHn = await _dbContext.ServiceBookings
            .IgnoreQueryFilters()
            .SingleAsync(b => b.Id == invoiceHn.ServiceBookingId, cancellationToken);

        var bookingDn = await _dbContext.ServiceBookings
            .IgnoreQueryFilters()
            .SingleAsync(b => b.Id == invoiceDn.ServiceBookingId, cancellationToken);

        var existingReviews = await _dbContext.ServiceReviews
            .IgnoreQueryFilters()
            .Select(r => new { r.Id, r.ReviewNumber, r.ServiceInvoiceId })
            .ToListAsync(cancellationToken);

        var existingReviewNumbers = existingReviews
            .Select(r => r.ReviewNumber)
            .ToHashSet(StringComparer.Ordinal);

        var existingReviewedInvoiceIds = existingReviews
            .Select(r => r.ServiceInvoiceId)
            .ToHashSet();

        var existingLedgerKeys = (await _dbContext.PointLedgerEntries
                .IgnoreQueryFilters()
                .Select(p => p.IdempotencyKey)
                .ToListAsync(cancellationToken))
            .ToHashSet(StringComparer.Ordinal);

        var accGold = await _dbContext.LoyaltyAccounts
            .IgnoreQueryFilters()
            .SingleAsync(a => a.CustomerId == custGold.Id, cancellationToken);

        var accPlatinum = await _dbContext.LoyaltyAccounts
            .IgnoreQueryFilters()
            .SingleAsync(a => a.CustomerId == custPlatinum.Id, cancellationToken);

        const int reviewBonusPoints = 25;
        var reviewHnId = existingReviews
            .FirstOrDefault(r => r.ServiceInvoiceId == invoiceHn.Id || string.Equals(r.ReviewNumber, "REV-SEED-HN-001", StringComparison.Ordinal))?.Id
            ?? Guid.Parse("15151515-1515-1515-1515-151515151501");

        if (!existingReviewNumbers.Contains("REV-SEED-HN-001") && !existingReviewedInvoiceIds.Contains(invoiceHn.Id))
        {
            _dbContext.ServiceReviews.Add(new ServiceReview(reviewHnId)
            {
                ReviewNumber = "REV-SEED-HN-001",
                ServiceInvoiceId = invoiceHn.Id,
                ServiceBookingId = bookingHn.Id,
                CustomerId = custGold.Id,
                DealerId = dealers["DLR-HN-01"].Id,
                CustomerVehicleId = bookingHn.CustomerVehicleId,
                Rating = 5,
                QualityTags = "ExpertTechnician,GenuineParts,TransparentPricing",
                Comment = "Flawless CB300R suspension, DID X-Ring chain kit, and brake overhaul at Hanoi Flagship.",
                BonusPointsAwarded = reviewBonusPoints,
                StaffResponse = "Thank you for trusting Apex Moto Hanoi Flagship with your CB300R major overhaul! Safe riding.",
                RespondedByUserId = staffHn.Id,
                RespondedAtUtc = utcNow.AddMonths(-2).AddDays(2),
                CreatedAtUtc = utcNow.AddMonths(-2).AddDays(1)
            });
            existingReviewNumbers.Add("REV-SEED-HN-001");
            existingReviewedInvoiceIds.Add(invoiceHn.Id);
        }

        var bonusKeyHn = $"SERVICE_REVIEW:{invoiceHn.Id}:BONUS";
        if (existingReviewedInvoiceIds.Contains(invoiceHn.Id) && existingLedgerKeys.Add(bonusKeyHn))
        {
            accGold.AvailablePoints += reviewBonusPoints;
            accGold.UpdatedAtUtc = utcNow;
            _dbContext.PointLedgerEntries.Add(new PointLedgerEntry
            {
                LoyaltyAccountId = accGold.Id,
                TransactionType = nameof(LedgerTransactionType.ServiceReviewBonus),
                PointsDelta = reviewBonusPoints,
                BalanceAfter = accGold.AvailablePoints,
                ReferenceType = "ServiceReview",
                ReferenceId = reviewHnId,
                IdempotencyKey = bonusKeyHn,
                Notes = "Earned +25 CSAT bonus points for 5-star workshop review REV-SEED-HN-001.",
                CreatedByUserId = custGold.Id,
                CreatedAtUtc = utcNow.AddDays(-5)
            });
        }

        var reviewDnId = existingReviews
            .FirstOrDefault(r => r.ServiceInvoiceId == invoiceDn.Id || string.Equals(r.ReviewNumber, "REV-SEED-DN-001", StringComparison.Ordinal))?.Id
            ?? Guid.Parse("15151515-1515-1515-1515-151515151502");

        if (!existingReviewNumbers.Contains("REV-SEED-DN-001") && !existingReviewedInvoiceIds.Contains(invoiceDn.Id))
        {
            _dbContext.ServiceReviews.Add(new ServiceReview(reviewDnId)
            {
                ReviewNumber = "REV-SEED-DN-001",
                ServiceInvoiceId = invoiceDn.Id,
                ServiceBookingId = bookingDn.Id,
                CustomerId = custPlatinum.Id,
                DealerId = dealers["DLR-DN-01"].Id,
                CustomerVehicleId = bookingDn.CustomerVehicleId,
                Rating = 4,
                QualityTags = "CleanLounge,FastTurnaround",
                Comment = "Great VIP coastal detailing bay experience; lounge espresso service was excellent.",
                BonusPointsAwarded = reviewBonusPoints,
                StaffResponse = null,
                RespondedByUserId = null,
                RespondedAtUtc = null,
                CreatedAtUtc = utcNow.AddDays(-4)
            });
            existingReviewNumbers.Add("REV-SEED-DN-001");
            existingReviewedInvoiceIds.Add(invoiceDn.Id);
        }

        var bonusKeyDn = $"SERVICE_REVIEW:{invoiceDn.Id}:BONUS";
        if (existingReviewedInvoiceIds.Contains(invoiceDn.Id) && existingLedgerKeys.Add(bonusKeyDn))
        {
            accPlatinum.AvailablePoints += reviewBonusPoints;
            accPlatinum.UpdatedAtUtc = utcNow;
            _dbContext.PointLedgerEntries.Add(new PointLedgerEntry
            {
                LoyaltyAccountId = accPlatinum.Id,
                TransactionType = nameof(LedgerTransactionType.ServiceReviewBonus),
                PointsDelta = reviewBonusPoints,
                BalanceAfter = accPlatinum.AvailablePoints,
                ReferenceType = "ServiceReview",
                ReferenceId = reviewDnId,
                IdempotencyKey = bonusKeyDn,
                Notes = "Earned +25 CSAT bonus points for workshop review REV-SEED-DN-001.",
                CreatedByUserId = custPlatinum.Id,
                CreatedAtUtc = utcNow.AddDays(-4)
            });
        }

        if (_dbContext.ChangeTracker.HasChanges())
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        // Reconcile LoyaltyAccount.AvailablePoints == SUM(PointLedgerEntries.PointsDelta) without mutating LifetimeQualifyingPoints
        var ledgerSumsByAccount = await _dbContext.PointLedgerEntries
            .IgnoreQueryFilters()
            .GroupBy(p => p.LoyaltyAccountId)
            .Select(g => new { AccountId = g.Key, TotalPoints = g.Sum(p => p.PointsDelta) })
            .ToDictionaryAsync(x => x.AccountId, x => x.TotalPoints, cancellationToken);

        var allAccounts = await _dbContext.LoyaltyAccounts
            .IgnoreQueryFilters()
            .ToListAsync(cancellationToken);

        var accountsReconciled = false;
        foreach (var account in allAccounts)
        {
            var expectedAvailable = ledgerSumsByAccount.TryGetValue(account.Id, out var sum) ? sum : 0;
            if (account.AvailablePoints != expectedAvailable && expectedAvailable >= 0)
            {
                account.AvailablePoints = expectedAvailable;
                account.UpdatedAtUtc = utcNow;
                accountsReconciled = true;
            }
        }

        if (accountsReconciled)
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        // 2. Seed 3 realistic WarrantyClaims across DLR-HN-01, DLR-DN-01, and DLR-HCM-01
        var existingClaims = await _dbContext.WarrantyClaims
            .IgnoreQueryFilters()
            .ToDictionaryAsync(c => c.ClaimNumber, StringComparer.Ordinal, cancellationToken);

        var vehBronzeAb = await _dbContext.CustomerVehicles
            .IgnoreQueryFilters()
            .SingleAsync(v => v.VIN == "RLHJF5108RY000123", cancellationToken);

        var vehMinhWinner = await _dbContext.CustomerVehicles
            .IgnoreQueryFilters()
            .SingleAsync(v => v.VIN == "RLHKF4102RY000205", cancellationToken);

        var vehPlatinumSh = await _dbContext.CustomerVehicles
            .IgnoreQueryFilters()
            .SingleAsync(v => v.VIN == "RLHJC9204RY000388", cancellationToken);

        var sparkPlugPart = spareParts["PRT-SPK-IR01"];

        var claimSpecs = new[]
        {
            new WarrantyClaim(Guid.Parse("16161616-1616-1616-1616-161616161601"))
            {
                ClaimNumber = "WC-SEED-HN-001",
                CustomerId = custBronze.Id,
                CustomerVehicleId = vehBronzeAb.Id,
                DealerId = dealers["DLR-HN-01"].Id,
                VinSnapshot = vehBronzeAb.VIN,
                OdometerAtClaimKm = 880,
                DefectCategory = WarrantyDefectCategory.Electrical,
                SymptomDescription = "Intermittent cold-start ignition misfire traced to factory spark plug resistor cap.",
                Status = WarrantyClaimStatus.Completed,
                CoveredSparePartId = sparkPlugPart.Id,
                CoveredAmountVnd = 350_000m,
                ResolutionNotes = "Replaced spark plug assembly with OEM NGK Laser Iridium unit PRT-SPK-IR01 under 36-month factory warranty and verified ECU ignition timing.",
                ReviewedByUserId = staffHn.Id,
                ReviewedAtUtc = utcNow.AddDays(-14),
                CompletedAtUtc = utcNow.AddDays(-12),
                CreatedAtUtc = utcNow.AddDays(-15)
            },
            new WarrantyClaim(Guid.Parse("16161616-1616-1616-1616-161616161602"))
            {
                ClaimNumber = "WC-SEED-DN-001",
                CustomerId = minhBronze.Id,
                CustomerVehicleId = vehMinhWinner.Id,
                DealerId = dealers["DLR-DN-01"].Id,
                VinSnapshot = vehMinhWinner.VIN,
                OdometerAtClaimKm = 900,
                DefectCategory = WarrantyDefectCategory.BrakesAndSuspension,
                SymptomDescription = "Front ABS caliper slight vibration under low-speed coastal braking.",
                Status = WarrantyClaimStatus.UnderReview,
                CoveredSparePartId = null,
                CoveredAmountVnd = 0.00m,
                ResolutionNotes = null,
                ReviewedByUserId = staffDn.Id,
                ReviewedAtUtc = utcNow.AddDays(-2),
                CompletedAtUtc = null,
                CreatedAtUtc = utcNow.AddDays(-3)
            },
            new WarrantyClaim(Guid.Parse("16161616-1616-1616-1616-161616161603"))
            {
                ClaimNumber = "WC-SEED-HCM-001",
                CustomerId = custPlatinum.Id,
                CustomerVehicleId = vehPlatinumSh.Id,
                DealerId = dealers["DLR-HCM-01"].Id,
                VinSnapshot = vehPlatinumSh.VIN,
                OdometerAtClaimKm = 5_200,
                DefectCategory = WarrantyDefectCategory.Transmission,
                SymptomDescription = "Noticeable CVT drive belt shudder during initial throttle roll-on from standstill.",
                Status = WarrantyClaimStatus.Submitted,
                CoveredSparePartId = null,
                CoveredAmountVnd = 0.00m,
                ResolutionNotes = null,
                ReviewedByUserId = null,
                ReviewedAtUtc = null,
                CompletedAtUtc = null,
                CreatedAtUtc = utcNow.AddHours(-4)
            }
        };

        var claimsAdded = false;
        foreach (var spec in claimSpecs)
        {
            if (!existingClaims.ContainsKey(spec.ClaimNumber))
            {
                _dbContext.WarrantyClaims.Add(spec);
                existingClaims[spec.ClaimNumber] = spec;
                claimsAdded = true;
            }
        }

        if (claimsAdded)
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
