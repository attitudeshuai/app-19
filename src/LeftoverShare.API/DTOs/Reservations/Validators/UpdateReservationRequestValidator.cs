using FluentValidation;
using LeftoverShare.API.DTOs.Reservations;

namespace LeftoverShare.API.DTOs.Reservations.Validators;

public class UpdateReservationRequestValidator : AbstractValidator<UpdateReservationRequest>
{
    public UpdateReservationRequestValidator()
    {
        RuleFor(x => x.Note)
            .MaximumLength(500).WithMessage("备注长度不能超过500个字符")
            .When(x => !string.IsNullOrEmpty(x.Note));
    }
}
