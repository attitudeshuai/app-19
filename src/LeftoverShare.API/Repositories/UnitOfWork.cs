using LeftoverShare.API.Data;

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
    }

    public async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }
}
