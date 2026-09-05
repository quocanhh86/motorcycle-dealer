using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MotorcycleDealer.Application.Common.Interfaces;
using MotorcycleDealer.Application.Common.Models;
using MotorcycleDealer.Application.Features.Auth;
using MotorcycleDealer.Application.Features.Reviews;
using MotorcycleDealer.Application.Features.Showroom;
using MotorcycleDealer.Domain.Constants;
using MotorcycleDealer.WebApi.Middleware;
using MotorcycleDealer.WebApi.Security;

namespace MotorcycleDealer.WebApi.Controllers;

[ApiController]
[Route("api/v1/service-reviews")]
public sealed class ServiceReviewsController : ControllerBase
{
    private readonly IServiceReviewService _serviceReviewService;
    private readonly IDateTimeProvider _dateTimeProvider;

    public ServiceReviewsController(
        IServiceReviewService serviceReviewService,
        IDateTimeProvider dateTimeProvider)
    {
        _serviceReviewService = serviceReviewService ?? throw new ArgumentNullException(nameof(serviceReviewService));
        _dateTimeProvider = dateTimeProvider ?? throw new ArgumentNullException(nameof(dateTimeProvider));
    }

    [HttpGet("my")]
    [HasPermission(Permissions.Reviews.ViewOwn)]
    [ProducesResponseType(typeof(PagedApiEnvelope<ServiceReviewDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedApiEnvelope<ServiceReviewDto>>> ListMyServiceReviewsAsync(
        [FromQuery] Guid? dealerId = null,
        [FromQuery] int? rating = null,
        [FromQuery] int? minRating = null,
        [FromQuery] bool? unansweredOnly = null,
        [FromQuery] int? page = null,
        [FromQuery] int pageNumber = PaginationQuery.DefaultPageNumber,
        [FromQuery] int pageSize = PaginationQuery.DefaultPageSize,
        CancellationToken cancellationToken = default)
    {
        var effectivePageNumber = page ?? pageNumber;
        var result = await _serviceReviewService.ListMyServiceReviewsAsync(
            new ServiceReviewListQuery(
                DealerId: dealerId,
                Rating: rating,
                MinRating: minRating,
                UnansweredOnly: unansweredOnly,
                PageNumber: effectivePageNumber,
                PageSize: pageSize),
            cancellationToken);

        return Ok(WrapPagedSuccess(result));
    }

    [HttpGet]
    [HasPermission(Permissions.Reviews.ViewDealer)]
    [ProducesResponseType(typeof(ApiEnvelope<ServiceReviewPageDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiEnvelope<ServiceReviewPageDto>>> ListDealerServiceReviewsAsync(
        [FromQuery] Guid? dealerId = null,
        [FromQuery] int? rating = null,
        [FromQuery] int? minRating = null,
        [FromQuery] bool? unansweredOnly = null,
        [FromQuery] string? search = null,
        [FromQuery] int? page = null,
        [FromQuery] int pageNumber = PaginationQuery.DefaultPageNumber,
        [FromQuery] int pageSize = PaginationQuery.DefaultPageSize,
        CancellationToken cancellationToken = default)
    {
        var effectivePageNumber = page ?? pageNumber;
        var pageDto = await _serviceReviewService.ListDealerServiceReviewsAsync(
            new ServiceReviewListQuery(
                DealerId: dealerId,
                Rating: rating,
                MinRating: minRating,
                UnansweredOnly: unansweredOnly,
                Search: search,
                PageNumber: effectivePageNumber,
                PageSize: pageSize),
            cancellationToken);

        return Ok(WrapSuccess(pageDto));
    }

    [HttpGet("by-invoice/{invoiceId:guid}")]
    [Authorize]
    [ProducesResponseType(typeof(ApiEnvelope<ServiceReviewDto?>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiEnvelope<ServiceReviewDto?>>> GetServiceReviewByInvoiceIdAsync(
        [FromRoute] Guid invoiceId,
        CancellationToken cancellationToken = default)
    {
        var dto = await _serviceReviewService.GetServiceReviewByInvoiceIdAsync(invoiceId, cancellationToken);
        return Ok(WrapSuccess(dto));
    }

    [HttpPost]
    [HasPermission(Permissions.Reviews.CreateOwn)]
    [ProducesResponseType(typeof(ApiEnvelope<ServiceReviewDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<ApiEnvelope<ServiceReviewDto>>> CreateServiceReviewAsync(
        [FromBody] CreateServiceReviewRequest request,
        CancellationToken cancellationToken = default)
    {
        var created = await _serviceReviewService.CreateServiceReviewAsync(request, cancellationToken);
        Response.Headers.Location = $"/api/v1/service-reviews/by-invoice/{created.ServiceInvoiceId}";
        return StatusCode(StatusCodes.Status201Created, WrapSuccess(created));
    }

    [HttpPatch("{reviewId:guid}/response")]
    [HasPermission(Permissions.Reviews.RespondDealer)]
    [ProducesResponseType(typeof(ApiEnvelope<ServiceReviewDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<ApiEnvelope<ServiceReviewDto>>> RespondToServiceReviewAsync(
        [FromRoute] Guid reviewId,
        [FromBody] RespondToServiceReviewRequest request,
        CancellationToken cancellationToken = default)
    {
        var updated = await _serviceReviewService.RespondToServiceReviewAsync(reviewId, request, cancellationToken);
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
