using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MotorcycleDealer.Application.Common.Interfaces;
using MotorcycleDealer.Application.Features.Auth;
using MotorcycleDealer.Application.Features.Vehicles;
using MotorcycleDealer.Domain.Constants;
using MotorcycleDealer.WebApi.Middleware;
using MotorcycleDealer.WebApi.Security;

namespace MotorcycleDealer.WebApi.Controllers;

[ApiController]
[Route("api/v1/vehicles")]
public sealed class VehiclesController : ControllerBase
{
    private readonly ICustomerVehicleService _vehicleService;
    private readonly IDateTimeProvider _dateTimeProvider;

    public VehiclesController(
        ICustomerVehicleService vehicleService,
        IDateTimeProvider dateTimeProvider)
    {
        _vehicleService = vehicleService ?? throw new ArgumentNullException(nameof(vehicleService));
        _dateTimeProvider = dateTimeProvider ?? throw new ArgumentNullException(nameof(dateTimeProvider));
    }

    [HttpGet]
    [Authorize]
    [ProducesResponseType(typeof(ApiEnvelope<IReadOnlyList<CustomerVehicleDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiEnvelope<IReadOnlyList<CustomerVehicleDto>>>> ListVehiclesAsync(
        [FromQuery] string? vin = null,
        [FromQuery] Guid? customerId = null,
        [FromQuery] string? licensePlate = null,
        [FromQuery] string? email = null,
        [FromQuery] string? search = null,
        CancellationToken cancellationToken = default)
    {
        var query = new VehicleLookupQuery(
            Vin: vin,
            CustomerId: customerId,
            LicensePlate: licensePlate,
            Email: email,
            Search: search);

        var vehicles = await _vehicleService.ListVehiclesAsync(
            query,
            forceOwnCustomerOnly: false,
            cancellationToken);

        return Ok(WrapSuccess(vehicles));
    }

    [HttpGet("my")]
    [HasPermission(Permissions.Vehicles.ViewOwn)]
    [ProducesResponseType(typeof(ApiEnvelope<IReadOnlyList<CustomerVehicleDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiEnvelope<IReadOnlyList<CustomerVehicleDto>>>> ListMyVehiclesAsync(
        [FromQuery] string? vin = null,
        [FromQuery] string? search = null,
        CancellationToken cancellationToken = default)
    {
        var query = new VehicleLookupQuery(Vin: vin, Search: search);
        var vehicles = await _vehicleService.ListVehiclesAsync(
            query,
            forceOwnCustomerOnly: true,
            cancellationToken);

        return Ok(WrapSuccess(vehicles));
    }

    [HttpGet("lookup")]
    [HasPermission(Permissions.Vehicles.ViewDealer)]
    [ProducesResponseType(typeof(ApiEnvelope<IReadOnlyList<CustomerVehicleDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiEnvelope<IReadOnlyList<CustomerVehicleDto>>>> LookupVehiclesForWorkshopAsync(
        [FromQuery] string? vin = null,
        [FromQuery] Guid? customerId = null,
        [FromQuery] string? licensePlate = null,
        [FromQuery] string? email = null,
        [FromQuery] string? search = null,
        CancellationToken cancellationToken = default)
    {
        var query = new VehicleLookupQuery(
            Vin: vin,
            CustomerId: customerId,
            LicensePlate: licensePlate,
            Email: email,
            Search: search);

        var vehicles = await _vehicleService.ListVehiclesAsync(
            query,
            forceOwnCustomerOnly: false,
            cancellationToken);

        return Ok(WrapSuccess(vehicles));
    }

    [HttpGet("{vehicleId:guid}")]
    [Authorize]
    [ProducesResponseType(typeof(ApiEnvelope<CustomerVehicleDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiEnvelope<CustomerVehicleDto>>> GetVehicleByIdAsync(
        [FromRoute] Guid vehicleId,
        CancellationToken cancellationToken = default)
    {
        var vehicle = await _vehicleService.GetVehicleByIdAsync(vehicleId, cancellationToken);
        return Ok(WrapSuccess(vehicle));
    }

    [HttpGet("{vehicleId:guid}/reminders")]
    [Authorize]
    [ProducesResponseType(typeof(ApiEnvelope<VehicleMaintenanceScheduleDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiEnvelope<VehicleMaintenanceScheduleDto>>> ListVehicleRemindersAsync(
        [FromRoute] Guid vehicleId,
        CancellationToken cancellationToken = default)
    {
        var schedule = await _vehicleService.GetVehicleMaintenanceScheduleAsync(vehicleId, cancellationToken);
        return Ok(WrapSuccess(schedule));
    }

    [HttpGet("{vehicleId:guid}/maintenance-history")]
    [Authorize]
    [ProducesResponseType(typeof(ApiEnvelope<VehicleMaintenanceScheduleDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiEnvelope<VehicleMaintenanceScheduleDto>>> GetVehicleMaintenanceHistoryAsync(
        [FromRoute] Guid vehicleId,
        CancellationToken cancellationToken = default)
    {
        var schedule = await _vehicleService.GetVehicleMaintenanceScheduleAsync(vehicleId, cancellationToken);
        return Ok(WrapSuccess(schedule));
    }

    [HttpPost]
    [HasPermission(Permissions.Vehicles.RegisterOwn)]
    [ProducesResponseType(typeof(ApiEnvelope<CustomerVehicleDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<ApiEnvelope<CustomerVehicleDto>>> RegisterExternalVehicleAsync(
        [FromBody] RegisterExternalVehicleRequest request,
        CancellationToken cancellationToken = default)
    {
        var created = await _vehicleService.RegisterExternalVehicleAsync(request, cancellationToken);
        Response.Headers.Location = $"/api/v1/vehicles/{created.Id}";
        return StatusCode(StatusCodes.Status201Created, WrapSuccess(created));
    }

    [HttpPatch("{vehicleId:guid}/mileage")]
    [HasPermission(Permissions.Vehicles.RegisterOwn)]
    [ProducesResponseType(typeof(ApiEnvelope<CustomerVehicleDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<ApiEnvelope<CustomerVehicleDto>>> UpdateVehicleMileageAsync(
        [FromRoute] Guid vehicleId,
        [FromBody] UpdateVehicleMileageRequest request,
        CancellationToken cancellationToken = default)
    {
        var updated = await _vehicleService.UpdateVehicleMileageAsync(vehicleId, request, cancellationToken);
        return Ok(WrapSuccess(updated));
    }

    private ApiEnvelope<T> WrapSuccess<T>(T data) =>
        ApiEnvelope.Success(
            data,
            _dateTimeProvider.UtcNow,
            GlobalExceptionHandler.ResolveTraceId(HttpContext));
}
