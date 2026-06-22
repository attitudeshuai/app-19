using AutoMapper;
using LeftoverShare.API.DTOs.Common;
using LeftoverShare.API.DTOs.Reservations;
using LeftoverShare.API.Entities.Enums;
using LeftoverShare.API.Helpers;
using LeftoverShare.API.Helpers.Exceptions;
using LeftoverShare.API.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using KarmaPointEntity = LeftoverShare.API.Entities.KarmaPoint;
using PickupCodeEntity = LeftoverShare.API.Entities.PickupCode;
using ReservationEntity = LeftoverShare.API.Entities.Reservation;
using SharePostEntity = LeftoverShare.API.Entities.SharePost;

namespace LeftoverShare.API.Services.Impl;

/// <summary>
/// 预约服务实现
/// 高并发设计要点：
/// 1. 原子性库存扣减（原生 SQL + WHERE 条件）
/// 2. 乐观并发控制（RowVersion）
/// 3. 数据库事务隔离级别（Read Committed）
/// 4. 幂等性检查（防重复预约）
/// 5. 指数退避重试机制
/// 6. 异常自动回滚
/// </summary>
public class ReservationService : IReservationService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<ReservationService> _logger;

    /// <summary>
    /// 最大重试次数
    /// </summary>
    private const int MaxRetryAttempts = 3;

    /// <summary>
    /// 初始重试延迟（毫秒）
    /// </summary>
    private const int InitialRetryDelayMs = 50;

    /// <summary>
    /// 预约数量（当前设计为每次预约1份）
    /// </summary>
    private const int ReservationQuantity = 1;

    public ReservationService(IUnitOfWork unitOfWork, IMapper mapper, ILogger<ReservationService> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }

    /// <summary>
    /// 分页获取预约列表，可按用户筛选
    /// </summary>
    public async Task<ApiResponse> GetPagedAsync(PagedRequest request, int? userId = null)
    {
        IEnumerable<ReservationEntity> items;
        int totalCount;

        if (userId.HasValue)
        {
            items = await _unitOfWork.Reservations.GetByUserIdAsync(userId.Value);
            totalCount = items.Count();
            items = items
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToList();
        }
        else
        {
            var (pagedItems, pagedTotalCount) = await _unitOfWork.Reservations.GetPagedWithDetailsAsync(
                request.PageNumber, request.PageSize);
            items = pagedItems;
            totalCount = pagedTotalCount;
        }

        var reservationResponses = _mapper.Map<List<ReservationResponse>>(items);
        var pagedResponse = new PagedResponse<ReservationResponse>
        {
            Items = reservationResponses,
            TotalCount = totalCount,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize,
            TotalPages = (int)Math.Ceiling((double)totalCount / request.PageSize)
        };

        return ApiResponse.Success(pagedResponse);
    }

    /// <summary>
    /// 根据ID获取预约详情
    /// </summary>
    public async Task<ApiResponse> GetByIdAsync(int id)
    {
        var reservation = await _unitOfWork.Reservations.GetByIdAsync(id);
        if (reservation == null)
        {
            return ApiResponse.Fail("预订不存在", 404);
        }

        var reservationResponse = _mapper.Map<ReservationResponse>(reservation);
        return ApiResponse.Success(reservationResponse);
    }

    /// <summary>
    /// 创建预约（高并发安全版本）
    /// 业务流程：
    /// 1. 前置校验（帖子存在、状态、权限）
    /// 2. 幂等性检查（防止重复预约）
    /// 3. 原子性库存扣减
    /// 4. 创建预约记录
    /// 5. 更新帖子状态（如果全部约完）
    /// 6. 异常重试与回滚
    /// </summary>
    public async Task<ApiResponse> CreateAsync(int userId, CreateReservationRequest request)
    {
        int retryCount = 0;
        while (true)
        {
            try
            {
                return await CreateReservationWithTransactionAsync(userId, request, retryCount);
            }
            catch (DbUpdateConcurrencyException)
            {
                retryCount++;
                if (retryCount >= MaxRetryAttempts)
                {
                    _logger.LogWarning(
                        "预约失败：达到最大重试次数 {MaxRetryAttempts}，PostId: {PostId}, UserId: {UserId}",
                        MaxRetryAttempts, request.PostId, userId);
                    throw ReservationException.ReservationTimeout(request.PostId, MaxRetryAttempts);
                }

                var delay = InitialRetryDelayMs * (int)Math.Pow(2, retryCount - 1);
                _logger.LogInformation(
                    "检测到并发冲突，正在进行第 {RetryCount} 次重试，延迟 {Delay}ms，PostId: {PostId}, UserId: {UserId}",
                    retryCount, delay, request.PostId, userId);
                await Task.Delay(delay);
            }
        }
    }

    /// <summary>
    /// 使用事务执行预约创建
    /// 业务意图：在事务中执行所有操作，确保数据一致性
    /// </summary>
    private async Task<ApiResponse> CreateReservationWithTransactionAsync(int userId, CreateReservationRequest request, int retryCount)
    {
        using var transaction = await _unitOfWork.BeginTransactionAsync();
        try
        {
            var post = await _unitOfWork.SharePosts.GetByIdAsync(request.PostId);
            if (post == null)
            {
                throw ReservationException.PostNotFound(request.PostId);
            }

            if (post.Status != SharePostStatus.Available)
            {
                throw ReservationException.PostNotAvailable(request.PostId, post.Status.ToString());
            }

            if (post.PosterId == userId)
            {
                throw ReservationException.CannotReserveOwnPost(request.PostId, userId);
            }

            var hasExistingReservation = await _unitOfWork.SharePosts.HasExistingReservationAsync(request.PostId, userId);
            if (hasExistingReservation)
            {
                throw ReservationException.DuplicateReservation(request.PostId, userId);
            }

            var availableQuantity = await _unitOfWork.SharePosts.GetAvailableQuantityAsync(request.PostId);
            if (availableQuantity < ReservationQuantity)
            {
                throw ReservationException.InsufficientStock(request.PostId, availableQuantity, ReservationQuantity);
            }

            var stockDecremented = await _unitOfWork.SharePosts.TryDecrementReservedQuantityAsync(
                request.PostId, ReservationQuantity);

            if (!stockDecremented)
            {
                throw ReservationException.ConcurrencyConflict(request.PostId, retryCount);
            }

            var refreshedPost = await _unitOfWork.SharePosts.GetByIdAsync(request.PostId);
            if (refreshedPost == null)
            {
                throw ReservationException.PostNotFound(request.PostId);
            }

            var newReservedQuantity = refreshedPost.ReservedQuantity;
            var totalQuantity = refreshedPost.Quantity;
            var isFullyReserved = newReservedQuantity >= totalQuantity;

            var pickupCode = PickupCodeGenerator.Generate();

            var reservation = new ReservationEntity
            {
                PostId = request.PostId,
                ClaimerId = userId,
                Quantity = ReservationQuantity,
                PickupCode = pickupCode,
                Status = ReservationStatus.Pending,
                Note = request.Note,
                ReservedAt = DateTime.UtcNow
            };

            await _unitOfWork.Reservations.AddAsync(reservation);

            var pickupCodeEntity = new PickupCodeEntity
            {
                ReservationId = reservation.Id,
                Code = pickupCode,
                ExpiresAt = DateTime.UtcNow.AddHours(24),
                IsUsed = false
            };
            await _unitOfWork.PickupCodes.AddAsync(pickupCodeEntity);

            if (isFullyReserved)
            {
                refreshedPost.Status = SharePostStatus.Reserved;
                _unitOfWork.SharePosts.Update(refreshedPost);
            }

            try
            {
                await _unitOfWork.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogWarning(ex,
                    "保存预约时发生并发冲突，PostId: {PostId}, UserId: {UserId}",
                    request.PostId, userId);
                throw;
            }
            catch (DbUpdateException ex)
                when (ex.InnerException?.Message.Contains("IX_Reservations_PostId_ClaimerId") == true)
            {
                _logger.LogWarning(
                    "检测到唯一索引冲突（重复预约），PostId: {PostId}, UserId: {UserId}",
                    request.PostId, userId);
                throw ReservationException.DuplicateReservation(request.PostId, userId);
            }

            await _unitOfWork.CommitTransactionAsync();

            _logger.LogInformation(
                "预约成功：PostId: {PostId}, UserId: {UserId}, ReservationId: {ReservationId}",
                request.PostId, userId, reservation.Id);

            var reservationResponse = _mapper.Map<ReservationResponse>(reservation);
            return ApiResponse.Success(reservationResponse, "预订成功");
        }
        catch (ReservationException)
        {
            try
            {
                await _unitOfWork.RollbackTransactionAsync();
            }
            catch { /* 忽略回滚错误 */ }
            throw;
        }
        catch (DbUpdateConcurrencyException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "创建预约时发生未知错误，PostId: {PostId}, UserId: {UserId}",
                request.PostId, userId);
            try
            {
                await _unitOfWork.RollbackTransactionAsync();
            }
            catch { /* 忽略回滚错误 */ }
            throw;
        }
    }

    /// <summary>
    /// 更新预约：权限检查(ClaimerId或Post的PosterId)
    /// </summary>
    public async Task<ApiResponse> UpdateAsync(int id, int userId, UpdateReservationRequest request)
    {
        var reservation = await _unitOfWork.Reservations.GetByIdAsync(id);
        if (reservation == null)
        {
            return ApiResponse.Fail("预订不存在", 404);
        }

        var post = await _unitOfWork.SharePosts.GetByIdAsync(reservation.PostId);
        if (reservation.ClaimerId != userId && post?.PosterId != userId)
        {
            return ApiResponse.Fail("无权限修改此预订", 403);
        }

        reservation.Note = request.Note;
        _unitOfWork.Reservations.Update(reservation);
        await _unitOfWork.SaveChangesAsync();

        var reservationResponse = _mapper.Map<ReservationResponse>(reservation);
        return ApiResponse.Success(reservationResponse, "预订更新成功");
    }

    /// <summary>
    /// 取消预约：恢复帖子状态为Available，恢复库存
    /// </summary>
    public async Task<ApiResponse> DeleteAsync(int id, int userId)
    {
        using var transaction = await _unitOfWork.BeginTransactionAsync();
        ReservationEntity? reservation = null;
        try
        {
            reservation = await _unitOfWork.Reservations.GetByIdAsync(id);
            if (reservation == null)
            {
                throw new KeyNotFoundException("预订不存在");
            }

            var post = await _unitOfWork.SharePosts.GetByIdAsync(reservation.PostId);
            if (reservation.ClaimerId != userId && post?.PosterId != userId)
            {
                throw new UnauthorizedAccessException("无权限取消此预订");
            }

            if (reservation.Status == ReservationStatus.Completed)
            {
                await _unitOfWork.RollbackTransactionAsync();
                return ApiResponse.Fail("已完成的预订无法取消");
            }

            if (reservation.Status != ReservationStatus.Cancelled)
            {
                reservation.Status = ReservationStatus.Cancelled;
                reservation.DeletedBy = userId;
                reservation.DeletionReason = "用户取消预订";
                _unitOfWork.Reservations.Update(reservation);

                if (post != null && reservation.Quantity > 0)
                {
                    var stockRestored = await _unitOfWork.SharePosts.TryIncrementReservedQuantityAsync(
                        reservation.PostId, reservation.Quantity);

                    if (stockRestored)
                    {
                        var refreshedPost = await _unitOfWork.SharePosts.GetByIdAsync(reservation.PostId);
                        if (refreshedPost != null)
                        {
                            var availableQuantity = refreshedPost.Quantity - refreshedPost.ReservedQuantity;
                            if (availableQuantity > 0 && refreshedPost.Status == SharePostStatus.Reserved)
                            {
                                refreshedPost.Status = SharePostStatus.Available;
                                _unitOfWork.SharePosts.Update(refreshedPost);
                            }
                        }
                    }
                }

                _unitOfWork.Reservations.Delete(reservation);
            }

            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransactionAsync();

            _logger.LogInformation(
                "预约已取消：ReservationId: {ReservationId}, PostId: {PostId}, UserId: {UserId}",
                id, reservation.PostId, userId);

            return ApiResponse.Success(null, "预订已取消");
        }
        catch (DbUpdateConcurrencyException ex)
        {
            var postId = reservation?.PostId ?? 0;
            _logger.LogWarning(ex, "取消预约时发生并发冲突，ReservationId: {ReservationId}, PostId: {PostId}", id, postId);
            throw ReservationException.ConcurrencyConflict(postId, 0);
        }
        catch
        {
            try
            {
                await _unitOfWork.RollbackTransactionAsync();
            }
            catch { /* 忽略回滚错误 */ }
            throw;
        }
    }

    /// <summary>
    /// 从回收站恢复预订
    /// </summary>
    public async Task<ApiResponse> RestoreAsync(int id, int userId)
    {
        var reservation = await _unitOfWork.Reservations.GetByIdIgnoreFilterAsync(id);
        if (reservation == null)
        {
            return ApiResponse.Fail("预订不存在", 404);
        }

        if (!reservation.IsDeleted)
        {
            return ApiResponse.Fail("预订未被删除，无需恢复");
        }

        var post = await _unitOfWork.SharePosts.GetByIdIgnoreFilterAsync(reservation.PostId);
        if (reservation.DeletedBy != userId && reservation.ClaimerId != userId && post?.PosterId != userId)
        {
            return ApiResponse.Fail("无权限恢复此预订", 403);
        }

        reservation.IsDeleted = false;
        reservation.DeletedAt = null;
        reservation.DeletedBy = null;
        reservation.DeletionReason = null;

        _unitOfWork.Reservations.Update(reservation);
        await _unitOfWork.SaveChangesAsync();

        return ApiResponse.Success(null, "预订恢复成功");
    }

    /// <summary>
    /// 查询回收站预订列表
    /// </summary>
    public async Task<ApiResponse> GetRecycleBinAsync(int userId, PagedRequest request)
    {
        var postIds = _unitOfWork.SharePosts.GetQueryableIgnoreFilter()
            .Where(sp => sp.PosterId == userId)
            .Select(sp => sp.Id)
            .ToList();

        var (items, totalCount) = await _unitOfWork.Reservations.GetDeletedPagedAsync(
            r => r.DeletedBy == userId || r.ClaimerId == userId || postIds.Contains(r.PostId),
            request.PageNumber, request.PageSize);

        var reservationResponses = _mapper.Map<List<ReservationResponse>>(items);
        var pagedResponse = new PagedResponse<ReservationResponse>
        {
            Items = reservationResponses,
            TotalCount = totalCount,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize,
            TotalPages = (int)Math.Ceiling((double)totalCount / request.PageSize)
        };

        return ApiResponse.Success(pagedResponse);
    }

    /// <summary>
    /// 更新预约状态：状态流转验证，Completed时发放积分给帖子发布者
    /// </summary>
    public async Task<ApiResponse> UpdateStatusAsync(int id, int userId, UpdateReservationStatusRequest request)
    {
        var reservation = await _unitOfWork.Reservations.GetByIdAsync(id);
        if (reservation == null)
        {
            return ApiResponse.Fail("预订不存在", 404);
        }

        var post = await _unitOfWork.SharePosts.GetByIdAsync(reservation.PostId);
        if (post?.PosterId != userId && reservation.ClaimerId != userId)
        {
            return ApiResponse.Fail("无权限修改此预订状态", 403);
        }

        if (!Enum.TryParse<ReservationStatus>(request.Status, true, out var newStatus))
        {
            return ApiResponse.Fail("无效的状态值");
        }

        if (!IsValidStatusTransition(reservation.Status, newStatus))
        {
            return ApiResponse.Fail($"无法从状态 {reservation.Status} 转换为 {newStatus}");
        }

        if (newStatus == ReservationStatus.Confirmed)
        {
            reservation.ConfirmedAt = DateTime.UtcNow;
        }

        if (newStatus == ReservationStatus.Completed)
        {
            if (post == null)
            {
                return ApiResponse.Fail("关联帖子不存在");
            }

            reservation.PickedUpAt = DateTime.UtcNow;

            var karmaPoint = new KarmaPointEntity
            {
                UserId = post.PosterId,
                Points = 10,
                Reason = "分享食物完成",
                RelatedId = post.Id,
                TransactionType = "Earn",
                CreatedAt = DateTime.UtcNow
            };
            await _unitOfWork.KarmaPoints.AddAsync(karmaPoint);

            var poster = await _unitOfWork.Users.GetByIdAsync(post.PosterId);
            if (poster != null)
            {
                poster.TotalKarmaPoints += 10;
                _unitOfWork.Users.Update(poster);
            }

            post.Status = SharePostStatus.PickedUp;
            _unitOfWork.SharePosts.Update(post);
        }

        reservation.Status = newStatus;
        _unitOfWork.Reservations.Update(reservation);

        await _unitOfWork.SaveChangesAsync();

        var reservationResponse = _mapper.Map<ReservationResponse>(reservation);
        return ApiResponse.Success(reservationResponse, "状态更新成功");
    }

    /// <summary>
    /// 校验预约状态流转合法性
    /// Pending → Confirmed / Cancelled
    /// Confirmed → Completed / Cancelled
    /// Completed / Cancelled → (终态)
    /// </summary>
    private static bool IsValidStatusTransition(ReservationStatus current, ReservationStatus target)
    {
        return current switch
        {
            ReservationStatus.Pending => target is ReservationStatus.Confirmed or ReservationStatus.Cancelled,
            ReservationStatus.Confirmed => target is ReservationStatus.Completed or ReservationStatus.Cancelled,
            ReservationStatus.Completed => false,
            ReservationStatus.Cancelled => false,
            _ => false
        };
    }
}
