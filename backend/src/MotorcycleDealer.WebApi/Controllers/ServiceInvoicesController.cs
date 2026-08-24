using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MotorcycleDealer.Application.Common.Interfaces;
using MotorcycleDealer.Application.Common.Models;
using MotorcycleDealer.Application.Features.Auth;
using MotorcycleDealer.Application.Features.Invoices;
using MotorcycleDealer.Application.Features.Showroom;
using MotorcycleDealer.Domain.Constants;
using MotorcycleDealer.WebApi.Middleware;
using MotorcycleDealer.WebApi.Security;

namespace MotorcycleDealer.WebApi.Controllers;

[ApiController]
[Route("api/v1/service-invoices")]
public sealed class ServiceInvoicesController : ControllerBase
{
    private readonly IServiceInvoiceService _serviceInvoiceService;
    private readonly IDateTimeProvider _dateTimeProvider;

    public ServiceInvoicesController(
        IServiceInvoiceService serviceInvoiceService,
        IDateTimeProvider dateTimeProvider)
    {
        _serviceInvoiceService = serviceInvoiceService ?? throw new ArgumentNullException(nameof(serviceInvoiceService));
        _dateTimeProvider = dateTimeProvider ?? throw new ArgumentNullException(nameof(dateTimeProvider));
    }

    [HttpGet]
    [Authorize]
    [ProducesResponseType(typeof(PagedApiEnvelope<ServiceInvoiceDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedApiEnvelope<ServiceInvoiceDto>>> ListServiceInvoicesAsync(
        [FromQuery] int pageNumber = PaginationQuery.DefaultPageNumber,
        [FromQuery] int pageSize = PaginationQuery.DefaultPageSize,
        [FromQuery] Guid? dealerId = null,
        [FromQuery] Guid? customerId = null,
        [FromQuery] Guid? bookingId = null,
        [FromQuery] Guid? serviceBookingId = null,
        [FromQuery] string? search = null,
        CancellationToken cancellationToken = default)
    {
        var query = new ServiceInvoiceListQuery(
            PageNumber: pageNumber,
            PageSize: pageSize,
            DealerId: dealerId,
            CustomerId: customerId,
            BookingId: bookingId,
            ServiceBookingId: serviceBookingId,
            Search: search);

        var result = await _serviceInvoiceService.ListServiceInvoicesAsync(
            query,
            forceOwnCustomerOnly: false,
            cancellationToken);

        return Ok(WrapPagedSuccess(result));
    }

    [HttpGet("my")]
    [HasPermission(Permissions.Invoices.ViewOwn)]
    [ProducesResponseType(typeof(PagedApiEnvelope<ServiceInvoiceDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedApiEnvelope<ServiceInvoiceDto>>> ListMyServiceInvoicesAsync(
        [FromQuery] int pageNumber = PaginationQuery.DefaultPageNumber,
        [FromQuery] int pageSize = PaginationQuery.DefaultPageSize,
        [FromQuery] Guid? bookingId = null,
        [FromQuery] Guid? serviceBookingId = null,
        [FromQuery] string? search = null,
        CancellationToken cancellationToken = default)
    {
        var query = new ServiceInvoiceListQuery(
            PageNumber: pageNumber,
            PageSize: pageSize,
            BookingId: bookingId,
            ServiceBookingId: serviceBookingId,
            Search: search);

        var result = await _serviceInvoiceService.ListServiceInvoicesAsync(
            query,
            forceOwnCustomerOnly: true,
            cancellationToken);

        return Ok(WrapPagedSuccess(result));
    }

    [HttpGet("{invoiceId:guid}")]
    [Authorize]
    [ProducesResponseType(typeof(ApiEnvelope<ServiceInvoiceDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiEnvelope<ServiceInvoiceDto>>> GetServiceInvoiceByIdAsync(
        [FromRoute] Guid invoiceId,
        CancellationToken cancellationToken = default)
    {
        var invoice = await _serviceInvoiceService.GetServiceInvoiceByIdAsync(invoiceId, cancellationToken);
        return Ok(WrapSuccess(invoice));
    }

    [HttpGet("{invoiceId:guid}/receipt")]
    [Authorize]
    [ProducesResponseType(typeof(ApiEnvelope<PrintableServiceReceiptDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiEnvelope<PrintableServiceReceiptDto>>> GetPrintableReceiptAsync(
        [FromRoute] Guid invoiceId,
        CancellationToken cancellationToken = default)
    {
        var receipt = await _serviceInvoiceService.GetPrintableReceiptAsync(invoiceId, cancellationToken);
        return Ok(WrapSuccess(receipt));
    }

    [HttpPost]
    [HasPermission(Permissions.Invoices.CreateDealer)]
    [ProducesResponseType(typeof(ApiEnvelope<ServiceInvoiceDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<ApiEnvelope<ServiceInvoiceDto>>> CreateServiceInvoiceAsync(
        [FromBody] CreateServiceInvoiceRequest request,
        CancellationToken cancellationToken = default)
    {
        var created = await _serviceInvoiceService.CreateServiceInvoiceAsync(request, cancellationToken);
        Response.Headers.Location = $"/api/v1/service-invoices/{created.InvoiceId}";
        return StatusCode(StatusCodes.Status201Created, WrapSuccess(created));
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
