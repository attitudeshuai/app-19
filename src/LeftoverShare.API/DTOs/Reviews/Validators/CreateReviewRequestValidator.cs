using FluentValidation;
using LeftoverShare.API.DTOs.Reviews;

namespace LeftoverShare.API.DTOs.Reviews.Validators;

public class CreateReviewRequestValidator : AbstractValidator<CreateReviewRequest>
{
    public CreateReviewRequestValidator()
    {
        RuleFor(x => x.ReservationId)
            .GreaterThan(0).WithMessage("预约ID必须大于0");

        RuleFor(x => x.Rating)
            .InclusiveBetween(1, 5).WithMessage("评分必须在1-5之间");

        RuleFor(x => x.Comment)
            .MaximumLength(500).WithMessage("评价内容长度不能超过500个字符")
            .When(x => !string.IsNullOrEmpty(x.Comment));
    }
}
