using System.Linq.Expressions;

namespace LeftoverShare.API.Repositories;

// 泛型仓储接口，定义基本的 CRUD 操作
public interface IRepository<T> where T : class
{
    // 根据ID获取实体
    Task<T?> GetByIdAsync(int id);

    // 获取所有实体
    Task<IEnumerable<T>> GetAllAsync();

    // 分页获取实体，返回数据列表和总记录数
    Task<(IEnumerable<T> Items, int TotalCount)> GetPagedAsync(int pageNumber, int pageSize);

    // 根据条件查找实体
    Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate);

    // 添加实体
    Task<T> AddAsync(T entity);

    // 更新实体
    void Update(T entity);

    // 删除实体
    void Delete(T entity);

    // 获取可查询对象
    IQueryable<T> GetQueryable();
}
