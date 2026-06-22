using System.Linq.Expressions;
using System.Text.Json;
using AutoMapper;
using LeftoverShare.API.Data;
using LeftoverShare.API.DTOs.Common;
using LeftoverShare.API.DTOs.RecycleBin;
using LeftoverShare.API.Entities;
using LeftoverShare.API.Helpers;
using LeftoverShare.API.Repositories;
using Microsoft.EntityFrameworkCore;
using SnapshotEntity = LeftoverShare.API.Entities.DeletedEntitySnapshot;

namespace LeftoverShare.API.Services.Impl;

/// <summary>
/// 回收站服务实现
/// </summary>
public class RecycleBinService : IRecycleBinService
{
    private readonly AppDbContext _context;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private static readonly HashSet<string> ValidEntityTypes = new() { "SharePost", "Reservation", "PickupCode", "KarmaPoint" };

    public RecycleBinService(AppDbContext context, IUnitOfWork unitOfWork, IMapper mapper)
    {
        _context = context;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    /// <summary>
    /// 分页获取回收站列表：用户只能看到自己删除或作为原所有者的记录
    /// </summary>
    public async Task<ApiResponse> GetRecycleBinAsync(int userId, string? entityType, PagedRequest request)
    {
        if (!string.IsNullOrEmpty(entityType) && !ValidEntityTypes.Contains(entityType))
        {
            return ApiResponse.Fail($"无效的实体类型：{entityType}，仅支持 SharePost、Reservation、PickupCode、KarmaPoint");
        }

        var query = _context.DeletedEntitySnapshots.AsQueryable();

        query = query.Where(s => s.DeletedBy == userId || s.OriginalOwnerId == userId);

        if (!string.IsNullOrEmpty(entityType))
        {
            query = query.Where(s => s.EntityType == entityType);
        }

        if (!string.IsNullOrEmpty(request.SearchTerm))
        {
            query = query.Where(s => s.EntityDisplayName.Contains(request.SearchTerm) ||
                                     (s.DeletionReason != null && s.DeletionReason.Contains(request.SearchTerm)));
        }

        var totalCount = await query.CountAsync();

        query = request.SortBy?.ToLower() switch
        {
            "entitytype" => request.SortDirection?.ToLower() == "asc"
                ? query.OrderBy(s => s.EntityType)
                : query.OrderByDescending(s => s.EntityType),
            "entitydisplayname" => request.SortDirection?.ToLower() == "asc"
                ? query.OrderBy(s => s.EntityDisplayName)
                : query.OrderByDescending(s => s.EntityDisplayName),
            _ => request.SortDirection?.ToLower() == "asc"
                ? query.OrderBy(s => s.DeletedAt)
                : query.OrderByDescending(s => s.DeletedAt)
        };

        var items = await query
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync();

        var responseItems = items.Select(s => new RecycleBinItemResponse
        {
            Id = s.Id,
            EntityType = s.EntityType,
            EntityId = s.EntityId,
            EntityDisplayName = s.EntityDisplayName,
            SnapshotDataPreview = GetSnapshotPreview(s.SnapshotData),
            DeletedBy = s.DeletedBy,
            DeletedAt = s.DeletedAt,
            DeletionReason = s.DeletionReason,
            OriginalOwnerId = s.OriginalOwnerId
        }).ToList();

        var pagedResponse = new PagedResponse<RecycleBinItemResponse>
        {
            Items = responseItems,
            TotalCount = totalCount,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize,
            TotalPages = (int)Math.Ceiling((double)totalCount / request.PageSize)
        };

        return ApiResponse.Success(pagedResponse);
    }

    /// <summary>
    /// 恢复已删除的实体：根据快照信息恢复软删除的实体
    /// </summary>
    public async Task<ApiResponse> RestoreAsync(int userId, RestoreRequest request)
    {
        if (!ValidEntityTypes.Contains(request.EntityType))
        {
            return ApiResponse.Fail($"无效的实体类型：{request.EntityType}，仅支持 SharePost、Reservation、PickupCode、KarmaPoint");
        }

        var snapshot = await _context.DeletedEntitySnapshots
            .FirstOrDefaultAsync(s => s.Id == request.Id && s.EntityType == request.EntityType);

        if (snapshot == null)
        {
            return ApiResponse.Fail("快照不存在", 404);
        }

        if (snapshot.DeletedBy != userId && snapshot.OriginalOwnerId != userId)
        {
            return ApiResponse.Fail("无权限恢复此记录", 403);
        }

        var restoreResult = request.EntityType switch
        {
            "SharePost" => await RestoreSharePostAsync(snapshot.EntityId),
            "Reservation" => await RestoreReservationAsync(snapshot.EntityId),
            "PickupCode" => await RestorePickupCodeAsync(snapshot.EntityId),
            "KarmaPoint" => await RestoreKarmaPointAsync(snapshot.EntityId),
            _ => ApiResponse.Fail("不支持的实体类型")
        };

        if (restoreResult.Code == 200)
        {
            _context.DeletedEntitySnapshots.Remove(snapshot);
            await _context.SaveChangesAsync();
        }

        return restoreResult;
    }

    /// <summary>
    /// 获取快照详情
    /// </summary>
    public async Task<ApiResponse> GetSnapshotAsync(int userId, int snapshotId)
    {
        var snapshot = await _context.DeletedEntitySnapshots.FirstOrDefaultAsync(s => s.Id == snapshotId);
        if (snapshot == null)
        {
            return ApiResponse.Fail("快照不存在", 404);
        }

        if (snapshot.DeletedBy != userId && snapshot.OriginalOwnerId != userId)
        {
            return ApiResponse.Fail("无权限查看此快照", 403);
        }

        try
        {
            var snapshotData = JsonSerializer.Deserialize<Dictionary<string, object?>>(snapshot.SnapshotData);
            return ApiResponse.Success(new
            {
                snapshot.Id,
                snapshot.EntityType,
                snapshot.EntityId,
                snapshot.EntityDisplayName,
                SnapshotData = snapshotData,
                snapshot.DeletedBy,
                snapshot.DeletedAt,
                snapshot.DeletionReason,
                snapshot.OriginalOwnerId
            });
        }
        catch (JsonException)
        {
            return ApiResponse.Success(new
            {
                snapshot.Id,
                snapshot.EntityType,
                snapshot.EntityId,
                snapshot.EntityDisplayName,
                SnapshotData = snapshot.SnapshotData,
                snapshot.DeletedBy,
                snapshot.DeletedAt,
                snapshot.DeletionReason,
                snapshot.OriginalOwnerId
            });
        }
    }

    /// <summary>
    /// 永久删除快照（仅供管理员使用，此处默认第一个用户为管理员）
    /// </summary>
    public async Task<ApiResponse> PermanentlyDeleteAsync(int userId, int snapshotId)
    {
        if (!await IsAdminAsync(userId))
        {
            return ApiResponse.Fail("无权限执行此操作，仅管理员可永久删除", 403);
        }

        var snapshot = await _context.DeletedEntitySnapshots.FirstOrDefaultAsync(s => s.Id == snapshotId);
        if (snapshot == null)
        {
            return ApiResponse.Fail("快照不存在", 404);
        }

        _context.DeletedEntitySnapshots.Remove(snapshot);
        await _context.SaveChangesAsync();

        return ApiResponse.Success(null, "快照已永久删除");
    }

    /// <summary>
    /// 生成快照预览（截取关键信息）
    /// </summary>
    private static string? GetSnapshotPreview(string snapshotData)
    {
        if (string.IsNullOrEmpty(snapshotData))
            return null;

        try
        {
            var data = JsonSerializer.Deserialize<Dictionary<string, object?>>(snapshotData);
            if (data == null)
                return snapshotData.Length > 200 ? snapshotData[..200] + "..." : snapshotData;

            var previewKeys = new[] { "Title", "Description", "Reason", "Status", "Quantity", "FoodType", "Code" };
            var previewItems = data
                .Where(kv => previewKeys.Contains(kv.Key) && kv.Value != null)
                .Take(3)
                .Select(kv => $"{kv.Key}: {kv.Value}");

            var preview = string.Join(", ", previewItems);
            return string.IsNullOrEmpty(preview)
                ? (snapshotData.Length > 200 ? snapshotData[..200] + "..." : snapshotData)
                : preview;
        }
        catch
        {
            return snapshotData.Length > 200 ? snapshotData[..200] + "..." : snapshotData;
        }
    }

    /// <summary>
    /// 恢复分享帖子
    /// </summary>
    private async Task<ApiResponse> RestoreSharePostAsync(int entityId)
    {
        var post = await _unitOfWork.SharePosts.GetByIdIgnoreFilterAsync(entityId);
        if (post == null)
        {
            return ApiResponse.Fail("分享帖子不存在或已被物理删除", 404);
        }

        if (!post.IsDeleted)
        {
            return ApiResponse.Fail("分享帖子未被删除，无需恢复");
        }

        post.IsDeleted = false;
        post.DeletedAt = null;
        post.DeletedBy = null;
        post.DeletionReason = null;

        _unitOfWork.SharePosts.Update(post);
        await _unitOfWork.SaveChangesAsync();

        var response = _mapper.Map<DTOs.SharePosts.SharePostResponse>(post);
        return ApiResponse.Success(response, "分享帖子恢复成功");
    }

    /// <summary>
    /// 恢复预订记录
    /// </summary>
    private async Task<ApiResponse> RestoreReservationAsync(int entityId)
    {
        var reservation = await _unitOfWork.Reservations.GetByIdIgnoreFilterAsync(entityId);
        if (reservation == null)
        {
            return ApiResponse.Fail("预订记录不存在或已被物理删除", 404);
        }

        if (!reservation.IsDeleted)
        {
            return ApiResponse.Fail("预订记录未被删除，无需恢复");
        }

        reservation.IsDeleted = false;
        reservation.DeletedAt = null;
        reservation.DeletedBy = null;
        reservation.DeletionReason = null;

        _unitOfWork.Reservations.Update(reservation);
        await _unitOfWork.SaveChangesAsync();

        var response = _mapper.Map<DTOs.Reservations.ReservationResponse>(reservation);
        return ApiResponse.Success(response, "预订记录恢复成功");
    }

    /// <summary>
    /// 恢复取餐码
    /// </summary>
    private async Task<ApiResponse> RestorePickupCodeAsync(int entityId)
    {
        var pickupCode = await _unitOfWork.PickupCodes.GetByIdIgnoreFilterAsync(entityId);
        if (pickupCode == null)
        {
            return ApiResponse.Fail("取餐码不存在或已被物理删除", 404);
        }

        if (!pickupCode.IsDeleted)
        {
            return ApiResponse.Fail("取餐码未被删除，无需恢复");
        }

        pickupCode.IsDeleted = false;
        pickupCode.DeletedAt = null;
        pickupCode.DeletedBy = null;
        pickupCode.DeletionReason = null;

        _unitOfWork.PickupCodes.Update(pickupCode);
        await _unitOfWork.SaveChangesAsync();

        var response = _mapper.Map<DTOs.PickupCodes.PickupCodeResponse>(pickupCode);
        return ApiResponse.Success(response, "取餐码恢复成功");
    }

    /// <summary>
    /// 恢复积分流水
    /// </summary>
    private async Task<ApiResponse> RestoreKarmaPointAsync(int entityId)
    {
        var karmaPoint = await _unitOfWork.KarmaPoints.GetByIdIgnoreFilterAsync(entityId);
        if (karmaPoint == null)
        {
            return ApiResponse.Fail("积分流水不存在或已被物理删除", 404);
        }

        if (!karmaPoint.IsDeleted)
        {
            return ApiResponse.Fail("积分流水未被删除，无需恢复");
        }

        karmaPoint.IsDeleted = false;
        karmaPoint.DeletedAt = null;
        karmaPoint.DeletedBy = null;
        karmaPoint.DeletionReason = null;

        _unitOfWork.KarmaPoints.Update(karmaPoint);
        await _unitOfWork.SaveChangesAsync();

        var response = _mapper.Map<DTOs.KarmaPoints.KarmaPointResponse>(karmaPoint);
        return ApiResponse.Success(response, "积分流水恢复成功");
    }

    /// <summary>
    /// 检查用户是否为管理员（默认ID为1的用户为管理员，可根据需求扩展）
    /// </summary>
    private async Task<bool> IsAdminAsync(int userId)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId);
        if (user == null)
            return false;

        return userId == 1;
    }
}
