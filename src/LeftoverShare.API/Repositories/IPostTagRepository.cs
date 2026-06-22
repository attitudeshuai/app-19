using LeftoverShare.API.Entities;

namespace LeftoverShare.API.Repositories;

/// <summary>
/// 帖子标签仓储接口
/// </summary>
public interface IPostTagRepository : IRepository<PostTag>
{
    /// <summary>
    /// 根据编码获取标签
    /// </summary>
    Task<PostTag?> GetByCodeAsync(string code);

    /// <summary>
    /// 获取所有启用的标签
    /// </summary>
    Task<List<PostTag>> GetAllActiveAsync(bool includeUserDefined = true);

    /// <summary>
    /// 获取热门标签（按使用次数排序）
    /// </summary>
    Task<List<PostTag>> GetPopularAsync(int topN = 20);

    /// <summary>
    /// 根据用户ID获取用户创建的标签
    /// </summary>
    Task<List<PostTag>> GetByUserIdAsync(int userId);

    /// <summary>
    /// 根据多个ID批量获取
    /// </summary>
    Task<List<PostTag>> GetByIdsAsync(IEnumerable<int> ids);

    /// <summary>
    /// 根据名称搜索标签（模糊匹配）
    /// </summary>
    Task<List<PostTag>> SearchByNameAsync(string keyword, int limit = 20);

    /// <summary>
    /// 检查标签是否被帖子使用
    /// </summary>
    Task<bool> IsUsedByPostsAsync(int tagId);
}
