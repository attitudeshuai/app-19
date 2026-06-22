using FluentValidation;

namespace LeftoverShare.API.DTOs.PostTags.Validators;

/// <summary>
/// 创建帖子标签请求验证器
/// </summary>
public class CreatePostTagRequestValidator : AbstractValidator<CreatePostTagRequest>
{
    public CreatePostTagRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("标签名称不能为空")
            .MaximumLength(30).WithMessage("标签名称长度不能超过30个字符");

        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("标签编码不能为空")
            .MaximumLength(30).WithMessage("标签编码长度不能超过30个字符")
            .Matches(@"^[a-zA-Z][a-zA-Z0-9_]*$").WithMessage("标签编码只能包含字母、数字和下划线，且必须以字母开头");

        RuleFor(x => x.Color)
            .Matches(@"^#([A-Fa-f0-9]{6}|[A-Fa-f0-9]{3})$")
            .When(x => !string.IsNullOrEmpty(x.Color))
            .WithMessage("颜色必须是有效的十六进制颜色值（如#FFFFFF或#FFF）");

        RuleFor(x => x.IconUrl)
            .MaximumLength(500).When(x => !string.IsNullOrEmpty(x.IconUrl)).WithMessage("图标URL长度不能超过500个字符");

        RuleFor(x => x.Description)
            .MaximumLength(200).When(x => !string.IsNullOrEmpty(x.Description)).WithMessage("描述长度不能超过200个字符");

        RuleFor(x => x.SortOrder)
            .GreaterThanOrEqualTo(0).WithMessage("排序权重不能为负数");
    }
}
