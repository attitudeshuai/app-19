using FluentValidation;
using LeftoverShare.API.DTOs.Auth;

namespace LeftoverShare.API.DTOs.Auth.Validators;

public class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(x => x.Username)
            .NotEmpty().WithMessage("用户名不能为空")
            .Length(3, 50).WithMessage("用户名长度必须在3到50个字符之间")
            .Matches(@"^[a-zA-Z0-9_]+$").WithMessage("用户名只能包含字母、数字和下划线");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("邮箱不能为空")
            .EmailAddress().WithMessage("邮箱格式不正确");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("密码不能为空")
            .Length(6, 100).WithMessage("密码长度必须在6到100个字符之间")
            .Matches(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).+$").WithMessage("密码必须包含至少一个小写字母、一个大写字母和一个数字");

        RuleFor(x => x.ConfirmPassword)
            .NotEmpty().WithMessage("确认密码不能为空")
            .Equal(x => x.Password).WithMessage("两次输入的密码不一致");
    }
}
