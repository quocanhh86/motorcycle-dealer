using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MotorcycleDealer.Application.Common.Interfaces;
using MotorcycleDealer.Application.Common.Models;
using MotorcycleDealer.Application.Features.Auth;
using MotorcycleDealer.Application.Features.Showroom;
using MotorcycleDealer.Application.Features.TestDrives;
using MotorcycleDealer.Domain.Constants;
using MotorcycleDealer.WebApi.Middleware;
using MotorcycleDealer.WebApi.Security;

namespace MotorcycleDealer.WebApi.Controllers;

[ApiController]
[Route("api/v1/test-drives")]
public sealed class TestDrivesController : ControllerBase
{
    private readonly ITestDriveService _testDriveService;
    private readonly IDateTimeProvider _dateTimeProvider;

    public TestDrivesController(
        ITestDriveService testDriveService,
        IDateTimeProvider dateTimeProvider)
    {
        _testDriveService = testDriveService ?? throw new ArgumentNullException(nameof(testDriveService));
        _dateTimeProvider = dateTimeProvider ?? throw new ArgumentNullException(nameof(dateTimeProvider));
    }

    [HttpGet("my")]
    [HasPermission(Permissions.TestDrives.ViewOwn)]
    [ProducesResponseType(typeof(PagedApiEnvelope<TestDriveBookingDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedApiEnvelope<TestDriveBookingDto>>> ListMyTestDrivesAsync(
        [FromQuery] string? status = null,
        [FromQuery] Guid? dealerId = null,
        [FromQuery] DateOnly? preferredDate = null,
        [FromQuery] string? search = null,
        [FromQuery] int? page = null,
        [FromQuery] int pageNumber = PaginationQuery.DefaultPageNumber,
        [FromQuery] int pageSize = PaginationQuery.DefaultPageSize,
        CancellationToken cancellationToken = default)
    {
        var effectivePageNumber = page ?? pageNumber;
        var result = await _testDriveService.ListTestDrivesAsync(
            new TestDriveListQuery(
                DealerId: dealerId,
                PreferredDate: preferredDate,
                Status: status,
                Search: search,
                PageNumber: effectivePageNumber,
                PageSize: pageSize),
            forceOwnCustomerOnly: true,
            cancellationToken);

        return Ok(WrapPagedSuccess(result));
    }

    [HttpGet]
    [HasPermission(Permissions.TestDrives.ViewDealer)]
    [ProducesResponseType(typeof(PagedApiEnvelope<TestDriveBookingDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedApiEnvelope<TestDriveBookingDto>>> ListDealerTestDrivesAsync(
        [FromQuery] Guid? dealerId = null,
        [FromQuery] DateOnly? preferredDate = null,
        [FromQuery] string? status = null,
        [FromQuery] string? search = null,
        [FromQuery] int? page = null,
        [FromQuery] int pageNumber = PaginationQuery.DefaultPageNumber,
        [FromQuery] int pageSize = PaginationQuery.DefaultPageSize,
        CancellationToken cancellationToken = default)
    {
        var effectivePageNumber = page ?? pageNumber;
        var result = await _testDriveService.ListTestDrivesAsync(
            new TestDriveListQuery(
                DealerId: dealerId,
                PreferredDate: preferredDate,
                Status: status,
                Search: search,
                PageNumber: effectivePageNumber,
                PageSize: pageSize),
            forceOwnCustomerOnly: false,
            cancellationToken);

        return Ok(WrapPagedSuccess(result));
    }

    [HttpGet("{testDriveId:guid}")]
    [Authorize]
    [ProducesResponseType(typeof(ApiEnvelope<TestDriveBookingDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiEnvelope<TestDriveBookingDto>>> GetTestDriveByIdAsync(
        [FromRoute] Guid testDriveId,
        CancellationToken cancellationToken = default)
    {
        var dto = await _testDriveService.GetTestDriveByIdAsync(testDriveId, cancellationToken);
        return Ok(WrapSuccess(dto));
    }

    [HttpPost]
    [HasPermission(Permissions.TestDrives.Create)]
    [ProducesResponseType(typeof(ApiEnvelope<TestDriveBookingDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<ApiEnvelope<TestDriveBookingDto>>> CreateTestDriveAsync(
        [FromBody] CreateTestDriveRequest request,
        CancellationToken cancellationToken = default)
    {
        var created = await _testDriveService.CreateTestDriveAsync(request, cancellationToken);
        Response.Headers.Location = $"/api/v1/test-drives/{created.TestDriveId}";
        return StatusCode(StatusCodes.Status201Created, WrapSuccess(created));
    }

    [HttpPost("{testDriveId:guid}/cancel")]
    [Authorize]
    [ProducesResponseType(typeof(ApiEnvelope<TestDriveBookingDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<ApiEnvelope<TestDriveBookingDto>>> CancelTestDriveAsync(
        [FromRoute] Guid testDriveId,
        [FromBody] CancelTestDriveRequest? request = null,
        CancellationToken cancellationToken = default)
    {
        var cancelled = await _testDriveService.CancelTestDriveAsync(testDriveId, request, cancellationToken);
        return Ok(WrapSuccess(cancelled));
    }

    [HttpPatch("{testDriveId:guid}/status")]
    [HasPermission(Permissions.TestDrives.UpdateStatus)]
    [ProducesResponseType(typeof(ApiEnvelope<TestDriveBookingDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<ApiEnvelope<TestDriveBookingDto>>> UpdateTestDriveStatusAsync(
        [FromRoute] Guid testDriveId,
        [FromBody] UpdateTestDriveStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        var updated = await _testDriveService.UpdateTestDriveStatusAsync(testDriveId, request, cancellationToken);
        return Ok(WrapSuccess(updated));
    }

    private ApiEnvelope<T> WrapSuccess<T>(T data) =>
        ApiEnvelope.Success(
            data,
            _dateTimeProvider.UtcNow,
            GlobalExceptionHandler.ResolveTraceId(HttpContext));

    private PagedApiEnvelope<T> WrapPagedSuccess<T>(PagedResult<T> result) =>
        PagedApiEnvelope.Success(
            result,
            _dateTimeProvider.UtcNow,
            GlobalExceptionHandler.ResolveTraceId(HttpContext));
}
