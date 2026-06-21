namespace LeftoverShare.API.Repositories;

public interface IUnitOfWork
{
    IUserRepository Users { get; }
    ISharePostRepository SharePosts { get; }
    IReservationRepository Reservations { get; }
    IPickupCodeRepository PickupCodes { get; }
    IKarmaPointRepository KarmaPoints { get; }
    Task<int> SaveChangesAsync();
}
