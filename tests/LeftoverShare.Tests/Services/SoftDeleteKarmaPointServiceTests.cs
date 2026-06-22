namespace LeftoverShare.Tests.Services;

public class SoftDeleteKarmaPointServiceTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IMapper> _mapperMock;
    private readonly KarmaPointService _karmaPointService;

    public SoftDeleteKarmaPointServiceTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _mapperMock = new Mock<IMapper>();
        _karmaPointService = new KarmaPointService(_unitOfWorkMock.Object, _mapperMock.Object);
    }

    [Fact]
    public async Task DeleteAsync_Should_RollbackTotalPoints()
    {
        var user = new User
        {
            Id = 1,
            Username = "testuser",
            TotalKarmaPoints = 100
        };

        var karmaPoint = new KarmaPoint
        {
            Id = 1,
            UserId = 1,
            Points = 10,
            Reason = "分享食物完成",
            TransactionType = "Earn",
            IsDeleted = false,
            DeletedAt = null,
            DeletedBy = null
        };

        _unitOfWorkMock.Setup(x => x.KarmaPoints.GetByIdAsync(1))
            .ReturnsAsync(karmaPoint);
        _unitOfWorkMock.Setup(x => x.Users.GetByIdAsync(1))
            .ReturnsAsync(user);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync())
            .ReturnsAsync(1);

        var result = await _karmaPointService.DeleteAsync(1);

        result.Should().NotBeNull();
        result.Code.Should().Be(200);
        user.TotalKarmaPoints.Should().Be(90);
        karmaPoint.DeletedBy.Should().Be(1);
        karmaPoint.DeletionReason.Should().Be("用户主动删除");
        _unitOfWorkMock.Verify(x => x.Users.Update(It.Is<User>(u =>
            u.Id == 1 &&
            u.TotalKarmaPoints == 90)), Times.Once);
        _unitOfWorkMock.Verify(x => x.KarmaPoints.Delete(It.Is<KarmaPoint>(k =>
            k.Id == 1 &&
            k.DeletedBy == 1)), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task RestoreAsync_Should_ReapplyTotalPoints()
    {
        var user = new User
        {
            Id = 1,
            Username = "testuser",
            TotalKarmaPoints = 90
        };

        var karmaPoint = new KarmaPoint
        {
            Id = 1,
            UserId = 1,
            Points = 10,
            Reason = "分享食物完成",
            TransactionType = "Earn",
            IsDeleted = true,
            DeletedAt = DateTime.UtcNow.AddHours(-1),
            DeletedBy = 1,
            DeletionReason = "用户主动删除"
        };

        _unitOfWorkMock.Setup(x => x.KarmaPoints.GetByIdIgnoreFilterAsync(1))
            .ReturnsAsync(karmaPoint);
        _unitOfWorkMock.Setup(x => x.Users.GetByIdAsync(1))
            .ReturnsAsync(user);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync())
            .ReturnsAsync(1);

        var result = await _karmaPointService.RestoreAsync(1, 1);

        result.Should().NotBeNull();
        result.Code.Should().Be(200);
        user.TotalKarmaPoints.Should().Be(100);
        karmaPoint.IsDeleted.Should().BeFalse();
        karmaPoint.DeletedAt.Should().BeNull();
        karmaPoint.DeletedBy.Should().BeNull();
        karmaPoint.DeletionReason.Should().BeNull();
        _unitOfWorkMock.Verify(x => x.Users.Update(It.Is<User>(u =>
            u.Id == 1 &&
            u.TotalKarmaPoints == 100)), Times.Once);
        _unitOfWorkMock.Verify(x => x.KarmaPoints.Update(It.Is<KarmaPoint>(k =>
            k.Id == 1 &&
            !k.IsDeleted &&
            k.DeletedAt == null)), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(), Times.Once);
    }
}
