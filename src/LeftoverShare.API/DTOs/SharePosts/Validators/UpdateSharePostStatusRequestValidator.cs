using FluentValidation;
using LeftoverShare.API.DTOs.SharePosts;

namespace LeftoverShare.API.DTOs.SharePosts.Validators;

public class UpdateSharePostStatusRequestValidator : AbstractValidator<UpdateSharePostStatusRequest>
{
    public UpdateSharePostStatusRequestValidator()
    {
        RuleFor(x => x.Status)
            .NotEmpty().WithMessage("状态不能为空")
            .Must(x => x == "Active" || x == "Reserved" || x == "Completed" || x == "Cancelled" || x == "Expired")
            .WithMessage("状态必须是 Active、Reserved、Completed、Cancelled 或 Expired");
    }
}
