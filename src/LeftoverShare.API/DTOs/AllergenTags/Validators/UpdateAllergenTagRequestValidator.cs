using FluentValidation;

namespace LeftoverShare.API.DTOs.AllergenTags.Validators;

/// <summary>
/// 更新过敏原标签请求验证器
/// </summary>
public class UpdateAllergenTagRequestValidator : AbstractValidator<UpdateAllergenTagRequest>
{
    public UpdateAllergenTagRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("标签名称不能为空")
            .MaximumLength(50).WithMessage("标签名称长度不能超过50个字符");

        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("标签编码不能为空")
            .MaximumLength(50).WithMessage("标签编码长度不能超过50个字符")
            .Matches(@"^[a-zA-Z][a-zA-Z0-9_]*$").WithMessage("标签编码只能包含字母、数字和下划线，且必须以字母开头");

        RuleFor(x => x.IconUrl)
            .MaximumLength(500).When(x => !string.IsNullOrEmpty(x.IconUrl)).WithMessage("图标URL长度不能超过500个字符");

        RuleFor(x => x.SeverityLevel)
            .InclusiveBetween(1, 3).WithMessage("严重程度只能是1、2或3（1-轻度 2-中度 3-重度）");

        RuleFor(x => x.Description)
            .MaximumLength(500).When(x => !string.IsNullOrEmpty(x.Description)).WithMessage("说明长度不能超过500个字符");

        RuleFor(x => x.SortOrder)
            .GreaterThanOrEqualTo(0).WithMessage("排序权重不能为负数");
    }
}
