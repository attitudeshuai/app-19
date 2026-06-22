using AutoMapper;
using LeftoverShare.API.DTOs.Common;
using LeftoverShare.API.DTOs.PostTags;
using LeftoverShare.API.Entities;
using LeftoverShare.API.Entities.Enums;
using LeftoverShare.API.Helpers;
using LeftoverShare.API.Repositories;

namespace LeftoverShare.API.Services.Impl;

/// <summary>
/// 帖子标签服务实现
/// </summary>
public class PostTagService : IPostTagService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public PostTagService(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<ApiResponse> GetAllActiveAsync(bool includeUserDefined = true)
    {
        var tags = await _unitOfWork.PostTags.GetAllActiveAsync(includeUserDefined);
        var dtos = _mapper.Map<List<PostTagResponse>>(tags);
        return ApiResponse.Success(dtos, "获取帖子标签成功");
    }

    public async Task<ApiResponse> GetPopularAsync(int topN = 20)
    {
        if (topN < 1) topN = 1;
        if (topN > 100) topN = 100;

        var tags = await _unitOfWork.PostTags.GetPopularAsync(topN);
        var dtos = _mapper.Map<List<PostTagResponse>>(tags);
        return ApiResponse.Success(dtos, "获取热门标签成功");
    }

    public async Task<ApiResponse> GetPagedAsync(int pageNumber, int pageSize, bool includeInactive = false, bool? isSystemDefined = null, int? createdBy = null, string? searchTerm = null)
    {
        if (pageNumber < 1) pageNumber = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 10;

        var query = await _unitOfWork.PostTags.GetAllAsync();
        IEnumerable<PostTag> filtered = query;

        if (!includeInactive)
        {
            filtered = filtered.Where(t => t.IsActive);
        }

        if (isSystemDefined.HasValue)
        {
            filtered = filtered.Where(t => t.IsSystemDefined == isSystemDefined.Value);
        }

        if (createdBy.HasValue)
        {
            filtered = filtered.Where(t => t.CreatedBy == createdBy.Value);
        }

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim().ToLower();
            filtered = filtered.Where(t =>
                t.Name.ToLower().Contains(term) ||
                t.Code.ToLower().Contains(term) ||
                (t.Description != null && t.Description.ToLower().Contains(term)));
        }

        var ordered = filtered.OrderBy(t => t.SortOrder).ThenByDescending(t => t.UsageCount).ThenBy(t => t.Name).ToList();

        var totalCount = ordered.Count;
        var items = ordered.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();

        var dtos = _mapper.Map<List<PostTagResponse>>(items);

        var pagedResponse = new PagedResponse<PostTagResponse>
        {
            Items = dtos,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)
        };

        return ApiResponse.Success(pagedResponse, "获取分页列表成功");
    }

    public async Task<ApiResponse> GetByIdAsync(int id)
    {
        var tag = await _unitOfWork.PostTags.GetByIdAsync(id);
        if (tag == null)
        {
            return ApiResponse.Fail("帖子标签不存在", 404);
        }

        var dto = _mapper.Map<PostTagResponse>(tag);
        return ApiResponse.Success(dto, "获取标签详情成功");
    }

    public async Task<ApiResponse> SearchAsync(string keyword, int limit = 20)
    {
        if (string.IsNullOrWhiteSpace(keyword))
        {
            return await GetPopularAsync(limit);
        }

        var tags = await _unitOfWork.PostTags.SearchByNameAsync(keyword.Trim(), limit);
        var dtos = _mapper.Map<List<PostTagResponse>>(tags);
        return ApiResponse.Success(dtos, "搜索标签成功");
    }

    public async Task<ApiResponse> GetMyTagsAsync(int userId)
    {
        var tags = await _unitOfWork.PostTags.GetByUserIdAsync(userId);
        var dtos = _mapper.Map<List<PostTagResponse>>(tags);
        return ApiResponse.Success(dtos, "获取我的标签成功");
    }

    public async Task<ApiResponse> CreateSystemTagAsync(int currentUserId, CreatePostTagRequest request)
    {
        if (!await IsAdmin(currentUserId))
        {
            return ApiResponse.Fail("无权限操作，仅管理员可创建系统预设标签", 403);
        }

        var existing = await _unitOfWork.PostTags.GetByCodeAsync(request.Code);
        if (existing != null)
        {
            return ApiResponse.Fail($"标签编码 {request.Code} 已存在");
        }

        var tag = _mapper.Map<PostTag>(request);
        tag.IsSystemDefined = true;
        tag.CreatedBy = null;
        tag.CreatedAt = DateTime.UtcNow;

        await _unitOfWork.PostTags.AddAsync(tag);
        await _unitOfWork.SaveChangesAsync();

        var dto = _mapper.Map<PostTagResponse>(tag);
        return ApiResponse.Success(dto, "系统标签创建成功");
    }

    public async Task<ApiResponse> CreateUserTagAsync(int currentUserId, CreatePostTagRequest request)
    {
        var existing = await _unitOfWork.PostTags.GetByCodeAsync(request.Code);
        if (existing != null)
        {
            return ApiResponse.Fail($"标签编码 {request.Code} 已存在");
        }

        var tag = _mapper.Map<PostTag>(request);
        tag.IsSystemDefined = false;
        tag.CreatedBy = currentUserId;
        tag.CreatedAt = DateTime.UtcNow;

        await _unitOfWork.PostTags.AddAsync(tag);
        await _unitOfWork.SaveChangesAsync();

        var dto = _mapper.Map<PostTagResponse>(tag);
        return ApiResponse.Success(dto, "自定义标签创建成功");
    }

    public async Task<ApiResponse> UpdateAsync(int id, int currentUserId, UpdatePostTagRequest request)
    {
        var tag = await _unitOfWork.PostTags.GetByIdAsync(id);
        if (tag == null)
        {
            return ApiResponse.Fail("帖子标签不存在", 404);
        }

        var isAdmin = await IsAdmin(currentUserId);
        var isCreator = tag.CreatedBy == currentUserId;

        if (!isAdmin && !isCreator)
        {
            return ApiResponse.Fail("无权限操作，仅管理员或标签创建者可修改", 403);
        }

        if (tag.IsSystemDefined && !isAdmin)
        {
            return ApiResponse.Fail("系统预设标签仅管理员可修改", 403);
        }

        var existing = await _unitOfWork.PostTags.GetByCodeAsync(request.Code);
        if (existing != null && existing.Id != id)
        {
            return ApiResponse.Fail($"标签编码 {request.Code} 已被其他标签使用");
        }

        _mapper.Map(request, tag);
        tag.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.PostTags.Update(tag);
        await _unitOfWork.SaveChangesAsync();

        var dto = _mapper.Map<PostTagResponse>(tag);
        return ApiResponse.Success(dto, "标签更新成功");
    }

    public async Task<ApiResponse> DeleteAsync(int id, int currentUserId)
    {
        var tag = await _unitOfWork.PostTags.GetByIdAsync(id);
        if (tag == null)
        {
            return ApiResponse.Fail("帖子标签不存在", 404);
        }

        var isAdmin = await IsAdmin(currentUserId);
        var isCreator = tag.CreatedBy == currentUserId;

        if (!isAdmin && !isCreator)
        {
            return ApiResponse.Fail("无权限操作，仅管理员或标签创建者可删除", 403);
        }

        if (tag.IsSystemDefined && !isAdmin)
        {
            return ApiResponse.Fail("系统预设标签仅管理员可删除", 403);
        }

        if (await _unitOfWork.PostTags.IsUsedByPostsAsync(id))
        {
            return ApiResponse.Fail("该标签已被分享帖使用，无法删除（可先禁用）");
        }

        tag.DeletedBy = currentUserId;
        _unitOfWork.PostTags.Delete(tag);
        await _unitOfWork.SaveChangesAsync();

        return ApiResponse.Success(null, "标签删除成功");
    }

    public async Task<ApiResponse> ToggleActiveAsync(int id, int currentUserId, bool isActive)
    {
        if (!await IsAdmin(currentUserId))
        {
            return ApiResponse.Fail("无权限操作，仅管理员可启用/禁用标签", 403);
        }

        var tag = await _unitOfWork.PostTags.GetByIdAsync(id);
        if (tag == null)
        {
            return ApiResponse.Fail("帖子标签不存在", 404);
        }

        tag.IsActive = isActive;
        tag.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.PostTags.Update(tag);
        await _unitOfWork.SaveChangesAsync();

        var dto = _mapper.Map<PostTagResponse>(tag);
        return ApiResponse.Success(dto, isActive ? "标签已启用" : "标签已禁用");
    }

    public async Task<ApiResponse> UpdateSortOrderAsync(int currentUserId, Dictionary<int, int> sortOrders)
    {
        if (!await IsAdmin(currentUserId))
        {
            return ApiResponse.Fail("无权限操作，仅管理员可调整排序", 403);
        }

        if (sortOrders == null || sortOrders.Count == 0)
        {
            return ApiResponse.Fail("排序数据不能为空");
        }

        foreach (var kvp in sortOrders)
        {
            var tag = await _unitOfWork.PostTags.GetByIdAsync(kvp.Key);
            if (tag != null)
            {
                tag.SortOrder = kvp.Value;
                tag.UpdatedAt = DateTime.UtcNow;
                _unitOfWork.PostTags.Update(tag);
            }
        }

        await _unitOfWork.SaveChangesAsync();
        return ApiResponse.Success(null, "排序更新成功");
    }

    private async Task<bool> IsAdmin(int userId)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId);
        return user != null && user.Role == UserRole.Admin;
    }
}
