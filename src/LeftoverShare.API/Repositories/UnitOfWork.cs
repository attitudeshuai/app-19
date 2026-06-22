using LeftoverShare.API.Data;
using Microsoft.EntityFrameworkCore.Storage;

namespace LeftoverShare.API.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _context;

    public IUserRepository Users { get; private set; }
    public ISharePostRepository SharePosts { get; private set; }
    public IReservationRepository Reservations { get; private set; }
    public IPickupCodeRepository PickupCodes { get; private set; }
    public IKarmaPointRepository KarmaPoints { get; private set; }
    public INotificationRepository Notifications { get; private set; }
    public IScheduledTaskLogRepository ScheduledTaskLogs { get; private set; }
    public IDeletedEntitySnapshotRepository DeletedEntitySnapshots { get; private set; }
    public IFoodCategoryRepository FoodCategories { get; private set; }
    public IAllergenTagRepository AllergenTags { get; private set; }
    public IPostTagRepository PostTags { get; private set; }
    public IReviewRepository Reviews { get; private set; }
    public IPublisherReputationRepository PublisherReputations { get; private set; }

    public UnitOfWork(AppDbContext context)
    {
        _context = context;
        Users = new UserRepository(_context);
        SharePosts = new SharePostRepository(_context);
        Reservations = new ReservationRepository(_context);
        PickupCodes = new PickupCodeRepository(_context);
        KarmaPoints = new KarmaPointRepository(_context);
        Notifications = new NotificationRepository(_context);
        ScheduledTaskLogs = new ScheduledTaskLogRepository(_context);
        DeletedEntitySnapshots = new DeletedEntitySnapshotRepository(_context);
        FoodCategories = new FoodCategoryRepository(_context);
        AllergenTags = new AllergenTagRepository(_context);
        PostTags = new PostTagRepository(_context);
        Reviews = new ReviewRepository(_context);
        PublisherReputations = new PublisherReputationRepository(_context);
    }

    public async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }

    /// <summary>
    /// 开始事务
    /// 业务意图：在高并发场景下执行多个数据库操作时，
    /// 使用事务确保数据一致性
    /// </summary>
    public async Task<IDbContextTransaction> BeginTransactionAsync()
    {
        return await _context.Database.BeginTransactionAsync();
    }

    /// <summary>
    /// 提交事务
    /// </summary>
    public async Task CommitTransactionAsync()
    {
        await _context.Database.CommitTransactionAsync();
    }

    /// <summary>
    /// 回滚事务
    /// </summary>
    public async Task RollbackTransactionAsync()
    {
        await _context.Database.RollbackTransactionAsync();
    }
}
