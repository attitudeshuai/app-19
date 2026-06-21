using AutoMapper;
using LeftoverShare.API.DTOs.Common;
using LeftoverShare.API.DTOs.SharePosts;
using LeftoverShare.API.Entities.Enums;
using LeftoverShare.API.Helpers;
using LeftoverShare.API.Repositories;
using SharePostEntity = LeftoverShare.API.Entities.SharePost;

namespace LeftoverShare.API.Services.Impl;

/// <summary>
/// 分享帖子服务实现
/// </summary>
public class SharePostService : ISharePostService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public SharePostService(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    /// <summary>
    /// 分页获取分享帖子列表
    /// </summary>
    public async Task<ApiResponse> GetPagedAsync(PagedRequest request)
    {
        var (items, totalCount) = await _unitOfWork.SharePosts.GetPagedWithDetailsAsync(
            request.PageNumber, request.PageSize);

        var postResponses = _mapper.Map<List<SharePostListResponse>>(items);
        var pagedResponse = new PagedResponse<SharePostListResponse>
        {
            Items = postResponses,
            TotalCount = totalCount,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize,
            TotalPages = (int)Math.Ceiling((double)totalCount / request.PageSize)
        };

        return ApiResponse.Success(pagedResponse);
    }

    /// <summary>
    /// 根据ID获取帖子详情，Include发布者信息
    /// </summary>
    public async Task<ApiResponse> GetByIdAsync(int id)
    {
        var post = await _unitOfWork.SharePosts.GetByIdAsync(id);
        if (post == null)
        {
            return ApiResponse.Fail("帖子不存在", 404);
        }

        var postResponse = _mapper.Map<SharePostResponse>(post);
        return ApiResponse.Success(postResponse);
    }

    /// <summary>
    /// 创建帖子：PosterId=userId，Status=Available（枚举值）
    /// </summary>
    public async Task<ApiResponse> CreateAsync(int userId, CreateSharePostRequest request)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId);
        if (user == null)
        {
            return ApiResponse.Fail("用户不存在", 404);
        }

        var post = _mapper.Map<SharePostEntity>(request);
        post.PosterId = userId;
        post.Status = SharePostStatus.Available;
        post.CreatedAt = DateTime.UtcNow;

        await _unitOfWork.SharePosts.AddAsync(post);
        await _unitOfWork.SaveChangesAsync();

        var postResponse = _mapper.Map<SharePostResponse>(post);
        return ApiResponse.Success(postResponse, "帖子发布成功");
    }

    /// <summary>
    /// 更新帖子：权限检查(post.PosterId==userId)
    /// </summary>
    public async Task<ApiResponse> UpdateAsync(int id, int userId, UpdateSharePostRequest request)
    {
        var post = await _unitOfWork.SharePosts.GetByIdAsync(id);
        if (post == null)
        {
            return ApiResponse.Fail("帖子不存在", 404);
        }

        if (post.PosterId != userId)
        {
            return ApiResponse.Fail("无权限修改此帖子", 403);
        }

        _mapper.Map(request, post);
        _unitOfWork.SharePosts.Update(post);
        await _unitOfWork.SaveChangesAsync();

        var postResponse = _mapper.Map<SharePostResponse>(post);
        return ApiResponse.Success(postResponse, "帖子更新成功");
    }

    /// <summary>
    /// 删除帖子：权限检查(post.PosterId==userId)
    /// </summary>
    public async Task<ApiResponse> DeleteAsync(int id, int userId)
    {
        var post = await _unitOfWork.SharePosts.GetByIdAsync(id);
        if (post == null)
        {
            return ApiResponse.Fail("帖子不存在", 404);
        }

        if (post.PosterId != userId)
        {
            return ApiResponse.Fail("无权限删除此帖子", 403);
        }

        _unitOfWork.SharePosts.Delete(post);
        await _unitOfWork.SaveChangesAsync();

        return ApiResponse.Success(null, "帖子删除成功");
    }

    /// <summary>
    /// 更新帖子状态：权限检查，枚举状态流转验证
    /// Available → Reserved / Expired
    /// Reserved → Available / PickedUp
    /// PickedUp → (终态)
    /// Expired → (终态)
    /// </summary>
    public async Task<ApiResponse> UpdateStatusAsync(int id, int userId, UpdateSharePostStatusRequest request)
    {
        var post = await _unitOfWork.SharePosts.GetByIdAsync(id);
        if (post == null)
        {
            return ApiResponse.Fail("帖子不存在", 404);
        }

        if (post.PosterId != userId)
        {
            return ApiResponse.Fail("无权限修改此帖子状态", 403);
        }

        if (!Enum.TryParse<SharePostStatus>(request.Status, true, out var newStatus))
        {
            return ApiResponse.Fail("无效的状态值");
        }

        if (!IsValidStatusTransition(post.Status, newStatus))
        {
            return ApiResponse.Fail($"无法从状态 {post.Status} 转换为 {newStatus}");
        }

        post.Status = newStatus;
        _unitOfWork.SharePosts.Update(post);
        await _unitOfWork.SaveChangesAsync();

        var postResponse = _mapper.Map<SharePostResponse>(post);
        return ApiResponse.Success(postResponse, "状态更新成功");
    }

    /// <summary>
    /// 校验帖子状态流转合法性
    /// </summary>
    private static bool IsValidStatusTransition(SharePostStatus current, SharePostStatus target)
    {
        return current switch
        {
            SharePostStatus.Available => target is SharePostStatus.Reserved or SharePostStatus.Expired,
            SharePostStatus.Reserved => target is SharePostStatus.Available or SharePostStatus.PickedUp,
            SharePostStatus.PickedUp => false,
            SharePostStatus.Expired => false,
            _ => false
        };
    }
}
