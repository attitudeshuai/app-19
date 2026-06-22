using LeftoverShare.API.DTOs.Common;
using LeftoverShare.API.Entities.Enums;

namespace LeftoverShare.Tests.Services;

public class SoftDeleteSharePostServiceTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IMapper> _mapperMock;
    private readonly SharePostService _sharePostService;

    public SoftDeleteSharePostServiceTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _mapperMock = new Mock<IMapper>();
        _sharePostService = new SharePostService(_unitOfWorkMock.Object, _mapperMock.Object);
    }

    [Fact]
    public async Task DeleteAsync_Should_SetSoftDeleteFlags_InsteadOfHardDelete()
    {
        var post = new SharePost
        {
            Id = 1,
            Title = "Test Food",
            PosterId = 1,
            IsDeleted = false,
            DeletedAt = null,
            DeletedBy = null
        };

        _unitOfWorkMock.Setup(x => x.SharePosts.GetByIdAsync(1))
            .ReturnsAsync(post);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync())
            .ReturnsAsync(1);

        var result = await _sharePostService.DeleteAsync(1, 1);

        result.Should().NotBeNull();
        result.Code.Should().Be(200);
        post.IsDeleted.Should().BeFalse();
        post.DeletedBy.Should().Be(1);
        post.DeletionReason.Should().Be("用户主动删除");
        _unitOfWorkMock.Verify(x => x.SharePosts.Delete(It.Is<SharePost>(p =>
            p.Id == 1 &&
            p.DeletedBy == 1 &&
            p.DeletionReason == "用户主动删除")), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_Should_CreateAuditSnapshot()
    {
        var post = new SharePost
        {
            Id = 1,
            Title = "Test Food",
            PosterId = 1,
            Status = SharePostStatus.Available
        };

        _unitOfWorkMock.Setup(x => x.SharePosts.GetByIdAsync(1))
            .ReturnsAsync(post);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync())
            .ReturnsAsync(1);

        await _sharePostService.DeleteAsync(1, 1);

        _unitOfWorkMock.Verify(x => x.SharePosts.Delete(It.Is<SharePost>(p =>
            p.Id == 1 &&
            p.DeletedBy == 1)), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_Should_Return404_ForSoftDeletedPost()
    {
        _unitOfWorkMock.Setup(x => x.SharePosts.GetByIdAsync(1))
            .ReturnsAsync((SharePost?)null);

        var result = await _sharePostService.GetByIdAsync(1);

        result.Should().NotBeNull();
        result.Code.Should().Be(404);
        result.Message.Should().Be("帖子不存在");
    }

    [Fact]
    public async Task RestoreAsync_Should_ClearSoftDeleteFlags()
    {
        var post = new SharePost
        {
            Id = 1,
            Title = "Test Food",
            PosterId = 1,
            IsDeleted = true,
            DeletedAt = DateTime.UtcNow.AddHours(-1),
            DeletedBy = 1,
            DeletionReason = "用户主动删除"
        };

        _unitOfWorkMock.Setup(x => x.SharePosts.GetByIdIgnoreFilterAsync(1))
            .ReturnsAsync(post);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync())
            .ReturnsAsync(1);

        var result = await _sharePostService.RestoreAsync(1, 1);

        result.Should().NotBeNull();
        result.Code.Should().Be(200);
        post.IsDeleted.Should().BeFalse();
        post.DeletedAt.Should().BeNull();
        post.DeletedBy.Should().BeNull();
        post.DeletionReason.Should().BeNull();
        _unitOfWorkMock.Verify(x => x.SharePosts.Update(It.Is<SharePost>(p =>
            p.Id == 1 &&
            !p.IsDeleted &&
            p.DeletedAt == null &&
            p.DeletedBy == null)), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task RestoreAsync_Should_Fail_ForNonExistentPost()
    {
        _unitOfWorkMock.Setup(x => x.SharePosts.GetByIdIgnoreFilterAsync(999))
            .ReturnsAsync((SharePost?)null);

        var result = await _sharePostService.RestoreAsync(999, 1);

        result.Should().NotBeNull();
        result.Code.Should().Be(404);
        result.Message.Should().Be("帖子不存在");
    }

    [Fact]
    public async Task RestoreAsync_Should_Fail_WithoutPermission()
    {
        var post = new SharePost
        {
            Id = 1,
            Title = "Test Food",
            PosterId = 1,
            IsDeleted = true,
            DeletedAt = DateTime.UtcNow.AddHours(-1),
            DeletedBy = 1
        };

        _unitOfWorkMock.Setup(x => x.SharePosts.GetByIdIgnoreFilterAsync(1))
            .ReturnsAsync(post);

        var result = await _sharePostService.RestoreAsync(1, 999);

        result.Should().NotBeNull();
        result.Code.Should().Be(403);
        result.Message.Should().Be("无权限恢复此帖子");
        _unitOfWorkMock.Verify(x => x.SharePosts.Update(It.IsAny<SharePost>()), Times.Never);
    }

    [Fact]
    public async Task GetRecycleBinAsync_Should_OnlyReturnDeletedPosts()
    {
        var deletedPosts = new List<SharePost>
        {
            new()
            {
                Id = 1,
                Title = "Deleted Post 1",
                PosterId = 1,
                IsDeleted = true,
                DeletedAt = DateTime.UtcNow.AddHours(-1),
                DeletedBy = 1
            },
            new()
            {
                Id = 2,
                Title = "Deleted Post 2",
                PosterId = 2,
                IsDeleted = true,
                DeletedAt = DateTime.UtcNow.AddHours(-2),
                DeletedBy = 1
            }
        };

        var request = new PagedRequest
        {
            PageNumber = 1,
            PageSize = 10
        };

        _unitOfWorkMock.Setup(x => x.SharePosts.GetDeletedPagedAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<SharePost, bool>>>(),
                1, 10))
            .ReturnsAsync((deletedPosts, 2));
        _mapperMock.Setup(x => x.Map<List<SharePostListResponse>>(It.IsAny<List<SharePost>>()))
            .Returns(deletedPosts.Select(p => new SharePostListResponse
            {
                Id = p.Id,
                Title = p.Title
            }).ToList());

        var result = await _sharePostService.GetRecycleBinAsync(1, request);

        result.Should().NotBeNull();
        result.Code.Should().Be(200);
        _unitOfWorkMock.Verify(x => x.SharePosts.GetDeletedPagedAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<SharePost, bool>>>(),
            1, 10), Times.Once);
    }
}
