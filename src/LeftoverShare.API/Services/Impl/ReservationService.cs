using AutoMapper;
using LeftoverShare.API.DTOs.Common;
using LeftoverShare.API.DTOs.Reservations;
using LeftoverShare.API.Entities.Enums;
using LeftoverShare.API.Helpers;
using LeftoverShare.API.Repositories;
using KarmaPointEntity = LeftoverShare.API.Entities.KarmaPoint;
using PickupCodeEntity = LeftoverShare.API.Entities.PickupCode;
using ReservationEntity = LeftoverShare.API.Entities.Reservation;

namespace LeftoverShare.API.Services.Impl;

/// <summary>
/// 预约服务实现
/// </summary>
public class ReservationService : IReservationService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public ReservationService(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
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
    /// 创建预约：检查帖子Available状态，不能预约自己的帖子，设置PostId/ClaimerId，
    /// 生成取餐码，更新帖子状态为Reserved
    /// </summary>
    public async Task<ApiResponse> CreateAsync(int userId, CreateReservationRequest request)
    {
        var post = await _unitOfWork.SharePosts.GetByIdAsync(request.PostId);
        if (post == null)
        {
            return ApiResponse.Fail("帖子不存在", 404);
        }

        if (post.Status != SharePostStatus.Available)
        {
            return ApiResponse.Fail("该帖子当前不可预订");
        }

        if (post.PosterId == userId)
        {
            return ApiResponse.Fail("不能预订自己发布的帖子");
        }

        var pickupCode = PickupCodeGenerator.Generate();

        var reservation = new ReservationEntity
        {
            PostId = request.PostId,
            ClaimerId = userId,
            Quantity = 1,
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

        post.Status = SharePostStatus.Reserved;
        _unitOfWork.SharePosts.Update(post);

        await _unitOfWork.SaveChangesAsync();

        var reservationResponse = _mapper.Map<ReservationResponse>(reservation);
        return ApiResponse.Success(reservationResponse, "预订成功");
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
    /// 取消预约：恢复帖子状态为Available
    /// </summary>
    public async Task<ApiResponse> DeleteAsync(int id, int userId)
    {
        var reservation = await _unitOfWork.Reservations.GetByIdAsync(id);
        if (reservation == null)
        {
            return ApiResponse.Fail("预订不存在", 404);
        }

        var post = await _unitOfWork.SharePosts.GetByIdAsync(reservation.PostId);
        if (reservation.ClaimerId != userId && post?.PosterId != userId)
        {
            return ApiResponse.Fail("无权限取消此预订", 403);
        }

        if (reservation.Status == ReservationStatus.Completed)
        {
            return ApiResponse.Fail("已完成的预订无法取消");
        }

        reservation.Status = ReservationStatus.Cancelled;
        _unitOfWork.Reservations.Update(reservation);

        if (post != null)
        {
            post.Status = SharePostStatus.Available;
            _unitOfWork.SharePosts.Update(post);
        }

        await _unitOfWork.SaveChangesAsync();

        return ApiResponse.Success(null, "预订已取消");
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
