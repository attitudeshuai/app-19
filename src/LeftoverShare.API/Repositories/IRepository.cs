using System.Linq.Expressions;

namespace LeftoverShare.API.Repositories;

// 泛型仓储接口，定义基本的 CRUD 操作
public interface IRepository<T> where T : class
{
    // 根据ID获取实体
    Task<T?> GetByIdAsync(int id);

    // 根据ID获取实体（忽略软删除过滤器，可获取已删除的实体）
    Task<T?> GetByIdIgnoreFilterAsync(int id);

    // 获取所有实体
    Task<IEnumerable<T>> GetAllAsync();

    // 分页获取实体，返回数据列表和总记录数
    Task<(IEnumerable<T> Items, int TotalCount)> GetPagedAsync(int pageNumber, int pageSize);

    // 根据条件查找实体
    Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate);

    // 获取回收站中的软删除记录（分页）
    Task<(IEnumerable<T> Items, int TotalCount)> GetDeletedPagedAsync(int pageNumber, int pageSize);

    // 获取回收站中的软删除记录（按条件，分页）
    Task<(IEnumerable<T> Items, int TotalCount)> GetDeletedPagedAsync(
        Expression<Func<T, bool>> predicate, int pageNumber, int pageSize);

    // 添加实体
    Task<T> AddAsync(T entity);

    // 更新实体
    void Update(T entity);

    // 删除实体（软删除，通过 IsDeleted 标记）
    void Delete(T entity);

    // 批量删除实体（软删除）
    void DeleteRange(IEnumerable<T> entities);

    // 删除实体（物理删除，用于6个月自动清理，或绕过软删除时使用）
    void HardDelete(T entity);

    // 批量物理删除（用于自动清理任务）
    void HardDeleteRange(IEnumerable<T> entities);

    // 获取可查询对象
    IQueryable<T> GetQueryable();

    // 获取可查询对象（忽略软删除过滤器，可查询已删除记录）
    IQueryable<T> GetQueryableIgnoreFilter();
}
