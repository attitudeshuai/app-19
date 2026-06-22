namespace LeftoverShare.Tests.Services;

public class SharePostServiceTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IMapper> _mapperMock;
    private readonly SharePostService _sharePostService;

    public SharePostServiceTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _mapperMock = new Mock<IMapper>();
        _sharePostService = new SharePostService(_unitOfWorkMock.Object, _mapperMock.Object);
    }

    [Fact]
    public async Task CreateAsync_WithValidRequest_CreatesPost()
    {
        var request = new CreateSharePostRequest
        {
            Title = "Test Food",
            Description = "Test Description",
            FoodType = "中餐",
            Quantity = 2,
            PickupAddress = "北京市朝阳区",
            AvailableUntil = DateTime.UtcNow.AddHours(5)
        };

        var user = new User { Id = 1, Username = "testuser" };

        _unitOfWorkMock.Setup(x => x.SharePosts.AddAsync(It.IsAny<SharePost>()))
            .ReturnsAsync((SharePost p) => p);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync())
            .ReturnsAsync(1);

        var result = await _sharePostService.CreateAsync(1, request);

        result.Should().NotBeNull();
        _unitOfWorkMock.Verify(x => x.SharePosts.AddAsync(It.Is<SharePost>(p =>
            p.PosterId == 1 &&
            p.Title == "Test Food" &&
            p.Status == SharePostStatus.Available)), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_WhenPostExists_ReturnsPost()
    {
        var post = new SharePost
        {
            Id = 1,
            Title = "Test Food",
            PosterId = 1,
            Poster = new User { Id = 1, Username = "testuser" }
        };

        _unitOfWorkMock.Setup(x => x.SharePosts.GetByIdAsync(1))
            .ReturnsAsync(post);

        var result = await _sharePostService.GetByIdAsync(1);

        result.Should().NotBeNull();
        result.Code.Should().Be(200);
    }

    [Fact]
    public async Task GetByIdAsync_WhenPostNotExists_ReturnsFail()
    {
        _unitOfWorkMock.Setup(x => x.SharePosts.GetByIdAsync(999))
            .ReturnsAsync((SharePost?)null);

        var result = await _sharePostService.GetByIdAsync(999);

        result.Should().NotBeNull();
        result.Code.Should().Be(404);
    }

    [Fact]
    public async Task DeleteAsync_WhenNotOwner_ReturnsFail()
    {
        var post = new SharePost { Id = 1, PosterId = 2 };

        _unitOfWorkMock.Setup(x => x.SharePosts.GetByIdAsync(1))
            .ReturnsAsync(post);

        var result = await _sharePostService.DeleteAsync(1, 1);

        result.Should().NotBeNull();
        result.Code.Should().Be(403);
    }

    [Fact]
    public async Task DeleteAsync_WhenOwner_DeletesPost()
    {
        var post = new SharePost { Id = 1, PosterId = 1 };

        _unitOfWorkMock.Setup(x => x.SharePosts.GetByIdAsync(1))
            .ReturnsAsync(post);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync())
            .ReturnsAsync(1);

        var result = await _sharePostService.DeleteAsync(1, 1);

        result.Should().NotBeNull();
        result.Code.Should().Be(200);
        _unitOfWorkMock.Verify(x => x.SharePosts.Delete(It.IsAny<SharePost>()), Times.Once);
    }

    [Fact]
    public async Task UpdateStatusAsync_WhenNotOwner_ReturnsFail()
    {
        var post = new SharePost { Id = 1, PosterId = 2, Status = SharePostStatus.Available };

        _unitOfWorkMock.Setup(x => x.SharePosts.GetByIdAsync(1))
            .ReturnsAsync(post);

        var result = await _sharePostService.UpdateStatusAsync(1, 1,
            new UpdateSharePostStatusRequest { Status = SharePostStatus.Reserved.ToString() });

        result.Should().NotBeNull();
        result.Code.Should().Be(403);
    }

    [Fact]
    public async Task UpdateStatusAsync_WhenOwner_UpdatesStatus()
    {
        var post = new SharePost { Id = 1, PosterId = 1, Status = SharePostStatus.Available };

        _unitOfWorkMock.Setup(x => x.SharePosts.GetByIdAsync(1))
            .ReturnsAsync(post);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync())
            .ReturnsAsync(1);

        var result = await _sharePostService.UpdateStatusAsync(1, 1,
            new UpdateSharePostStatusRequest { Status = SharePostStatus.Reserved.ToString() });

        result.Should().NotBeNull();
        result.Code.Should().Be(200);
        post.Status.Should().Be(SharePostStatus.Reserved);
    }
}
