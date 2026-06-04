using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace MotorcycleDealer.Infrastructure.Persistence;

public sealed class MotorcycleDealerDbContextFactory : IDesignTimeDbContextFactory<MotorcycleDealerDbContext>
{
    public MotorcycleDealerDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            connectionString = DependencyInjection.DefaultLocalDbConnectionString;
        }

        var optionsBuilder = new DbContextOptionsBuilder<MotorcycleDealerDbContext>();
        optionsBuilder.UseSqlServer(connectionString, sqlOptions =>
        {
            sqlOptions.UseCompatibilityLevel(DependencyInjection.SqlServerCompatibilityLevel);
            sqlOptions.MigrationsAssembly(typeof(MotorcycleDealerDbContext).Assembly.FullName);
        });

        optionsBuilder.ConfigureWarnings(warnings => warnings.Ignore(
            CoreEventId.PossibleIncorrectRequiredNavigationWithQueryFilterInteractionWarning,
            SqlServerEventId.SavepointsDisabledBecauseOfMARS));

        return new MotorcycleDealerDbContext(optionsBuilder.Options);
    }
}
