using LeftoverShare.API.Entities;

namespace LeftoverShare.API.Repositories;

public interface IPickupCodeRepository : IRepository<PickupCode>
{
    Task<PickupCode?> GetByReservationIdAsync(int reservationId);
    Task<PickupCode?> GetByCodeAsync(string code);
    Task<IEnumerable<PickupCode>> GetUnusedExpiredCodesAsync(DateTime now, TimeSpan unusedThreshold);
}
