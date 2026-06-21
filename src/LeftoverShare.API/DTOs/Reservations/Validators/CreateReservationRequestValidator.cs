using FluentValidation;
using LeftoverShare.API.DTOs.Reservations;

namespace LeftoverShare.API.DTOs.Reservations.Validators;

public class CreateReservationRequestValidator : AbstractValidator<CreateReservationRequest>
{
    public CreateReservationRequestValidator()
    {
        RuleFor(x => x.PostId)
            .GreaterThan(0).WithMessage("帖子ID必须大于0");

        RuleFor(x => x.Note)
            .MaximumLength(500).WithMessage("备注长度不能超过500个字符")
            .When(x => !string.IsNullOrEmpty(x.Note));
    }
}
