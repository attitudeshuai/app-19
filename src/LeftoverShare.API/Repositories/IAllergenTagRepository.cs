using LeftoverShare.API.Entities;

namespace LeftoverShare.API.Repositories;

/// <summary>
/// 过敏原标签仓储接口
/// </summary>
public interface IAllergenTagRepository : IRepository<AllergenTag>
{
    /// <summary>
    /// 根据编码获取标签
    /// </summary>
    Task<AllergenTag?> GetByCodeAsync(string code);

    /// <summary>
    /// 获取所有启用的标签
    /// </summary>
    Task<List<AllergenTag>> GetAllActiveAsync();

    /// <summary>
    /// 根据严重程度获取标签
    /// </summary>
    Task<List<AllergenTag>> GetBySeverityAsync(int severityLevel, bool includeInactive = false);

    /// <summary>
    /// 根据多个ID批量获取
    /// </summary>
    Task<List<AllergenTag>> GetByIdsAsync(IEnumerable<int> ids);

    /// <summary>
    /// 检查标签是否被帖子使用
    /// </summary>
    Task<bool> IsUsedByPostsAsync(int tagId);
}
