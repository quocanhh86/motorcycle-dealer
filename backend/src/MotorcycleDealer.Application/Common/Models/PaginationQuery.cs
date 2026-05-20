using FluentValidation;

namespace MotorcycleDealer.Application.Common.Models;

public record PaginationQuery(int PageNumber = 1, int PageSize = 20)
{
    public const int DefaultPageNumber = 1;
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 100;

    public int NormalizedPageNumber => PageNumber < 1 ? DefaultPageNumber : PageNumber;
    public int NormalizedPageSize => Math.Clamp(PageSize, 1, MaxPageSize);
    public int SkipCount => (NormalizedPageNumber - 1) * NormalizedPageSize;
}

public sealed class PaginationQueryValidator : AbstractValidator<PaginationQuery>
{
    public PaginationQueryValidator()
    {
        RuleFor(x => x.PageNumber)
            .GreaterThanOrEqualTo(1)
            .WithMessage("PageNumber must be greater than or equal to 1.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, PaginationQuery.MaxPageSize)
            .WithMessage($"PageSize must be between 1 and {PaginationQuery.MaxPageSize}.");
    }
}
