using Microsoft.EntityFrameworkCore.Storage;

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

    /// <summary>
    /// 保存更改
    /// </summary>
    Task<int> SaveChangesAsync();

    /// <summary>
    /// 开始事务
    /// 业务意图：在高并发场景下执行多个数据库操作时，
    /// 使用事务确保数据一致性
    /// </summary>
    /// <returns>事务对象</returns>
    Task<IDbContextTransaction> BeginTransactionAsync();

    /// <summary>
    /// 提交事务
    /// </summary>
    Task CommitTransactionAsync();

    /// <summary>
    /// 回滚事务
    /// </summary>
    Task RollbackTransactionAsync();
}
