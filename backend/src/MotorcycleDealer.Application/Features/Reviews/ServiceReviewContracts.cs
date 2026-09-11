using System.Text.Json;
using System.Text.Json.Serialization;
using MotorcycleDealer.Application.Common.Models;

namespace MotorcycleDealer.Application.Features.Reviews;

public static class ServiceReviewConstants
{
    public const int StandardReviewBonusPoints = 25;
    public const int MinRating = 1;
    public const int MaxRating = 5;
    public const int MaxQualityTagsCount = 8;
    public const int MaxQualityTagsLength = 200;
    public const int MaxCommentLength = 1000;
    public const int MinStaffResponseLength = 5;
    public const int MaxStaffResponseLength = 1000;
}

public sealed record ServiceReviewDto(
    Guid Id,
    Guid ReviewId,
    string ReviewNumber,
    Guid ServiceInvoiceId,
    string InvoiceNumber,
    Guid ServiceBookingId,
    string BookingNumber,
    string ServiceType,
    Guid CustomerId,
    string CustomerFullName,
    string CustomerEmail,
    Guid DealerId,
    string DealerCode,
    string DealerName,
    string DealerCity,
    Guid CustomerVehicleId,
    Guid MotorcycleModelId,
    string Vin,
    string MotorcycleModelName,
    string? LicensePlate,
    int Rating,
    string? QualityTags,
    IReadOnlyList<string> QualityTagList,
    string? Comment,
    int BonusPointsAwarded,
    int? AvailablePointsAfter,
    int? LifetimeQualifyingPointsAfter,
    string? StaffResponse,
    Guid? RespondedByUserId,
    string? RespondedByStaffName,
    DateTimeOffset? RespondedAtUtc,
    bool IsAnswered,
    string RowVersion,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? UpdatedAtUtc);

public sealed record ServiceReviewPageDto(
    IReadOnlyList<ServiceReviewDto> Items,
    PaginationMetadata Pagination,
    decimal AverageRating,
    int TotalReviewsCount,
    int FiveStarCount,
    int UnansweredCount,
    int TotalBonusPointsAwarded);

public sealed record ServiceReviewListQuery(
    Guid? DealerId = null,
    int? Rating = null,
    int? MinRating = null,
    bool? UnansweredOnly = null,
    string? Search = null,
    int PageNumber = PaginationQuery.DefaultPageNumber,
    int PageSize = PaginationQuery.DefaultPageSize) : PaginationQuery(PageNumber, PageSize);

public sealed class FlexibleQualityTagsJsonConverter : JsonConverter<string?>
{
    public override string? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return null;
        }

        if (reader.TokenType == JsonTokenType.String)
        {
            return reader.GetString();
        }

        if (reader.TokenType == JsonTokenType.StartArray)
        {
            var items = new List<string>();
            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndArray)
                {
                    break;
                }

                if (reader.TokenType == JsonTokenType.String)
                {
                    var item = reader.GetString();
                    if (!string.IsNullOrWhiteSpace(item))
                    {
                        items.Add(item.Trim());
                    }
                }
                else
                {
                    reader.Skip();
                }
            }

            return items.Count == 0 ? null : string.Join(',', items);
        }

        throw new JsonException("QualityTags must be a comma-separated string or an array of tag strings.");
    }

    public override void Write(Utf8JsonWriter writer, string? value, JsonSerializerOptions options)
    {
        if (value is null)
        {
            writer.WriteNullValue();
        }
        else
        {
            writer.WriteStringValue(value);
        }
    }
}

public sealed record CreateServiceReviewRequest
{
    public Guid ServiceInvoiceId { get; init; }
    public Guid? InvoiceId { get; init; }
    public int Rating { get; init; }

    [JsonConverter(typeof(FlexibleQualityTagsJsonConverter))]
    public string? QualityTags { get; init; }

    public IReadOnlyList<string>? QualityTagList { get; init; }
    public IReadOnlyList<string>? Tags { get; init; }
    public string? Comment { get; init; }

    public Guid ResolveEffectiveInvoiceId() =>
        ServiceInvoiceId != Guid.Empty
            ? ServiceInvoiceId
            : (InvoiceId is { } id && id != Guid.Empty ? id : Guid.Empty);

    public string? ResolveRawQualityTags()
    {
        if (!string.IsNullOrWhiteSpace(QualityTags))
        {
            return QualityTags.Trim();
        }

        if (QualityTagList is { Count: > 0 })
        {
            return string.Join(',', QualityTagList);
        }

        if (Tags is { Count: > 0 })
        {
            return string.Join(',', Tags);
        }

        return null;
    }
}

public sealed record RespondToServiceReviewRequest
{
    public string? StaffResponse { get; init; }
    public string? Response { get; init; }
    public string? RowVersion { get; init; }

    public string ResolveEffectiveStaffResponse() =>
        !string.IsNullOrWhiteSpace(StaffResponse)
            ? StaffResponse.Trim()
            : (!string.IsNullOrWhiteSpace(Response) ? Response.Trim() : string.Empty);
}

public interface IServiceReviewService
{
    Task<ServiceReviewDto> CreateServiceReviewAsync(
        CreateServiceReviewRequest request,
        CancellationToken cancellationToken = default);

    Task<PagedResult<ServiceReviewDto>> ListMyServiceReviewsAsync(
        ServiceReviewListQuery query,
        CancellationToken cancellationToken = default);

    Task<ServiceReviewPageDto> ListDealerServiceReviewsAsync(
        ServiceReviewListQuery query,
        CancellationToken cancellationToken = default);

    Task<ServiceReviewDto?> GetServiceReviewByInvoiceIdAsync(
        Guid invoiceId,
        CancellationToken cancellationToken = default);

    Task<ServiceReviewDto> RespondToServiceReviewAsync(
        Guid reviewId,
        RespondToServiceReviewRequest request,
        CancellationToken cancellationToken = default);
}
