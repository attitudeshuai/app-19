using FluentValidation;
using LeftoverShare.API.DTOs.RecycleBin;

namespace LeftoverShare.API.DTOs.RecycleBin.Validators;

/// <summary>
/// 恢复实体请求验证器
/// </summary>
public class RestoreRequestValidator : AbstractValidator<RestoreRequest>
{
    public RestoreRequestValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0)
            .WithMessage("ID必须大于0");
    }
}
