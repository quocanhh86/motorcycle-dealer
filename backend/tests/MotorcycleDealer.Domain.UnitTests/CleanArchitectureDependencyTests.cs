using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using MotorcycleDealer.Application.Common.Interfaces;
using MotorcycleDealer.Domain.Common;
using MotorcycleDealer.Domain.Constants;
using MotorcycleDealer.Domain.Enums;
using MotorcycleDealer.Infrastructure.Persistence;
using Xunit;

namespace MotorcycleDealer.Domain.UnitTests;

public sealed class CleanArchitectureDependencyTests
{
    private static readonly Assembly DomainAssembly = typeof(BaseEntity).Assembly;
    private static readonly Assembly ApplicationAssembly = typeof(IApplicationDbContext).Assembly;
    private static readonly Assembly InfrastructureAssembly = typeof(MotorcycleDealerDbContext).Assembly;
    private static readonly Assembly WebApiAssembly = typeof(Program).Assembly;

    private sealed class SampleDomainEntity : BaseEntity;

    [Fact]
    public void Domain_ShouldHaveZeroProjectOrFrameworkDependencies()
    {
        var referencedNames = DomainAssembly
            .GetReferencedAssemblies()
            .Select(a => a.Name ?? string.Empty)
            .ToArray();

        Assert.DoesNotContain(referencedNames, name => name.StartsWith("MotorcycleDealer.", StringComparison.Ordinal));
        Assert.DoesNotContain(referencedNames, name => name.StartsWith("Microsoft.EntityFrameworkCore", StringComparison.Ordinal));
        Assert.DoesNotContain(referencedNames, name => name.StartsWith("Microsoft.AspNetCore", StringComparison.Ordinal));
        Assert.DoesNotContain(referencedNames, name => name.StartsWith("FluentValidation", StringComparison.Ordinal));
        Assert.DoesNotContain(referencedNames, name => string.Equals(name, "System.Text.Json", StringComparison.Ordinal));
    }

    [Fact]
    public void Application_ShouldReferenceDomainOnly_AndZeroEfOrAspNetPackages()
    {
        var referencedNames = ApplicationAssembly
            .GetReferencedAssemblies()
            .Select(a => a.Name ?? string.Empty)
            .ToArray();

        Assert.Contains("MotorcycleDealer.Domain", referencedNames);
        Assert.DoesNotContain("MotorcycleDealer.Infrastructure", referencedNames);
        Assert.DoesNotContain("MotorcycleDealer.WebApi", referencedNames);
        Assert.DoesNotContain(referencedNames, name => name.StartsWith("Microsoft.EntityFrameworkCore", StringComparison.Ordinal));
        Assert.DoesNotContain(referencedNames, name => name.StartsWith("Microsoft.AspNetCore", StringComparison.Ordinal));
    }

    [Fact]
    public void Infrastructure_ShouldNeverReferenceWebApi()
    {
        var referencedNames = InfrastructureAssembly
            .GetReferencedAssemblies()
            .Select(a => a.Name ?? string.Empty)
            .ToArray();

        Assert.Contains("MotorcycleDealer.Domain", referencedNames);
        Assert.Contains("MotorcycleDealer.Application", referencedNames);
        Assert.DoesNotContain("MotorcycleDealer.WebApi", referencedNames);
    }

    [Fact]
    public void WebApiControllers_ShouldNotInjectConcreteDbContext_OrReturnBaseEntitiesDirectlyOrWrapped()
    {
        var controllerTypes = WebApiAssembly
            .GetTypes()
            .Where(t => !t.IsAbstract && typeof(ControllerBase).IsAssignableFrom(t))
            .ToArray();

        Assert.NotEmpty(controllerTypes);

        foreach (var controller in controllerTypes)
        {
            foreach (var constructor in controller.GetConstructors())
            {
                foreach (var parameter in constructor.GetParameters())
                {
                    Assert.False(
                        ContainsConcreteDbContext(parameter.ParameterType),
                        $"Controller {controller.Name} must not inject MotorcycleDealerDbContext.");
                }
            }

            foreach (var action in controller.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly))
            {
                Assert.False(
                    ContainsBaseEntityType(action.ReturnType),
                    $"Controller action {controller.Name}.{action.Name} must not return BaseEntity directly or wrapped in generic envelopes.");
            }
        }

        // Self-verify recursive reflection guard catches nested generic envelopes such as Task<ActionResult<IReadOnlyList<BaseEntity>>>
        Assert.True(ContainsBaseEntityType(typeof(Task<ActionResult<IReadOnlyList<SampleDomainEntity>>>)));
        Assert.True(ContainsBaseEntityType(typeof(ActionResult<SampleDomainEntity[]>)));
        Assert.False(ContainsBaseEntityType(typeof(Task<ActionResult<string>>)));
    }

    [Fact]
    public void MonetaryAndMultiplierProperties_ShouldUseDecimal_NeverFloatOrDouble()
    {
        var allDomainAndAppTypes = DomainAssembly.GetTypes().Concat(ApplicationAssembly.GetTypes());
        var financialSuffixes = new[] { "Amount", "Price", "Vnd", "Multiplier" };

        foreach (var type in allDomainAndAppTypes)
        {
            foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static))
            {
                if (financialSuffixes.Any(suffix => prop.Name.EndsWith(suffix, StringComparison.Ordinal)))
                {
                    Assert.NotEqual(typeof(float), prop.PropertyType);
                    Assert.NotEqual(typeof(double), prop.PropertyType);
                    Assert.NotEqual(typeof(float?), prop.PropertyType);
                    Assert.NotEqual(typeof(double?), prop.PropertyType);
                }
            }
        }
    }

    [Fact]
    public void PermissionsCatalog_ShouldContainExact48DistinctCanonicalPermissions_AndEnforceSeparationOfDuties()
    {
        Assert.Equal(48, Permissions.All.Count);
        Assert.Equal(48, Permissions.All.Distinct(StringComparer.Ordinal).Count());
        Assert.All(Permissions.All, code => Assert.StartsWith("Permissions.", code, StringComparison.Ordinal));

        Assert.Equal(20, Permissions.CustomerDefaults.Count);
        Assert.Equal(22, Permissions.DealerStaffDefaults.Count);
        Assert.Equal(32, Permissions.AdminDefaults.Count);

        string[] customerExclusivePermissions =
        [
            Permissions.SalesOrders.ViewOwn,
            Permissions.Vehicles.ViewOwn,
            Permissions.Vehicles.RegisterOwn,
            Permissions.Bookings.Create,
            Permissions.Bookings.CancelOwn,
            Permissions.Bookings.ViewOwn,
            Permissions.TestDrives.Create,
            Permissions.TestDrives.CancelOwn,
            Permissions.TestDrives.ViewOwn,
            Permissions.Invoices.ViewOwn,
            Permissions.Loyalty.ViewOwn,
            Permissions.Loyalty.RedeemVoucher,
            Permissions.Reviews.CreateOwn,
            Permissions.Reviews.ViewOwn,
            Permissions.Warranty.CreateOwn,
            Permissions.Warranty.ViewOwn
        ];

        foreach (var customerOnly in customerExclusivePermissions)
        {
            Assert.Contains(customerOnly, Permissions.CustomerDefaults);
            Assert.DoesNotContain(customerOnly, Permissions.DealerStaffDefaults);
            Assert.DoesNotContain(customerOnly, Permissions.AdminDefaults);
        }

        string[] staffAndAdminOperationalPermissions =
        [
            Permissions.Reviews.ViewDealer,
            Permissions.Reviews.RespondDealer,
            Permissions.Warranty.ViewDealer,
            Permissions.Warranty.ManageDealer
        ];

        foreach (var dealerPerm in staffAndAdminOperationalPermissions)
        {
            Assert.DoesNotContain(dealerPerm, Permissions.CustomerDefaults);
            Assert.Contains(dealerPerm, Permissions.DealerStaffDefaults);
            Assert.Contains(dealerPerm, Permissions.AdminDefaults);
        }

        Assert.DoesNotContain(Permissions.SpareParts.ManageCatalog, Permissions.CustomerDefaults);
        Assert.DoesNotContain(Permissions.SpareParts.ManageCatalog, Permissions.DealerStaffDefaults);
        Assert.Contains(Permissions.SpareParts.ManageCatalog, Permissions.AdminDefaults);

        Assert.Equal(Permissions.CustomerDefaults, Permissions.GetDefaultsForRole(UserRoleCode.Customer));
        Assert.Equal(Permissions.DealerStaffDefaults, Permissions.GetDefaultsForRole(UserRoleCode.DealerStaff));
        Assert.Equal(Permissions.AdminDefaults, Permissions.GetDefaultsForRole(UserRoleCode.Admin));
    }

    [Fact]
    public void InfrastructureAssembly_ShouldUseDomainNamedRegistrations_WithZeroPhaseNumberedShims()
    {
        var phaseNumberPattern = new Regex(@"Phase\d+", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        Assembly[] productionAssemblies =
        [
            DomainAssembly,
            ApplicationAssembly,
            InfrastructureAssembly,
            WebApiAssembly
        ];

        foreach (var assembly in productionAssemblies)
        {
            var asmName = assembly.GetName().Name ?? assembly.FullName ?? "UnknownAssembly";
            foreach (var type in assembly.GetTypes())
            {
                Assert.False(
                    phaseNumberPattern.IsMatch(type.Name),
                    $"Production assembly '{asmName}' must not declare phase-numbered type '{type.FullName}'.");

                foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
                {
                    Assert.False(
                        phaseNumberPattern.IsMatch(method.Name),
                        $"Production type '{type.FullName}' in '{asmName}' must not declare phase-numbered public method '{method.Name}'.");
                }
            }
        }

        // Self-verify regex detects Phase-numbered identifiers
        Assert.Matches(phaseNumberPattern, "Phase4Shim");
        Assert.Matches(phaseNumberPattern, "AddPhase8Services");
        Assert.DoesNotMatch(phaseNumberPattern, "AddTestDriveAndSparePartServices");
        Assert.DoesNotMatch(phaseNumberPattern, "AddReviewAndWarrantyServices");

        var diMethods = typeof(MotorcycleDealer.Infrastructure.DependencyInjection)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Select(m => m.Name)
            .ToHashSet(StringComparer.Ordinal);

        Assert.Contains("AddInfrastructure", diMethods);
        Assert.Contains("AddIdentityAndDealerServices", diMethods);
        Assert.Contains("AddShowroomAndGarageServices", diMethods);
        Assert.Contains("AddWorkshopBookingServices", diMethods);
        Assert.Contains("AddLoyaltyAndVoucherServices", diMethods);
        Assert.Contains("AddInvoiceAndNotificationServices", diMethods);
        Assert.Contains("AddTestDriveAndSparePartServices", diMethods);
        Assert.Contains("AddReviewAndWarrantyServices", diMethods);
        Assert.Contains("AddAnalyticsServices", diMethods);
    }

    private static bool ContainsConcreteDbContext(Type type) =>
        ContainsMatchingType(type, t => typeof(MotorcycleDealerDbContext).IsAssignableFrom(t), []);

    private static bool ContainsBaseEntityType(Type type) =>
        ContainsMatchingType(type, t => typeof(BaseEntity).IsAssignableFrom(t), []);

    private static bool ContainsMatchingType(Type type, Func<Type, bool> predicate, HashSet<Type> visited)
    {
        if (!visited.Add(type))
        {
            return false;
        }

        if (predicate(type))
        {
            return true;
        }

        if (type.IsArray && type.GetElementType() is { } elementType)
        {
            return ContainsMatchingType(elementType, predicate, visited);
        }

        if (type.IsGenericType)
        {
            foreach (var genericArg in type.GetGenericArguments())
            {
                if (ContainsMatchingType(genericArg, predicate, visited))
                {
                    return true;
                }
            }
        }

        return false;
    }
}
