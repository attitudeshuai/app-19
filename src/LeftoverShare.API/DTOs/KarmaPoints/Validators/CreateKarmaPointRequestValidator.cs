using FluentValidation;
using LeftoverShare.API.DTOs.KarmaPoints;

namespace LeftoverShare.API.DTOs.KarmaPoints.Validators;

public class CreateKarmaPointRequestValidator : AbstractValidator<CreateKarmaPointRequest>
{
    public CreateKarmaPointRequestValidator()
    {
        RuleFor(x => x.UserId)
            .GreaterThan(0).WithMessage("用户ID必须大于0");

        RuleFor(x => x.Points)
            .NotEqual(0).WithMessage("积分不能为0")
            .GreaterThanOrEqualTo(-1000).WithMessage("积分不能小于-1000")
            .LessThanOrEqualTo(1000).WithMessage("积分不能大于1000");

        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("原因不能为空")
            .Length(1, 200).WithMessage("原因长度必须在1到200个字符之间");

        RuleFor(x => x.RelatedId)
            .GreaterThan(0).WithMessage("关联ID必须大于0")
            .When(x => x.RelatedId.HasValue);
    }
}
