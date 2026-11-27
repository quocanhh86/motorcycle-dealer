using FluentValidation;
using MotorcycleDealer.Application.Common.Models;

namespace MotorcycleDealer.Application.Features.Notifications;

public sealed class NotificationListQueryValidator : AbstractValidator<NotificationListQuery>
{
    public NotificationListQueryValidator()
    {
        RuleFor(x => x.PageNumber)
            .GreaterThanOrEqualTo(1)
            .WithMessage("PageNumber must be greater than or equal to 1.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, PaginationQuery.MaxPageSize)
            .WithMessage($"PageSize must be between 1 and {PaginationQuery.MaxPageSize}.");
    }
}
