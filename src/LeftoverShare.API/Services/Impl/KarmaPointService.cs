using AutoMapper;
using LeftoverShare.API.DTOs.Common;
using LeftoverShare.API.DTOs.KarmaPoints;
using LeftoverShare.API.Helpers;
using LeftoverShare.API.Repositories;
using KarmaPointEntity = LeftoverShare.API.Entities.KarmaPoint;
using UserEntity = LeftoverShare.API.Entities.User;

namespace LeftoverShare.API.Services.Impl;

/// <summary>
/// 积分服务实现
/// </summary>
public class KarmaPointService : IKarmaPointService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public KarmaPointService(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    /// <summary>
    /// 分页获取所有积分记录
    /// </summary>
    public async Task<ApiResponse> GetPagedAsync(PagedRequest request)
    {
        var (items, totalCount) = await _unitOfWork.KarmaPoints.GetPagedAsync(
            request.PageNumber, request.PageSize);

        var karmaPointResponses = _mapper.Map<List<KarmaPointResponse>>(items);
        var pagedResponse = new PagedResponse<KarmaPointResponse>
        {
            Items = karmaPointResponses,
            TotalCount = totalCount,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize,
            TotalPages = (int)Math.Ceiling((double)totalCount / request.PageSize)
        };

        return ApiResponse.Success(pagedResponse);
    }

    /// <summary>
    /// 根据ID获取积分记录详情
    /// </summary>
    public async Task<ApiResponse> GetByIdAsync(int id)
    {
        var karmaPoint = await _unitOfWork.KarmaPoints.GetByIdAsync(id);
        if (karmaPoint == null)
        {
            return ApiResponse.Fail("积分记录不存在", 404);
        }

        var karmaPointResponse = _mapper.Map<KarmaPointResponse>(karmaPoint);
        return ApiResponse.Success(karmaPointResponse);
    }

    /// <summary>
    /// 获取用户积分列表（分页）
    /// </summary>
    public async Task<ApiResponse> GetMineAsync(int userId, PagedRequest request)
    {
        var allItems = await _unitOfWork.KarmaPoints.GetByUserIdAsync(userId);
        var totalCount = allItems.Count();
        var items = allItems
            .OrderByDescending(kp => kp.CreatedAt)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToList();

        var karmaPointResponses = _mapper.Map<List<KarmaPointResponse>>(items);
        var pagedResponse = new PagedResponse<KarmaPointResponse>
        {
            Items = karmaPointResponses,
            TotalCount = totalCount,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize,
            TotalPages = (int)Math.Ceiling((double)totalCount / request.PageSize)
        };

        return ApiResponse.Success(pagedResponse);
    }

    /// <summary>
    /// 创建积分记录：同时更新用户TotalKarmaPoints
    /// </summary>
    public async Task<ApiResponse> CreateAsync(CreateKarmaPointRequest request)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(request.UserId);
        if (user == null)
        {
            return ApiResponse.Fail("用户不存在", 404);
        }

        var karmaPoint = new KarmaPointEntity
        {
            UserId = request.UserId,
            Points = request.Points,
            Reason = request.Reason,
            RelatedId = request.RelatedId,
            TransactionType = request.Points >= 0 ? "Earn" : "Spend",
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.KarmaPoints.AddAsync(karmaPoint);

        user.TotalKarmaPoints += request.Points;
        _unitOfWork.Users.Update(user);

        await _unitOfWork.SaveChangesAsync();

        var karmaPointResponse = _mapper.Map<KarmaPointResponse>(karmaPoint);
        return ApiResponse.Success(karmaPointResponse, "积分记录创建成功");
    }

    /// <summary>
    /// 更新积分记录：差额更新TotalKarmaPoints
    /// </summary>
    public async Task<ApiResponse> UpdateAsync(int id, UpdateKarmaPointRequest request)
    {
        var karmaPoint = await _unitOfWork.KarmaPoints.GetByIdAsync(id);
        if (karmaPoint == null)
        {
            return ApiResponse.Fail("积分记录不存在", 404);
        }

        var user = await _unitOfWork.Users.GetByIdAsync(karmaPoint.UserId);
        if (user == null)
        {
            return ApiResponse.Fail("关联用户不存在", 404);
        }

        var pointsDiff = request.Points - karmaPoint.Points;

        karmaPoint.Points = request.Points;
        karmaPoint.Reason = request.Reason;
        karmaPoint.RelatedId = request.RelatedId;
        karmaPoint.TransactionType = request.Points >= 0 ? "Earn" : "Spend";

        _unitOfWork.KarmaPoints.Update(karmaPoint);

        user.TotalKarmaPoints += pointsDiff;
        _unitOfWork.Users.Update(user);

        await _unitOfWork.SaveChangesAsync();

        var karmaPointResponse = _mapper.Map<KarmaPointResponse>(karmaPoint);
        return ApiResponse.Success(karmaPointResponse, "积分记录更新成功");
    }

    /// <summary>
    /// 删除积分记录：回滚TotalKarmaPoints
    /// </summary>
    public async Task<ApiResponse> DeleteAsync(int id)
    {
        var karmaPoint = await _unitOfWork.KarmaPoints.GetByIdAsync(id);
        if (karmaPoint == null)
        {
            return ApiResponse.Fail("积分记录不存在", 404);
        }

        var user = await _unitOfWork.Users.GetByIdAsync(karmaPoint.UserId);
        if (user != null)
        {
            user.TotalKarmaPoints -= karmaPoint.Points;
            _unitOfWork.Users.Update(user);
        }

        karmaPoint.DeletedBy = karmaPoint.UserId;
        karmaPoint.DeletionReason = "用户主动删除";

        _unitOfWork.KarmaPoints.Delete(karmaPoint);
        await _unitOfWork.SaveChangesAsync();

        return ApiResponse.Success(null, "积分记录已删除");
    }

    /// <summary>
    /// 从回收站恢复积分记录：恢复TotalKarmaPoints
    /// </summary>
    public async Task<ApiResponse> RestoreAsync(int id, int userId)
    {
        var karmaPoint = await _unitOfWork.KarmaPoints.GetByIdIgnoreFilterAsync(id);
        if (karmaPoint == null)
        {
            return ApiResponse.Fail("积分记录不存在", 404);
        }

        if (!karmaPoint.IsDeleted)
        {
            return ApiResponse.Fail("积分记录未被删除，无需恢复");
        }

        if (karmaPoint.DeletedBy != userId && karmaPoint.UserId != userId)
        {
            return ApiResponse.Fail("无权限恢复此积分记录", 403);
        }

        var user = await _unitOfWork.Users.GetByIdAsync(karmaPoint.UserId);
        if (user != null)
        {
            user.TotalKarmaPoints += karmaPoint.Points;
            _unitOfWork.Users.Update(user);
        }

        karmaPoint.IsDeleted = false;
        karmaPoint.DeletedAt = null;
        karmaPoint.DeletedBy = null;
        karmaPoint.DeletionReason = null;

        _unitOfWork.KarmaPoints.Update(karmaPoint);
        await _unitOfWork.SaveChangesAsync();

        return ApiResponse.Success(null, "积分记录恢复成功");
    }

    /// <summary>
    /// 查询回收站积分记录列表
    /// </summary>
    public async Task<ApiResponse> GetRecycleBinAsync(int userId, PagedRequest request)
    {
        var (items, totalCount) = await _unitOfWork.KarmaPoints.GetDeletedPagedAsync(
            kp => kp.DeletedBy == userId || kp.UserId == userId,
            request.PageNumber, request.PageSize);

        var karmaPointResponses = _mapper.Map<List<KarmaPointResponse>>(items);
        var pagedResponse = new PagedResponse<KarmaPointResponse>
        {
            Items = karmaPointResponses,
            TotalCount = totalCount,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize,
            TotalPages = (int)Math.Ceiling((double)totalCount / request.PageSize)
        };

        return ApiResponse.Success(pagedResponse);
    }
}
