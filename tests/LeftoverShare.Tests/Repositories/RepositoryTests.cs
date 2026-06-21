using System.Linq.Expressions;
using LeftoverShare.API.Repositories;
using LeftoverShare.API.Entities.Enums;

namespace LeftoverShare.Tests.Repositories;

public class RepositoryTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly Repository<User> _userRepository;
    private readonly Repository<SharePost> _postRepository;
    private bool _disposed;

    public RepositoryTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new AppDbContext(options);
        _userRepository = new Repository<User>(_context);
        _postRepository = new Repository<SharePost>(_context);
    }

    [Fact]
    public async Task AddAsync_AddsEntityToDatabase()
    {
        var user = new User
        {
            Username = "testuser",
            Email = "test@example.com",
            PasswordHash = "hash123"
        };

        var result = await _userRepository.AddAsync(user);
        await _context.SaveChangesAsync();

        result.Should().NotBeNull();
        result.Id.Should().BeGreaterThan(0);

        var savedUser = await _context.Users.FindAsync(result.Id);
        savedUser.Should().NotBeNull();
        savedUser!.Username.Should().Be("testuser");
        savedUser.Email.Should().Be("test@example.com");
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsEntity_WhenExists()
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

        var result = await _userRepository.GetByIdAsync(1);

        result.Should().NotBeNull();
        result!.Id.Should().Be(1);
        result.Username.Should().Be("testuser");
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNull_WhenNotExists()
    {
        var result = await _userRepository.GetByIdAsync(999);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllEntities()
    {
        var users = new List<User>
        {
            new() { Username = "user1", Email = "user1@example.com", PasswordHash = "hash1" },
            new() { Username = "user2", Email = "user2@example.com", PasswordHash = "hash2" },
            new() { Username = "user3", Email = "user3@example.com", PasswordHash = "hash3" }
        };

        await _context.Users.AddRangeAsync(users);
        await _context.SaveChangesAsync();

        var result = await _userRepository.GetAllAsync();

        result.Should().HaveCount(3);
        result.Select(u => u.Username).Should().Contain(new[] { "user1", "user2", "user3" });
    }

    [Fact]
    public async Task GetPagedAsync_ReturnsPagedResults()
    {
        var users = Enumerable.Range(1, 25)
            .Select(i => new User
            {
                Username = $"user{i}",
                Email = $"user{i}@example.com",
                PasswordHash = $"hash{i}"
            })
            .ToList();

        await _context.Users.AddRangeAsync(users);
        await _context.SaveChangesAsync();

        var (items, totalCount) = await _userRepository.GetPagedAsync(2, 10);

        totalCount.Should().Be(25);
        items.Should().HaveCount(10);
    }

    [Fact]
    public async Task GetPagedAsync_FirstPage_ReturnsFirstItems()
    {
        var users = Enumerable.Range(1, 5)
            .Select(i => new User
            {
                Username = $"user{i}",
                Email = $"user{i}@example.com",
                PasswordHash = $"hash{i}"
            })
            .ToList();

        await _context.Users.AddRangeAsync(users);
        await _context.SaveChangesAsync();

        var (items, totalCount) = await _userRepository.GetPagedAsync(1, 3);

        totalCount.Should().Be(5);
        items.Should().HaveCount(3);
    }

    [Fact]
    public async Task FindAsync_ReturnsMatchingEntities()
    {
        var users = new List<User>
        {
            new() { Username = "alice", Email = "alice@example.com", PasswordHash = "hash1" },
            new() { Username = "bob", Email = "bob@example.com", PasswordHash = "hash2" },
            new() { Username = "charlie", Email = "charlie@example.com", PasswordHash = "hash3" }
        };

        await _context.Users.AddRangeAsync(users);
        await _context.SaveChangesAsync();

        var result = await _userRepository.FindAsync(u => u.Username == "alice" || u.Username == "bob");

        result.Should().HaveCount(2);
        result.Select(u => u.Username).Should().Contain(new[] { "alice", "bob" });
    }

    [Fact]
    public void Update_UpdatesEntityProperties()
    {
        var user = new User
        {
            Id = 1,
            Username = "oldusername",
            Email = "old@example.com",
            PasswordHash = "hash123"
        };

        _context.Users.Add(user);
        _context.SaveChanges();

        user.Username = "newusername";
        user.Email = "new@example.com";

        _userRepository.Update(user);
        _context.SaveChanges();

        var updatedUser = _context.Users.Find(1);
        updatedUser.Should().NotBeNull();
        updatedUser!.Username.Should().Be("newusername");
        updatedUser.Email.Should().Be("new@example.com");
    }

    [Fact]
    public void Delete_RemovesEntityFromDatabase()
    {
        var user = new User
        {
            Id = 1,
            Username = "testuser",
            Email = "test@example.com",
            PasswordHash = "hash123"
        };

        _context.Users.Add(user);
        _context.SaveChanges();

        _userRepository.Delete(user);
        _context.SaveChanges();

        var deletedUser = _context.Users.Find(1);
        deletedUser.Should().BeNull();
    }

    [Fact]
    public void DeleteRange_RemovesMultipleEntities()
    {
        var users = new List<User>
        {
            new() { Id = 1, Username = "user1", Email = "user1@example.com", PasswordHash = "hash1" },
            new() { Id = 2, Username = "user2", Email = "user2@example.com", PasswordHash = "hash2" },
            new() { Id = 3, Username = "user3", Email = "user3@example.com", PasswordHash = "hash3" }
        };

        _context.Users.AddRange(users);
        _context.SaveChanges();

        _userRepository.DeleteRange(users.Take(2));
        _context.SaveChanges();

        var remainingUsers = _context.Users.ToList();
        remainingUsers.Should().HaveCount(1);
        remainingUsers.First().Username.Should().Be("user3");
    }

    [Fact]
    public void GetQueryable_ReturnsQueryableCollection()
    {
        var posts = new List<SharePost>
        {
            new() { Id = 1, Title = "Post 1", PosterId = 1, Status = SharePostStatus.Available, FoodType = "中餐", PickupAddress = "北京", Quantity = 2 },
            new() { Id = 2, Title = "Post 2", PosterId = 1, Status = SharePostStatus.Reserved, FoodType = "西餐", PickupAddress = "上海", Quantity = 1 },
            new() { Id = 3, Title = "Post 3", PosterId = 2, Status = SharePostStatus.Available, FoodType = "中餐", PickupAddress = "广州", Quantity = 3 }
        };

        _context.SharePosts.AddRange(posts);
        _context.SaveChanges();

        var queryable = _postRepository.GetQueryable();

        var availablePosts = queryable.Where(p => p.Status == SharePostStatus.Available).ToList();
        availablePosts.Should().HaveCount(2);

        var chineseFoodPosts = queryable.Where(p => p.FoodType == "中餐").OrderByDescending(p => p.Quantity).ToList();
        chineseFoodPosts.Should().HaveCount(2);
        chineseFoodPosts.First().Quantity.Should().Be(3);
        chineseFoodPosts.Last().Quantity.Should().Be(2);
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
