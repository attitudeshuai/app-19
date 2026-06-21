using FluentValidation;
using LeftoverShare.API.DTOs.Common;

namespace LeftoverShare.API.DTOs.Common.Validators;

public class PagedRequestValidator : AbstractValidator<PagedRequest>
{
    public PagedRequestValidator()
    {
        RuleFor(x => x.PageNumber)
            .GreaterThan(0).WithMessage("页码必须大于0");

        RuleFor(x => x.PageSize)
            .GreaterThan(0).WithMessage("每页大小必须大于0")
            .LessThanOrEqualTo(100).WithMessage("每页大小不能超过100");

        RuleFor(x => x.SortDirection)
            .Must(x => x == null || x.Equals("asc", StringComparison.OrdinalIgnoreCase) || x.Equals("desc", StringComparison.OrdinalIgnoreCase))
            .WithMessage("排序方向必须是 'asc' 或 'desc'");
    }
}
