using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MotorcycleDealer.Application.Common.Interfaces;
using MotorcycleDealer.Application.Common.Models;
using MotorcycleDealer.Application.Features.Auth;
using MotorcycleDealer.Application.Features.Invoices;
using MotorcycleDealer.Application.Features.SalesOrders;
using MotorcycleDealer.Application.Features.Showroom;
using MotorcycleDealer.Domain.Constants;
using MotorcycleDealer.WebApi.Middleware;
using MotorcycleDealer.WebApi.Security;

namespace MotorcycleDealer.WebApi.Controllers;

[ApiController]
[Route("api/v1/sales-orders")]
public sealed class SalesOrdersController : ControllerBase
{
    private readonly ISalesOrderService _salesOrderService;
    private readonly IDateTimeProvider _dateTimeProvider;

    public SalesOrdersController(
        ISalesOrderService salesOrderService,
        IDateTimeProvider dateTimeProvider)
    {
        _salesOrderService = salesOrderService ?? throw new ArgumentNullException(nameof(salesOrderService));
        _dateTimeProvider = dateTimeProvider ?? throw new ArgumentNullException(nameof(dateTimeProvider));
    }

    [HttpGet]
    [Authorize]
    [ProducesResponseType(typeof(PagedApiEnvelope<SalesOrderDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedApiEnvelope<SalesOrderDto>>> ListSalesOrdersAsync(
        [FromQuery] int pageNumber = PaginationQuery.DefaultPageNumber,
        [FromQuery] int pageSize = PaginationQuery.DefaultPageSize,
        [FromQuery] Guid? dealerId = null,
        [FromQuery] Guid? customerId = null,
        [FromQuery] string? search = null,
        CancellationToken cancellationToken = default)
    {
        var query = new SalesOrderListQuery(
            PageNumber: pageNumber,
            PageSize: pageSize,
            DealerId: dealerId,
            CustomerId: customerId,
            Search: search);

        var result = await _salesOrderService.ListSalesOrdersAsync(
            query,
            forceOwnCustomerOnly: false,
            cancellationToken);

        return Ok(WrapPagedSuccess(result));
    }

    [HttpGet("my")]
    [HasPermission(Permissions.SalesOrders.ViewOwn)]
    [ProducesResponseType(typeof(PagedApiEnvelope<SalesOrderDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedApiEnvelope<SalesOrderDto>>> ListMySalesOrdersAsync(
        [FromQuery] int pageNumber = PaginationQuery.DefaultPageNumber,
        [FromQuery] int pageSize = PaginationQuery.DefaultPageSize,
        [FromQuery] string? search = null,
        CancellationToken cancellationToken = default)
    {
        var query = new SalesOrderListQuery(
            PageNumber: pageNumber,
            PageSize: pageSize,
            Search: search);

        var result = await _salesOrderService.ListSalesOrdersAsync(
            query,
            forceOwnCustomerOnly: true,
            cancellationToken);

        return Ok(WrapPagedSuccess(result));
    }

    [HttpGet("{orderId:guid}")]
    [Authorize]
    [ProducesResponseType(typeof(ApiEnvelope<SalesOrderDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiEnvelope<SalesOrderDto>>> GetSalesOrderByIdAsync(
        [FromRoute] Guid orderId,
        CancellationToken cancellationToken = default)
    {
        var order = await _salesOrderService.GetSalesOrderByIdAsync(orderId, cancellationToken);
        return Ok(WrapSuccess(order));
    }

    [HttpGet("{orderId:guid}/receipt")]
    [Authorize]
    [ProducesResponseType(typeof(ApiEnvelope<PrintableSalesOrderReceiptDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiEnvelope<PrintableSalesOrderReceiptDto>>> GetPrintableReceiptAsync(
        [FromRoute] Guid orderId,
        CancellationToken cancellationToken = default)
    {
        var receipt = await _salesOrderService.GetPrintableReceiptAsync(orderId, cancellationToken);
        return Ok(WrapSuccess(receipt));
    }

    [HttpPost]
    [HasPermission(Permissions.SalesOrders.Create)]
    [ProducesResponseType(typeof(ApiEnvelope<SalesOrderDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<ApiEnvelope<SalesOrderDto>>> CreateSalesOrderAsync(
        [FromBody] CreateSalesOrderRequest request,
        CancellationToken cancellationToken = default)
    {
        var created = await _salesOrderService.CreateSalesOrderAsync(request, cancellationToken);
        Response.Headers.Location = $"/api/v1/sales-orders/{created.SalesOrderId}";
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
