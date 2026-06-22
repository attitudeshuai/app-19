using FluentValidation;

namespace LeftoverShare.API.DTOs.FoodCategories.Validators;

/// <summary>
/// 更新食物分类请求验证器
/// </summary>
public class UpdateFoodCategoryRequestValidator : AbstractValidator<UpdateFoodCategoryRequest>
{
    public UpdateFoodCategoryRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("分类名称不能为空")
            .MaximumLength(50).WithMessage("分类名称长度不能超过50个字符");

        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("分类编码不能为空")
            .MaximumLength(50).WithMessage("分类编码长度不能超过50个字符")
            .Matches(@"^[a-zA-Z][a-zA-Z0-9_]*$").WithMessage("分类编码只能包含字母、数字和下划线，且必须以字母开头");

        RuleFor(x => x.IconUrl)
            .MaximumLength(500).When(x => !string.IsNullOrEmpty(x.IconUrl)).WithMessage("图标URL长度不能超过500个字符");

        RuleFor(x => x.Description)
            .MaximumLength(200).When(x => !string.IsNullOrEmpty(x.Description)).WithMessage("描述长度不能超过200个字符");

        RuleFor(x => x.SortOrder)
            .GreaterThanOrEqualTo(0).WithMessage("排序权重不能为负数");
    }
}
