using LeftoverShare.API.Entities.Enums;

namespace LeftoverShare.Tests.Repositories;

public class SoftDeleteRepositoryTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly SharePostRepository _postRepository;
    private bool _disposed;

    public SoftDeleteRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new AppDbContext(options);
        _postRepository = new SharePostRepository(_context);
    }

    [Fact]
    public async Task GetByIdIgnoreFilterAsync_Should_ReturnSoftDeletedEntity()
    {
        var user = new User
        {
            Id = 1,
            Username = "testuser",
            Email = "test@example.com",
            PasswordHash = "hash123"
        };
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        var post = new SharePost
        {
            Id = 1,
            Title = "Test Post",
            PosterId = 1,
            Poster = user,
            FoodType = "中餐",
            Quantity = 2,
            PickupAddress = "北京市",
            AvailableUntil = DateTime.UtcNow.AddHours(5),
            Status = SharePostStatus.Available,
            CreatedAt = DateTime.UtcNow
        };

        await _postRepository.AddAsync(post);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        post = await _postRepository.GetByIdAsync(1);
        post.Should().NotBeNull();

        _postRepository.Delete(post!);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var normalResult = await _postRepository.GetByIdAsync(1);
        normalResult.Should().BeNull();

        var ignoreFilterResult = await _postRepository.GetByIdIgnoreFilterAsync(1);
        ignoreFilterResult.Should().NotBeNull();
        ignoreFilterResult!.Id.Should().Be(1);
        ignoreFilterResult.IsDeleted.Should().BeTrue();
        ignoreFilterResult.DeletedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task GetDeletedPagedAsync_Should_OnlyReturnSoftDeletedEntities()
    {
        var user = new User
        {
            Id = 1,
            Username = "testuser",
            Email = "test@example.com",
            PasswordHash = "hash123"
        };
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        var posts = new List<SharePost>
        {
            new()
            {
                Id = 1,
                Title = "Active Post",
                PosterId = 1,
                Poster = user,
                FoodType = "中餐",
                Quantity = 2,
                PickupAddress = "北京市",
                AvailableUntil = DateTime.UtcNow.AddHours(5),
                Status = SharePostStatus.Available,
                CreatedAt = DateTime.UtcNow,
                IsDeleted = false
            },
            new()
            {
                Id = 2,
                Title = "Deleted Post 1",
                PosterId = 1,
                Poster = user,
                FoodType = "西餐",
                Quantity = 1,
                PickupAddress = "上海市",
                AvailableUntil = DateTime.UtcNow.AddHours(5),
                Status = SharePostStatus.Available,
                CreatedAt = DateTime.UtcNow,
                IsDeleted = false
            },
            new()
            {
                Id = 3,
                Title = "Deleted Post 2",
                PosterId = 1,
                Poster = user,
                FoodType = "日料",
                Quantity = 3,
                PickupAddress = "广州市",
                AvailableUntil = DateTime.UtcNow.AddHours(5),
                Status = SharePostStatus.Available,
                CreatedAt = DateTime.UtcNow,
                IsDeleted = false
            }
        };

        await _context.SharePosts.AddRangeAsync(posts);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var post1 = await _postRepository.GetByIdAsync(2);
        var post2 = await _postRepository.GetByIdAsync(3);
        _postRepository.Delete(post1!);
        _postRepository.Delete(post2!);
        await _context.SaveChangesAsync();

        var (items, totalCount) = await _postRepository.GetDeletedPagedAsync(1, 10);

        totalCount.Should().Be(2);
        items.Should().HaveCount(2);
        items.Select(p => p.Title).Should().Contain(new[] { "Deleted Post 1", "Deleted Post 2" });
        items.Select(p => p.Title).Should().NotContain("Active Post");
    }

    [Fact]
    public async Task HardDelete_Should_RemoveEntityPermanently()
    {
        var user = new User
        {
            Id = 1,
            Username = "testuser",
            Email = "test@example.com",
            PasswordHash = "hash123"
        };
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        var post = new SharePost
        {
            Id = 1,
            Title = "Test Post",
            PosterId = 1,
            Poster = user,
            FoodType = "中餐",
            Quantity = 2,
            PickupAddress = "北京市",
            AvailableUntil = DateTime.UtcNow.AddHours(5),
            Status = SharePostStatus.Available,
            CreatedAt = DateTime.UtcNow
        };

        await _postRepository.AddAsync(post);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var initialCount = await _context.SharePosts.IgnoreQueryFilters().CountAsync();
        initialCount.Should().Be(1);

        post = await _postRepository.GetByIdAsync(1);
        _postRepository.Delete(post!);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var afterSoftDeleteCount = await _context.SharePosts.IgnoreQueryFilters().CountAsync();
        afterSoftDeleteCount.Should().Be(1);

        var softDeletedPost = await _postRepository.GetByIdIgnoreFilterAsync(1);
        softDeletedPost.Should().NotBeNull();

        _context.ChangeTracker.Clear();
        softDeletedPost = await _postRepository.GetByIdIgnoreFilterAsync(1);

        _postRepository.HardDelete(softDeletedPost!);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var afterHardDeleteCount = await _context.SharePosts.IgnoreQueryFilters().CountAsync();
        afterHardDeleteCount.Should().Be(0);

        var hardDeletedPost = await _postRepository.GetByIdIgnoreFilterAsync(1);
        hardDeletedPost.Should().BeNull();
    }

    [Fact]
    public async Task SoftDelete_Should_CreateDeletedEntitySnapshot()
    {
        var user = new User
        {
            Id = 1,
            Username = "testuser",
            Email = "test@example.com",
            PasswordHash = "hash123"
        };
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        var post = new SharePost
        {
            Id = 1,
            Title = "Test Post For Snapshot",
            PosterId = 1,
            Poster = user,
            FoodType = "中餐",
            Quantity = 2,
            PickupAddress = "北京市",
            AvailableUntil = DateTime.UtcNow.AddHours(5),
            Status = SharePostStatus.Available,
            CreatedAt = DateTime.UtcNow,
            DeletedBy = 1,
            DeletionReason = "测试删除"
        };

        await _postRepository.AddAsync(post);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var snapshotCountBefore = await _context.DeletedEntitySnapshots.CountAsync();
        snapshotCountBefore.Should().Be(0);

        post = await _postRepository.GetByIdAsync(1);
        _postRepository.Delete(post!);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var snapshotCountAfter = await _context.DeletedEntitySnapshots.CountAsync();
        snapshotCountAfter.Should().Be(1);

        var snapshot = await _context.DeletedEntitySnapshots.FirstOrDefaultAsync();
        snapshot.Should().NotBeNull();
        snapshot!.EntityType.Should().Be("SharePost");
        snapshot.EntityId.Should().Be(1);
        snapshot.EntityDisplayName.Should().Contain("Test Post For Snapshot");
        snapshot.DeletedBy.Should().Be(1);
        snapshot.DeletionReason.Should().Be("测试删除");
        snapshot.OriginalOwnerId.Should().Be(1);
        snapshot.SnapshotData.Should().NotBeNullOrEmpty();
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed) return;

        if (disposing)
        {
            _context.Dispose();
        }

        _disposed = true;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}
