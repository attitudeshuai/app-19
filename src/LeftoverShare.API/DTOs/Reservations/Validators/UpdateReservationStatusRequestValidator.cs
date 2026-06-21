using FluentValidation;
using LeftoverShare.API.DTOs.Reservations;

namespace LeftoverShare.API.DTOs.Reservations.Validators;

public class UpdateReservationStatusRequestValidator : AbstractValidator<UpdateReservationStatusRequest>
{
    public UpdateReservationStatusRequestValidator()
    {
        RuleFor(x => x.Status)
            .NotEmpty().WithMessage("状态不能为空")
            .Must(x => x == "Pending" || x == "Confirmed" || x == "Completed" || x == "Cancelled")
            .WithMessage("状态必须是 Pending、Confirmed、Completed 或 Cancelled");
    }
}
