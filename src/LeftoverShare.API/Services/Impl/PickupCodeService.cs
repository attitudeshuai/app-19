using AutoMapper;
using LeftoverShare.API.DTOs.Common;
using LeftoverShare.API.DTOs.PickupCodes;
using LeftoverShare.API.Helpers;
using LeftoverShare.API.Repositories;
using PickupCodeEntity = LeftoverShare.API.Entities.PickupCode;

namespace LeftoverShare.API.Services.Impl;

/// <summary>
/// 取餐码服务实现
/// </summary>
public class PickupCodeService : IPickupCodeService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public PickupCodeService(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    /// <summary>
    /// 分页获取取餐码列表
    /// </summary>
    public async Task<ApiResponse> GetPagedAsync(PagedRequest request)
    {
        var (items, totalCount) = await _unitOfWork.PickupCodes.GetPagedAsync(
            request.PageNumber, request.PageSize);

        var pickupCodeResponses = _mapper.Map<List<PickupCodeResponse>>(items);
        var pagedResponse = new PagedResponse<PickupCodeResponse>
        {
            Items = pickupCodeResponses,
            TotalCount = totalCount,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize,
            TotalPages = (int)Math.Ceiling((double)totalCount / request.PageSize)
        };

        return ApiResponse.Success(pagedResponse);
    }

    /// <summary>
    /// 根据ID获取取餐码详情
    /// </summary>
    public async Task<ApiResponse> GetByIdAsync(int id)
    {
        var pickupCode = await _unitOfWork.PickupCodes.GetByIdAsync(id);
        if (pickupCode == null)
        {
            return ApiResponse.Fail("取餐码不存在", 404);
        }

        var pickupCodeResponse = _mapper.Map<PickupCodeResponse>(pickupCode);
        return ApiResponse.Success(pickupCodeResponse);
    }

    /// <summary>
    /// 创建取餐码：生成6位随机码，ExpiresAt=24h后
    /// </summary>
    public async Task<ApiResponse> CreateAsync(int userId, CreatePickupCodeRequest request)
    {
        var reservation = await _unitOfWork.Reservations.GetByIdAsync(request.ReservationId);
        if (reservation == null)
        {
            return ApiResponse.Fail("预订不存在", 404);
        }

        var existingCode = await _unitOfWork.PickupCodes.GetByReservationIdAsync(request.ReservationId);
        if (existingCode != null && !existingCode.IsUsed)
        {
            return ApiResponse.Fail("该预订已有有效的取餐码");
        }

        string code;
        do
        {
            code = PickupCodeGenerator.Generate();
        } while (await _unitOfWork.PickupCodes.GetByCodeAsync(code) != null);

        var pickupCode = new PickupCodeEntity
        {
            ReservationId = request.ReservationId,
            Code = code,
            ExpiresAt = DateTime.UtcNow.AddHours(24),
            IsUsed = false
        };

        await _unitOfWork.PickupCodes.AddAsync(pickupCode);
        await _unitOfWork.SaveChangesAsync();

        var pickupCodeResponse = _mapper.Map<PickupCodeResponse>(pickupCode);
        return ApiResponse.Success(pickupCodeResponse, "取餐码生成成功");
    }

    /// <summary>
    /// 删除取餐码：权限检查（预约者或帖子发布者）
    /// </summary>
    public async Task<ApiResponse> DeleteAsync(int id, int userId)
    {
        var pickupCode = await _unitOfWork.PickupCodes.GetByIdAsync(id);
        if (pickupCode == null)
        {
            return ApiResponse.Fail("取餐码不存在", 404);
        }

        var reservation = await _unitOfWork.Reservations.GetByIdAsync(pickupCode.ReservationId);
        if (reservation == null)
        {
            return ApiResponse.Fail("关联预订不存在", 404);
        }

        var post = await _unitOfWork.SharePosts.GetByIdAsync(reservation.PostId);
        if (reservation.ClaimerId != userId && post?.PosterId != userId)
        {
            return ApiResponse.Fail("无权限删除此取餐码", 403);
        }

        _unitOfWork.PickupCodes.Delete(pickupCode);
        await _unitOfWork.SaveChangesAsync();

        return ApiResponse.Success(null, "取餐码已删除");
    }
}
