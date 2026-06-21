using FluentValidation;
using LeftoverShare.API.DTOs.Auth;

namespace LeftoverShare.API.DTOs.Auth.Validators;

public class UpdateProfileRequestValidator : AbstractValidator<UpdateProfileRequest>
{
    public UpdateProfileRequestValidator()
    {
        RuleFor(x => x.Username)
            .NotEmpty().WithMessage("用户名不能为空")
            .Length(3, 50).WithMessage("用户名长度必须在3到50个字符之间")
            .Matches(@"^[a-zA-Z0-9_]+$").WithMessage("用户名只能包含字母、数字和下划线");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("邮箱不能为空")
            .EmailAddress().WithMessage("邮箱格式不正确");

        RuleFor(x => x.Avatar)
            .MaximumLength(500).WithMessage("头像URL长度不能超过500个字符")
            .When(x => !string.IsNullOrEmpty(x.Avatar));
    }
}
