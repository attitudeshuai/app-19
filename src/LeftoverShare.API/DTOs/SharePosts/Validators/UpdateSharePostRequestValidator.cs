using FluentValidation;
using LeftoverShare.API.DTOs.SharePosts;

namespace LeftoverShare.API.DTOs.SharePosts.Validators;

public class UpdateSharePostRequestValidator : AbstractValidator<UpdateSharePostRequest>
{
    public UpdateSharePostRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("标题不能为空")
            .Length(1, 200).WithMessage("标题长度必须在1到200个字符之间");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("描述不能为空")
            .Length(1, 2000).WithMessage("描述长度必须在1到2000个字符之间");

        RuleFor(x => x.FoodType)
            .NotEmpty().WithMessage("食物类型不能为空")
            .Length(1, 50).WithMessage("食物类型长度必须在1到50个字符之间");

        RuleFor(x => x.Quantity)
            .GreaterThan(0).WithMessage("数量必须大于0");

        RuleFor(x => x.PickupAddress)
            .NotEmpty().WithMessage("取餐地址不能为空")
            .Length(1, 500).WithMessage("取餐地址长度必须在1到500个字符之间");

        RuleFor(x => x.Latitude)
            .InclusiveBetween(-90.0, 90.0).WithMessage("纬度必须在-90到90之间");

        RuleFor(x => x.Longitude)
            .InclusiveBetween(-180.0, 180.0).WithMessage("经度必须在-180到180之间");

        RuleFor(x => x.AvailableUntil)
            .GreaterThan(DateTime.Now).WithMessage("可领取时间必须晚于当前时间");

        RuleForEach(x => x.Photos)
            .MaximumLength(500).WithMessage("照片URL长度不能超过500个字符")
            .When(x => x.Photos != null);
    }
}
