using LeftoverShare.API.DTOs.Reservations;
using LeftoverShare.API.Helpers.Exceptions;
using LeftoverShare.API.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;

namespace LeftoverShare.Tests.Services;

/// <summary>
/// 高并发预约场景测试
/// 业务意图：验证在高并发场景下预约机制的正确性，
/// 包括库存扣减、重复预约检查、并发冲突处理、重试机制等
/// </summary>
public class ReservationConcurrencyTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IMapper> _mapperMock;
    private readonly Mock<ILogger<ReservationService>> _loggerMock;
    private readonly Mock<IDbContextTransaction> _transactionMock;
    private readonly ReservationService _reservationService;

    public ReservationConcurrencyTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _mapperMock = new Mock<IMapper>();
        _loggerMock = new Mock<ILogger<ReservationService>>();
        _transactionMock = new Mock<IDbContextTransaction>();

        var reservationsMock = new Mock<IReservationRepository>();
        reservationsMock.Setup(x => x.AddAsync(It.IsAny<Reservation>()))
            .ReturnsAsync((Reservation r) => r);

        var pickupCodesMock = new Mock<IPickupCodeRepository>();
        pickupCodesMock.Setup(x => x.AddAsync(It.IsAny<PickupCode>()))
            .ReturnsAsync((PickupCode pc) => pc);

        _unitOfWorkMock.Setup(x => x.Reservations)
            .Returns(reservationsMock.Object);
        _unitOfWorkMock.Setup(x => x.PickupCodes)
            .Returns(pickupCodesMock.Object);

        _unitOfWorkMock.Setup(x => x.BeginTransactionAsync())
            .ReturnsAsync(_transactionMock.Object);
        _unitOfWorkMock.Setup(x => x.CommitTransactionAsync())
            .Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(x => x.RollbackTransactionAsync())
            .Returns(Task.CompletedTask);

        _reservationService = new ReservationService(
            _unitOfWorkMock.Object,
            _mapperMock.Object,
            _loggerMock.Object);
    }

    /// <summary>
    /// 测试：分享帖不存在时应抛出正确的异常
    /// </summary>
    [Fact]
    public async Task CreateAsync_WhenPostNotFound_ShouldThrowPostNotFoundException()
    {
        var request = new CreateReservationRequest { PostId = 999, Note = "测试" };

        _unitOfWorkMock.Setup(x => x.SharePosts.GetByIdAsync(999))
            .ReturnsAsync((SharePost?)null);

        var exception = await Assert.ThrowsAsync<ReservationException>(() =>
            _reservationService.CreateAsync(1, request));

        exception.ErrorCode.Should().Be(ReservationErrorCode.PostNotFound);
        exception.Message.Should().Contain("999");
        _unitOfWorkMock.Verify(x => x.RollbackTransactionAsync(), Times.Once);
    }

    /// <summary>
    /// 测试：分享帖状态不可预约时应抛出正确的异常
    /// </summary>
    [Fact]
    public async Task CreateAsync_WhenPostNotAvailable_ShouldThrowPostNotAvailableException()
    {
        var request = new CreateReservationRequest { PostId = 1, Note = "测试" };
        var post = new SharePost
        {
            Id = 1,
            Status = SharePostStatus.Reserved,
            PosterId = 2
        };

        _unitOfWorkMock.Setup(x => x.SharePosts.GetByIdAsync(1))
            .ReturnsAsync(post);

        var exception = await Assert.ThrowsAsync<ReservationException>(() =>
            _reservationService.CreateAsync(1, request));

        exception.ErrorCode.Should().Be(ReservationErrorCode.PostNotAvailable);
        exception.Message.Should().Contain("Reserved");
        _unitOfWorkMock.Verify(x => x.RollbackTransactionAsync(), Times.Once);
    }

    /// <summary>
    /// 测试：用户预约自己发布的帖子时应抛出正确的异常
    /// </summary>
    [Fact]
    public async Task CreateAsync_WhenUserIsPoster_ShouldThrowCannotReserveOwnPostException()
    {
        var request = new CreateReservationRequest { PostId = 1, Note = "测试" };
        var post = new SharePost
        {
            Id = 1,
            Status = SharePostStatus.Available,
            PosterId = 1
        };

        _unitOfWorkMock.Setup(x => x.SharePosts.GetByIdAsync(1))
            .ReturnsAsync(post);

        var exception = await Assert.ThrowsAsync<ReservationException>(() =>
            _reservationService.CreateAsync(1, request));

        exception.ErrorCode.Should().Be(ReservationErrorCode.CannotReserveOwnPost);
        _unitOfWorkMock.Verify(x => x.RollbackTransactionAsync(), Times.Once);
    }

    /// <summary>
    /// 测试：用户重复预约时应抛出正确的异常
    /// </summary>
    [Fact]
    public async Task CreateAsync_WhenDuplicateReservation_ShouldThrowDuplicateReservationException()
    {
        var request = new CreateReservationRequest { PostId = 1, Note = "测试" };
        var post = new SharePost
        {
            Id = 1,
            Status = SharePostStatus.Available,
            PosterId = 2
        };

        _unitOfWorkMock.Setup(x => x.SharePosts.GetByIdAsync(1))
            .ReturnsAsync(post);
        _unitOfWorkMock.Setup(x => x.SharePosts.HasExistingReservationAsync(1, 1))
            .ReturnsAsync(true);

        var exception = await Assert.ThrowsAsync<ReservationException>(() =>
            _reservationService.CreateAsync(1, request));

        exception.ErrorCode.Should().Be(ReservationErrorCode.DuplicateReservation);
        exception.Details.Should().ContainKey("existingReservation");
        _unitOfWorkMock.Verify(x => x.RollbackTransactionAsync(), Times.Once);
    }

    /// <summary>
    /// 测试：库存不足时应抛出正确的异常
    /// </summary>
    [Fact]
    public async Task CreateAsync_WhenInsufficientStock_ShouldThrowInsufficientStockException()
    {
        var request = new CreateReservationRequest { PostId = 1, Note = "测试" };
        var post = new SharePost
        {
            Id = 1,
            Status = SharePostStatus.Available,
            PosterId = 2,
            Quantity = 5,
            ReservedQuantity = 5
        };

        _unitOfWorkMock.Setup(x => x.SharePosts.GetByIdAsync(1))
            .ReturnsAsync(post);
        _unitOfWorkMock.Setup(x => x.SharePosts.HasExistingReservationAsync(1, 1))
            .ReturnsAsync(false);
        _unitOfWorkMock.Setup(x => x.SharePosts.GetAvailableQuantityAsync(1))
            .ReturnsAsync(0);

        var exception = await Assert.ThrowsAsync<ReservationException>(() =>
            _reservationService.CreateAsync(1, request));

        exception.ErrorCode.Should().Be(ReservationErrorCode.InsufficientStock);
        exception.Details.Should().ContainKey("availableQuantity");
        exception.Details!["availableQuantity"].Should().Be(0);
        _unitOfWorkMock.Verify(x => x.RollbackTransactionAsync(), Times.Once);
    }

    /// <summary>
    /// 测试：库存扣减失败（并发冲突）时应抛出并发冲突异常
    /// </summary>
    [Fact]
    public async Task CreateAsync_WhenStockDecrementFails_ShouldThrowConcurrencyConflictException()
    {
        var request = new CreateReservationRequest { PostId = 1, Note = "测试" };
        var post = new SharePost
        {
            Id = 1,
            Status = SharePostStatus.Available,
            PosterId = 2,
            Quantity = 5,
            ReservedQuantity = 4
        };

        _unitOfWorkMock.Setup(x => x.SharePosts.GetByIdAsync(1))
            .ReturnsAsync(post);
        _unitOfWorkMock.Setup(x => x.SharePosts.HasExistingReservationAsync(1, 1))
            .ReturnsAsync(false);
        _unitOfWorkMock.Setup(x => x.SharePosts.GetAvailableQuantityAsync(1))
            .ReturnsAsync(1);
        _unitOfWorkMock.Setup(x => x.SharePosts.TryDecrementReservedQuantityAsync(1, 1))
            .ReturnsAsync(false);

        var exception = await Assert.ThrowsAsync<ReservationException>(() =>
            _reservationService.CreateAsync(1, request));

        exception.ErrorCode.Should().Be(ReservationErrorCode.ConcurrencyConflict);
        exception.Details.Should().ContainKey("canRetry");
        _unitOfWorkMock.Verify(x => x.RollbackTransactionAsync(), Times.Once);
    }

    /// <summary>
    /// 测试：发生 DbUpdateConcurrencyException 时应进行重试
    /// </summary>
    [Fact]
    public async Task CreateAsync_WhenDbUpdateConcurrencyException_ShouldRetryWithExponentialBackoff()
    {
        var request = new CreateReservationRequest { PostId = 1, Note = "测试" };
        var post = new SharePost
        {
            Id = 1,
            Status = SharePostStatus.Available,
            PosterId = 2,
            Quantity = 5,
            ReservedQuantity = 0
        };

        var refreshedPost = new SharePost
        {
            Id = 1,
            Status = SharePostStatus.Available,
            PosterId = 2,
            Quantity = 5,
            ReservedQuantity = 1
        };

        _unitOfWorkMock.Setup(x => x.SharePosts.GetByIdAsync(1))
            .ReturnsAsync(post);
        _unitOfWorkMock.Setup(x => x.SharePosts.HasExistingReservationAsync(1, 1))
            .ReturnsAsync(false);
        _unitOfWorkMock.Setup(x => x.SharePosts.GetAvailableQuantityAsync(1))
            .ReturnsAsync(5);
        _unitOfWorkMock.Setup(x => x.SharePosts.TryDecrementReservedQuantityAsync(1, 1))
            .ReturnsAsync(true);
        _unitOfWorkMock.Setup(x => x.SharePosts.GetByIdAsync(1))
            .ReturnsAsync(refreshedPost);

        var callCount = 0;
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync())
            .Returns(() =>
            {
                callCount++;
                if (callCount < 3)
                {
                    throw new DbUpdateConcurrencyException("并发冲突");
                }
                return Task.FromResult(1);
            });

        _mapperMock.Setup(x => x.Map<ReservationResponse>(It.IsAny<Reservation>()))
            .Returns(new ReservationResponse { Id = 1, PostId = 1 });

        var result = await _reservationService.CreateAsync(1, request);

        result.Should().NotBeNull();
        result.Code.Should().Be(200);
        callCount.Should().Be(3);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(), Times.Exactly(3));
        _unitOfWorkMock.Verify(x => x.RollbackTransactionAsync(), Times.Exactly(2));
        _unitOfWorkMock.Verify(x => x.CommitTransactionAsync(), Times.Once);
    }

    /// <summary>
    /// 测试：超过最大重试次数后应抛出超时异常
    /// </summary>
    [Fact]
    public async Task CreateAsync_WhenMaxRetriesExceeded_ShouldThrowReservationTimeoutException()
    {
        var request = new CreateReservationRequest { PostId = 1, Note = "测试" };
        var post = new SharePost
        {
            Id = 1,
            Status = SharePostStatus.Available,
            PosterId = 2,
            Quantity = 5,
            ReservedQuantity = 0
        };

        var refreshedPost = new SharePost
        {
            Id = 1,
            Status = SharePostStatus.Available,
            PosterId = 2,
            Quantity = 5,
            ReservedQuantity = 1
        };

        _unitOfWorkMock.Setup(x => x.SharePosts.GetByIdAsync(1))
            .ReturnsAsync(post);
        _unitOfWorkMock.Setup(x => x.SharePosts.HasExistingReservationAsync(1, 1))
            .ReturnsAsync(false);
        _unitOfWorkMock.Setup(x => x.SharePosts.GetAvailableQuantityAsync(1))
            .ReturnsAsync(5);
        _unitOfWorkMock.Setup(x => x.SharePosts.TryDecrementReservedQuantityAsync(1, 1))
            .ReturnsAsync(true);
        _unitOfWorkMock.Setup(x => x.SharePosts.GetByIdAsync(1))
            .ReturnsAsync(refreshedPost);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync())
            .ThrowsAsync(new DbUpdateConcurrencyException("并发冲突"));

        var exception = await Assert.ThrowsAsync<ReservationException>(() =>
            _reservationService.CreateAsync(1, request));

        exception.ErrorCode.Should().Be(ReservationErrorCode.ReservationTimeout);
        exception.Details.Should().ContainKey("maxRetries");
        exception.Details!["maxRetries"].Should().Be(3);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(), Times.Exactly(3));
        _unitOfWorkMock.Verify(x => x.RollbackTransactionAsync(), Times.Exactly(3));
    }

    /// <summary>
    /// 测试：发生唯一索引冲突（重复预约）时应抛出正确的异常
    /// </summary>
    [Fact]
    public async Task CreateAsync_WhenUniqueIndexViolation_ShouldThrowDuplicateReservationException()
    {
        var request = new CreateReservationRequest { PostId = 1, Note = "测试" };
        var post = new SharePost
        {
            Id = 1,
            Status = SharePostStatus.Available,
            PosterId = 2,
            Quantity = 5,
            ReservedQuantity = 0
        };

        var refreshedPost = new SharePost
        {
            Id = 1,
            Status = SharePostStatus.Available,
            PosterId = 2,
            Quantity = 5,
            ReservedQuantity = 1
        };

        var innerException = new Exception("Duplicate entry for key 'IX_Reservations_PostId_ClaimerId'");
        var dbUpdateException = new DbUpdateException("数据库更新失败", innerException);

        _unitOfWorkMock.Setup(x => x.SharePosts.GetByIdAsync(1))
            .ReturnsAsync(post);
        _unitOfWorkMock.Setup(x => x.SharePosts.HasExistingReservationAsync(1, 1))
            .ReturnsAsync(false);
        _unitOfWorkMock.Setup(x => x.SharePosts.GetAvailableQuantityAsync(1))
            .ReturnsAsync(5);
        _unitOfWorkMock.Setup(x => x.SharePosts.TryDecrementReservedQuantityAsync(1, 1))
            .ReturnsAsync(true);
        _unitOfWorkMock.Setup(x => x.SharePosts.GetByIdAsync(1))
            .ReturnsAsync(refreshedPost);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync())
            .ThrowsAsync(dbUpdateException);

        var exception = await Assert.ThrowsAsync<ReservationException>(() =>
            _reservationService.CreateAsync(1, request));

        exception.ErrorCode.Should().Be(ReservationErrorCode.DuplicateReservation);
        _unitOfWorkMock.Verify(x => x.RollbackTransactionAsync(), Times.Once);
    }

    /// <summary>
    /// 测试：预约成功后库存全部约完时应更新帖子状态为 Reserved
    /// </summary>
    [Fact]
    public async Task CreateAsync_WhenStockFullyReserved_ShouldUpdatePostStatusToReserved()
    {
        var request = new CreateReservationRequest { PostId = 1, Note = "测试" };
        var post = new SharePost
        {
            Id = 1,
            Status = SharePostStatus.Available,
            PosterId = 2,
            Quantity = 1,
            ReservedQuantity = 0
        };

        var refreshedPost = new SharePost
        {
            Id = 1,
            Status = SharePostStatus.Available,
            PosterId = 2,
            Quantity = 1,
            ReservedQuantity = 1
        };

        _unitOfWorkMock.Setup(x => x.SharePosts.GetByIdAsync(1))
            .ReturnsAsync(post);
        _unitOfWorkMock.Setup(x => x.SharePosts.HasExistingReservationAsync(1, 1))
            .ReturnsAsync(false);
        _unitOfWorkMock.Setup(x => x.SharePosts.GetAvailableQuantityAsync(1))
            .ReturnsAsync(1);
        _unitOfWorkMock.Setup(x => x.SharePosts.TryDecrementReservedQuantityAsync(1, 1))
            .ReturnsAsync(true);
        _unitOfWorkMock.Setup(x => x.SharePosts.GetByIdAsync(1))
            .ReturnsAsync(refreshedPost);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync())
            .ReturnsAsync(1);
        _mapperMock.Setup(x => x.Map<ReservationResponse>(It.IsAny<Reservation>()))
            .Returns(new ReservationResponse { Id = 1, PostId = 1 });

        var result = await _reservationService.CreateAsync(1, request);

        result.Should().NotBeNull();
        result.Code.Should().Be(200);
        _unitOfWorkMock.Verify(x => x.SharePosts.Update(It.Is<SharePost>(p =>
            p.Id == 1 &&
            p.Status == SharePostStatus.Reserved)), Times.Once);
        _unitOfWorkMock.Verify(x => x.CommitTransactionAsync(), Times.Once);
    }

    /// <summary>
    /// 测试：预约成功后库存未全部约完时应保持帖子状态为 Available
    /// </summary>
    [Fact]
    public async Task CreateAsync_WhenStockNotFullyReserved_ShouldKeepPostStatusAvailable()
    {
        var request = new CreateReservationRequest { PostId = 1, Note = "测试" };
        var post = new SharePost
        {
            Id = 1,
            Status = SharePostStatus.Available,
            PosterId = 2,
            Quantity = 10,
            ReservedQuantity = 0
        };

        var refreshedPost = new SharePost
        {
            Id = 1,
            Status = SharePostStatus.Available,
            PosterId = 2,
            Quantity = 10,
            ReservedQuantity = 1
        };

        _unitOfWorkMock.Setup(x => x.SharePosts.GetByIdAsync(1))
            .ReturnsAsync(post);
        _unitOfWorkMock.Setup(x => x.SharePosts.HasExistingReservationAsync(1, 1))
            .ReturnsAsync(false);
        _unitOfWorkMock.Setup(x => x.SharePosts.GetAvailableQuantityAsync(1))
            .ReturnsAsync(10);
        _unitOfWorkMock.Setup(x => x.SharePosts.TryDecrementReservedQuantityAsync(1, 1))
            .ReturnsAsync(true);
        _unitOfWorkMock.Setup(x => x.SharePosts.GetByIdAsync(1))
            .ReturnsAsync(refreshedPost);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync())
            .ReturnsAsync(1);
        _mapperMock.Setup(x => x.Map<ReservationResponse>(It.IsAny<Reservation>()))
            .Returns(new ReservationResponse { Id = 1, PostId = 1 });

        var result = await _reservationService.CreateAsync(1, request);

        result.Should().NotBeNull();
        result.Code.Should().Be(200);
        _unitOfWorkMock.Verify(x => x.SharePosts.Update(It.Is<SharePost>(p =>
            p.Id == 1 &&
            p.Status == SharePostStatus.Reserved)), Times.Never);
        _unitOfWorkMock.Verify(x => x.CommitTransactionAsync(), Times.Once);
    }

    /// <summary>
    /// 测试：多个用户并发预约时的库存正确性
    /// 模拟10个用户同时预约只有5份库存的帖子
    /// </summary>
    [Fact]
    public async Task CreateAsync_WhenMultipleUsersConcurrent_ShouldNotOversell()
    {
        var post = new SharePost
        {
            Id = 1,
            Status = SharePostStatus.Available,
            PosterId = 99,
            Quantity = 5,
            ReservedQuantity = 0
        };

        var successfulReservations = 0;
        var failedReservations = 0;
        var lockObj = new object();

        _unitOfWorkMock.Setup(x => x.SharePosts.GetByIdAsync(1))
            .ReturnsAsync(post);
        _unitOfWorkMock.Setup(x => x.SharePosts.HasExistingReservationAsync(1, It.IsAny<int>()))
            .ReturnsAsync(false);
        _unitOfWorkMock.Setup(x => x.SharePosts.GetAvailableQuantityAsync(1))
            .ReturnsAsync(() => post.Quantity - post.ReservedQuantity);

        var stockLock = new object();
        _unitOfWorkMock.Setup(x => x.SharePosts.TryDecrementReservedQuantityAsync(1, 1))
            .ReturnsAsync(() =>
            {
                lock (stockLock)
                {
                    if (post.ReservedQuantity < post.Quantity)
                    {
                        post.ReservedQuantity++;
                        return true;
                    }
                    return false;
                }
            });

        _unitOfWorkMock.Setup(x => x.SharePosts.GetByIdAsync(1))
            .ReturnsAsync(post);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync())
            .ReturnsAsync(1);
        _mapperMock.Setup(x => x.Map<ReservationResponse>(It.IsAny<Reservation>()))
            .Returns(new ReservationResponse { Id = 1, PostId = 1 });

        var tasks = new List<Task>();
        for (int i = 1; i <= 10; i++)
        {
            var userId = i;
            tasks.Add(Task.Run(async () =>
            {
                try
                {
                    var request = new CreateReservationRequest { PostId = 1, Note = $"用户{userId}" };
                    await _reservationService.CreateAsync(userId, request);
                    lock (lockObj)
                    {
                        successfulReservations++;
                    }
                }
                catch (ReservationException)
                {
                    lock (lockObj)
                    {
                        failedReservations++;
                    }
                }
            }));
        }

        await Task.WhenAll(tasks);

        successfulReservations.Should().Be(5);
        failedReservations.Should().Be(5);
        post.ReservedQuantity.Should().Be(5);
    }

    /// <summary>
    /// 测试：取消预约后应正确恢复库存
    /// </summary>
    [Fact]
    public async Task DeleteAsync_WhenCancelling_ShouldRestoreStock()
    {
        var reservation = new Reservation
        {
            Id = 1,
            PostId = 1,
            ClaimerId = 1,
            Status = ReservationStatus.Pending,
            Quantity = 2
        };

        var post = new SharePost
        {
            Id = 1,
            PosterId = 2,
            Status = SharePostStatus.Reserved,
            Quantity = 10,
            ReservedQuantity = 5
        };

        var refreshedPost = new SharePost
        {
            Id = 1,
            PosterId = 2,
            Status = SharePostStatus.Reserved,
            Quantity = 10,
            ReservedQuantity = 3
        };

        _unitOfWorkMock.Setup(x => x.Reservations.GetByIdAsync(1))
            .ReturnsAsync(reservation);
        _unitOfWorkMock.Setup(x => x.SharePosts.GetByIdAsync(1))
            .ReturnsAsync(post);
        _unitOfWorkMock.Setup(x => x.SharePosts.TryIncrementReservedQuantityAsync(1, 2))
            .ReturnsAsync(true);
        _unitOfWorkMock.Setup(x => x.SharePosts.GetByIdAsync(1))
            .ReturnsAsync(refreshedPost);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync())
            .ReturnsAsync(1);

        var result = await _reservationService.DeleteAsync(1, 1);

        result.Should().NotBeNull();
        result.Code.Should().Be(200);
        _unitOfWorkMock.Verify(x => x.SharePosts.TryIncrementReservedQuantityAsync(1, 2), Times.Once);
        _unitOfWorkMock.Verify(x => x.SharePosts.Update(It.Is<SharePost>(p =>
            p.Id == 1 &&
            p.Status == SharePostStatus.Available)), Times.Once);
        _unitOfWorkMock.Verify(x => x.CommitTransactionAsync(), Times.Once);
    }
}
