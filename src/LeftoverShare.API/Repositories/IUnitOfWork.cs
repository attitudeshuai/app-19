namespace LeftoverShare.API.Repositories;

public interface IUnitOfWork
{
    IUserRepository Users { get; }
    ISharePostRepository SharePosts { get; }
    IReservationRepository Reservations { get; }
    IPickupCodeRepository PickupCodes { get; }
    IKarmaPointRepository KarmaPoints { get; }
    INotificationRepository Notifications { get; }
    IScheduledTaskLogRepository ScheduledTaskLogs { get; }
    IDeletedEntitySnapshotRepository DeletedEntitySnapshots { get; }
    IFoodCategoryRepository FoodCategories { get; }
    IAllergenTagRepository AllergenTags { get; }
    IPostTagRepository PostTags { get; }
    IReviewRepository Reviews { get; }
    IPublisherReputationRepository PublisherReputations { get; }
    Task<int> SaveChangesAsync();
}
