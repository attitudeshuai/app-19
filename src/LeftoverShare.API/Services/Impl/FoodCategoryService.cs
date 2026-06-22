using AutoMapper;
using LeftoverShare.API.DTOs.FoodCategories;
using LeftoverShare.API.Entities;
using LeftoverShare.API.Entities.Enums;
using LeftoverShare.API.Helpers;
using LeftoverShare.API.Repositories;

namespace LeftoverShare.API.Services.Impl;

/// <summary>
/// 食物分类服务实现
/// </summary>
public class FoodCategoryService : IFoodCategoryService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public FoodCategoryService(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<ApiResponse> GetTreeAsync(bool includeInactive = false)
    {
        var allCategories = includeInactive
            ? await _unitOfWork.FoodCategories.GetAllAsync()
            : await _unitOfWork.FoodCategories.FindAsync(fc => fc.IsActive);

        var categoryDtos = _mapper.Map<List<FoodCategoryResponse>>(allCategories.OrderBy(fc => fc.SortOrder).ThenBy(fc => fc.Name).ToList());

        var lookup = categoryDtos.ToDictionary(c => c.Id);
        var roots = new List<FoodCategoryResponse>();

        foreach (var cat in categoryDtos)
        {
            cat.Children = new List<FoodCategoryResponse>();
            if (cat.ParentId.HasValue && lookup.TryGetValue(cat.ParentId.Value, out var parent))
            {
                parent.Children ??= new List<FoodCategoryResponse>();
                parent.Children.Add(cat);
            }
            else if (!cat.ParentId.HasValue)
            {
                roots.Add(cat);
            }
        }

        return ApiResponse.Success(roots, "获取分类树成功");
    }

    public async Task<ApiResponse> GetRootCategoriesAsync(bool includeInactive = false)
    {
        var categories = await _unitOfWork.FoodCategories.GetRootCategoriesAsync(includeInactive);
        var dtos = _mapper.Map<List<FoodCategoryResponse>>(categories);
        return ApiResponse.Success(dtos, "获取顶级分类成功");
    }

    public async Task<ApiResponse> GetChildrenAsync(int parentId, bool includeInactive = false)
    {
        var parent = await _unitOfWork.FoodCategories.GetByIdAsync(parentId);
        if (parent == null)
        {
            return ApiResponse.Fail("父级分类不存在", 404);
        }

        var children = await _unitOfWork.FoodCategories.GetChildrenAsync(parentId, includeInactive);
        var dtos = _mapper.Map<List<FoodCategoryResponse>>(children);
        return ApiResponse.Success(dtos, "获取子分类成功");
    }

    public async Task<ApiResponse> GetByIdAsync(int id)
    {
        var category = await _unitOfWork.FoodCategories.GetByIdAsync(id);
        if (category == null)
        {
            return ApiResponse.Fail("分类不存在", 404);
        }

        var dto = _mapper.Map<FoodCategoryResponse>(category);
        return ApiResponse.Success(dto, "获取分类详情成功");
    }

    public async Task<ApiResponse> CreateAsync(int currentUserId, CreateFoodCategoryRequest request)
    {
        if (!await IsAdmin(currentUserId))
        {
            return ApiResponse.Fail("无权限操作，仅管理员可创建分类", 403);
        }

        var existing = await _unitOfWork.FoodCategories.GetByCodeAsync(request.Code);
        if (existing != null)
        {
            return ApiResponse.Fail($"分类编码 {request.Code} 已存在");
        }

        if (request.ParentId.HasValue)
        {
            var parent = await _unitOfWork.FoodCategories.GetByIdAsync(request.ParentId.Value);
            if (parent == null)
            {
                return ApiResponse.Fail("指定的父级分类不存在");
            }
        }

        var category = _mapper.Map<FoodCategory>(request);
        category.CreatedAt = DateTime.UtcNow;

        await _unitOfWork.FoodCategories.AddAsync(category);
        await _unitOfWork.SaveChangesAsync();

        var dto = _mapper.Map<FoodCategoryResponse>(category);
        return ApiResponse.Success(dto, "分类创建成功");
    }

    public async Task<ApiResponse> UpdateAsync(int id, int currentUserId, UpdateFoodCategoryRequest request)
    {
        if (!await IsAdmin(currentUserId))
        {
            return ApiResponse.Fail("无权限操作，仅管理员可更新分类", 403);
        }

        var category = await _unitOfWork.FoodCategories.GetByIdAsync(id);
        if (category == null)
        {
            return ApiResponse.Fail("分类不存在", 404);
        }

        var existing = await _unitOfWork.FoodCategories.GetByCodeAsync(request.Code);
        if (existing != null && existing.Id != id)
        {
            return ApiResponse.Fail($"分类编码 {request.Code} 已被其他分类使用");
        }

        if (request.ParentId.HasValue)
        {
            if (request.ParentId.Value == id)
            {
                return ApiResponse.Fail("不能将自己设为父级分类");
            }
            var parent = await _unitOfWork.FoodCategories.GetByIdAsync(request.ParentId.Value);
            if (parent == null)
            {
                return ApiResponse.Fail("指定的父级分类不存在");
            }
        }

        _mapper.Map(request, category);
        category.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.FoodCategories.Update(category);
        await _unitOfWork.SaveChangesAsync();

        var dto = _mapper.Map<FoodCategoryResponse>(category);
        return ApiResponse.Success(dto, "分类更新成功");
    }

    public async Task<ApiResponse> DeleteAsync(int id, int currentUserId)
    {
        if (!await IsAdmin(currentUserId))
        {
            return ApiResponse.Fail("无权限操作，仅管理员可删除分类", 403);
        }

        var category = await _unitOfWork.FoodCategories.GetByIdAsync(id);
        if (category == null)
        {
            return ApiResponse.Fail("分类不存在", 404);
        }

        if (await _unitOfWork.FoodCategories.HasChildrenAsync(id))
        {
            return ApiResponse.Fail("该分类下存在子分类，请先删除子分类");
        }

        if (await _unitOfWork.FoodCategories.IsUsedByPostsAsync(id))
        {
            return ApiResponse.Fail("该分类已被分享帖使用，无法删除（可先禁用）");
        }

        category.DeletedBy = currentUserId;
        _unitOfWork.FoodCategories.Delete(category);
        await _unitOfWork.SaveChangesAsync();

        return ApiResponse.Success(null, "分类删除成功");
    }

    public async Task<ApiResponse> ToggleActiveAsync(int id, int currentUserId, bool isActive)
    {
        if (!await IsAdmin(currentUserId))
        {
            return ApiResponse.Fail("无权限操作，仅管理员可启用/禁用分类", 403);
        }

        var category = await _unitOfWork.FoodCategories.GetByIdAsync(id);
        if (category == null)
        {
            return ApiResponse.Fail("分类不存在", 404);
        }

        category.IsActive = isActive;
        category.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.FoodCategories.Update(category);
        await _unitOfWork.SaveChangesAsync();

        var dto = _mapper.Map<FoodCategoryResponse>(category);
        return ApiResponse.Success(dto, isActive ? "分类已启用" : "分类已禁用");
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
            var category = await _unitOfWork.FoodCategories.GetByIdAsync(kvp.Key);
            if (category != null)
            {
                category.SortOrder = kvp.Value;
                category.UpdatedAt = DateTime.UtcNow;
                _unitOfWork.FoodCategories.Update(category);
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
