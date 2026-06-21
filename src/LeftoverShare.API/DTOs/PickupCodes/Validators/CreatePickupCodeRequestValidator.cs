using FluentValidation;
using LeftoverShare.API.DTOs.PickupCodes;

namespace LeftoverShare.API.DTOs.PickupCodes.Validators;

public class CreatePickupCodeRequestValidator : AbstractValidator<CreatePickupCodeRequest>
{
    public CreatePickupCodeRequestValidator()
    {
        RuleFor(x => x.ReservationId)
            .GreaterThan(0).WithMessage("预订ID必须大于0");
    }
}
