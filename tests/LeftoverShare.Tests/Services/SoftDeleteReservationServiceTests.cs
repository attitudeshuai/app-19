using LeftoverShare.API.Entities.Enums;

namespace LeftoverShare.Tests.Services;

public class SoftDeleteReservationServiceTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IMapper> _mapperMock;
    private readonly ReservationService _reservationService;

    public SoftDeleteReservationServiceTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _mapperMock = new Mock<IMapper>();
        _reservationService = new ReservationService(_unitOfWorkMock.Object, _mapperMock.Object);
    }

    [Fact]
    public async Task DeleteAsync_Should_SetSoftDeleteAndCancelStatus()
    {
        var reservation = new Reservation
        {
            Id = 1,
            PostId = 1,
            ClaimerId = 1,
            Status = ReservationStatus.Pending,
            IsDeleted = false,
            DeletedAt = null,
            DeletedBy = null
        };

        var post = new SharePost
        {
            Id = 1,
            PosterId = 2,
            Status = SharePostStatus.Reserved
        };

        _unitOfWorkMock.Setup(x => x.Reservations.GetByIdAsync(1))
            .ReturnsAsync(reservation);
        _unitOfWorkMock.Setup(x => x.SharePosts.GetByIdAsync(1))
            .ReturnsAsync(post);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync())
            .ReturnsAsync(1);

        var result = await _reservationService.DeleteAsync(1, 1);

        result.Should().NotBeNull();
        result.Code.Should().Be(200);
        reservation.Status.Should().Be(ReservationStatus.Cancelled);
        reservation.DeletedBy.Should().Be(1);
        reservation.DeletionReason.Should().Be("用户取消预订");
        post.Status.Should().Be(SharePostStatus.Available);
        _unitOfWorkMock.Verify(x => x.Reservations.Update(It.Is<Reservation>(r =>
            r.Id == 1 &&
            r.Status == ReservationStatus.Cancelled &&
            r.DeletedBy == 1)), Times.Once);
        _unitOfWorkMock.Verify(x => x.Reservations.Delete(It.Is<Reservation>(r =>
            r.Id == 1)), Times.Once);
        _unitOfWorkMock.Verify(x => x.SharePosts.Update(It.Is<SharePost>(p =>
            p.Id == 1 &&
            p.Status == SharePostStatus.Available)), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task RestoreAsync_Should_WorkForCancelledReservation()
    {
        var reservation = new Reservation
        {
            Id = 1,
            PostId = 1,
            ClaimerId = 1,
            Status = ReservationStatus.Cancelled,
            IsDeleted = true,
            DeletedAt = DateTime.UtcNow.AddHours(-1),
            DeletedBy = 1,
            DeletionReason = "用户取消预订"
        };

        var post = new SharePost
        {
            Id = 1,
            PosterId = 2
        };

        _unitOfWorkMock.Setup(x => x.Reservations.GetByIdIgnoreFilterAsync(1))
            .ReturnsAsync(reservation);
        _unitOfWorkMock.Setup(x => x.SharePosts.GetByIdIgnoreFilterAsync(1))
            .ReturnsAsync(post);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync())
            .ReturnsAsync(1);

        var result = await _reservationService.RestoreAsync(1, 1);

        result.Should().NotBeNull();
        result.Code.Should().Be(200);
        reservation.IsDeleted.Should().BeFalse();
        reservation.DeletedAt.Should().BeNull();
        reservation.DeletedBy.Should().BeNull();
        reservation.DeletionReason.Should().BeNull();
        _unitOfWorkMock.Verify(x => x.Reservations.Update(It.Is<Reservation>(r =>
            r.Id == 1 &&
            !r.IsDeleted &&
            r.DeletedAt == null)), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(), Times.Once);
    }
}
