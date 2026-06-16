using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MotorcycleDealer.Application.Common.Interfaces;
using MotorcycleDealer.Application.Common.Models;
using MotorcycleDealer.WebApi.Middleware;

namespace MotorcycleDealer.WebApi.Controllers;

[ApiController]
[Route("api/v1/health")]
public sealed class HealthController : ControllerBase
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IHostEnvironment _environment;

    public HealthController(
        IApplicationDbContext dbContext,
        IDateTimeProvider dateTimeProvider,
        IHostEnvironment environment)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _dateTimeProvider = dateTimeProvider ?? throw new ArgumentNullException(nameof(dateTimeProvider));
        _environment = environment ?? throw new ArgumentNullException(nameof(environment));
    }

    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(SystemHealthDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<SystemHealthDto>> GetHealthAsync(
        [FromQuery] bool probeDb = false,
        CancellationToken cancellationToken = default)
    {
        var databaseState = "MotorcycleDealerDb (Configured)";

        if (probeDb)
        {
            using var probeCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            probeCts.CancelAfter(TimeSpan.FromSeconds(2));
            var connected = await _dbContext.CanConnectAsync(probeCts.Token);

            if (!connected)
            {
                var problem = GlobalExceptionHandler.CreateCustomProblemDetails(
                    HttpContext,
                    StatusCodes.Status503ServiceUnavailable,
                    title: "Database Unavailable",
                    detail: "SQL Server MotorcycleDealerDb is currently unreachable.",
                    errorCode: "DATABASE_UNAVAILABLE");

                return new ObjectResult(problem)
                {
                    StatusCode = StatusCodes.Status503ServiceUnavailable,
                    ContentTypes = { "application/problem+json" }
                };
            }

            databaseState = "MotorcycleDealerDb (Connected)";
        }

        var dto = new SystemHealthDto(
            Status: "Healthy",
            Service: "MotorcycleDealer.WebApi",
            Version: "1.0.0",
            Environment: _environment.EnvironmentName,
            Database: databaseState,
            TimestampUtc: _dateTimeProvider.UtcNow,
            TraceId: GlobalExceptionHandler.ResolveTraceId(HttpContext));

        return Ok(dto);
    }
}
