using AutoMapper;
using LeftoverShare.API.DTOs.AllergenTags;
using LeftoverShare.API.DTOs.Common;
using LeftoverShare.API.Entities;
using LeftoverShare.API.Entities.Enums;
using LeftoverShare.API.Helpers;
using LeftoverShare.API.Repositories;

namespace LeftoverShare.API.Services.Impl;

/// <summary>
/// 过敏原标签服务实现
/// </summary>
public class AllergenTagService : IAllergenTagService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public AllergenTagService(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<ApiResponse> GetAllActiveAsync()
    {
        var tags = await _unitOfWork.AllergenTags.GetAllActiveAsync();
        var dtos = _mapper.Map<List<AllergenTagResponse>>(tags);
        return ApiResponse.Success(dtos, "获取过敏原标签成功");
    }

    public async Task<ApiResponse> GetPagedAsync(int pageNumber, int pageSize, bool includeInactive = false, int? severityLevel = null, string? searchTerm = null)
    {
        if (pageNumber < 1) pageNumber = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 10;

        var query = await _unitOfWork.AllergenTags.GetAllAsync();
        IEnumerable<AllergenTag> filtered = query;

        if (!includeInactive)
        {
            filtered = filtered.Where(t => t.IsActive);
        }

        if (severityLevel.HasValue)
        {
            filtered = filtered.Where(t => t.SeverityLevel == severityLevel.Value);
        }

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim().ToLower();
            filtered = filtered.Where(t =>
                t.Name.ToLower().Contains(term) ||
                t.Code.ToLower().Contains(term) ||
                (t.Description != null && t.Description.ToLower().Contains(term)));
        }

        var ordered = filtered.OrderBy(t => t.SortOrder).ThenByDescending(t => t.SeverityLevel).ThenBy(t => t.Name).ToList();

        var totalCount = ordered.Count;
        var items = ordered.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();

        var dtos = _mapper.Map<List<AllergenTagResponse>>(items);

        var pagedResponse = new PagedResponse<AllergenTagResponse>
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
        var tag = await _unitOfWork.AllergenTags.GetByIdAsync(id);
        if (tag == null)
        {
            return ApiResponse.Fail("过敏原标签不存在", 404);
        }

        var dto = _mapper.Map<AllergenTagResponse>(tag);
        return ApiResponse.Success(dto, "获取标签详情成功");
    }

    public async Task<ApiResponse> CreateAsync(int currentUserId, CreateAllergenTagRequest request)
    {
        if (!await IsAdmin(currentUserId))
        {
            return ApiResponse.Fail("无权限操作，仅管理员可创建过敏原标签", 403);
        }

        var existing = await _unitOfWork.AllergenTags.GetByCodeAsync(request.Code);
        if (existing != null)
        {
            return ApiResponse.Fail($"标签编码 {request.Code} 已存在");
        }

        var tag = _mapper.Map<AllergenTag>(request);
        tag.CreatedAt = DateTime.UtcNow;

        await _unitOfWork.AllergenTags.AddAsync(tag);
        await _unitOfWork.SaveChangesAsync();

        var dto = _mapper.Map<AllergenTagResponse>(tag);
        return ApiResponse.Success(dto, "过敏原标签创建成功");
    }

    public async Task<ApiResponse> UpdateAsync(int id, int currentUserId, UpdateAllergenTagRequest request)
    {
        if (!await IsAdmin(currentUserId))
        {
            return ApiResponse.Fail("无权限操作，仅管理员可更新过敏原标签", 403);
        }

        var tag = await _unitOfWork.AllergenTags.GetByIdAsync(id);
        if (tag == null)
        {
            return ApiResponse.Fail("过敏原标签不存在", 404);
        }

        var existing = await _unitOfWork.AllergenTags.GetByCodeAsync(request.Code);
        if (existing != null && existing.Id != id)
        {
            return ApiResponse.Fail($"标签编码 {request.Code} 已被其他标签使用");
        }

        _mapper.Map(request, tag);
        tag.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.AllergenTags.Update(tag);
        await _unitOfWork.SaveChangesAsync();

        var dto = _mapper.Map<AllergenTagResponse>(tag);
        return ApiResponse.Success(dto, "过敏原标签更新成功");
    }

    public async Task<ApiResponse> DeleteAsync(int id, int currentUserId)
    {
        if (!await IsAdmin(currentUserId))
        {
            return ApiResponse.Fail("无权限操作，仅管理员可删除过敏原标签", 403);
        }

        var tag = await _unitOfWork.AllergenTags.GetByIdAsync(id);
        if (tag == null)
        {
            return ApiResponse.Fail("过敏原标签不存在", 404);
        }

        if (await _unitOfWork.AllergenTags.IsUsedByPostsAsync(id))
        {
            return ApiResponse.Fail("该标签已被分享帖使用，无法删除（可先禁用）");
        }

        tag.DeletedBy = currentUserId;
        _unitOfWork.AllergenTags.Delete(tag);
        await _unitOfWork.SaveChangesAsync();

        return ApiResponse.Success(null, "过敏原标签删除成功");
    }

    public async Task<ApiResponse> ToggleActiveAsync(int id, int currentUserId, bool isActive)
    {
        if (!await IsAdmin(currentUserId))
        {
            return ApiResponse.Fail("无权限操作，仅管理员可启用/禁用标签", 403);
        }

        var tag = await _unitOfWork.AllergenTags.GetByIdAsync(id);
        if (tag == null)
        {
            return ApiResponse.Fail("过敏原标签不存在", 404);
        }

        tag.IsActive = isActive;
        tag.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.AllergenTags.Update(tag);
        await _unitOfWork.SaveChangesAsync();

        var dto = _mapper.Map<AllergenTagResponse>(tag);
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
            var tag = await _unitOfWork.AllergenTags.GetByIdAsync(kvp.Key);
            if (tag != null)
            {
                tag.SortOrder = kvp.Value;
                tag.UpdatedAt = DateTime.UtcNow;
                _unitOfWork.AllergenTags.Update(tag);
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
