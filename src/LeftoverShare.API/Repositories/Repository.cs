using System.Linq.Expressions;
using LeftoverShare.API.Data;
using Microsoft.EntityFrameworkCore;

namespace LeftoverShare.API.Repositories;

// 泛型仓储实现，使用 AppDbContext
public class Repository<T> : IRepository<T> where T : class
{
    protected readonly AppDbContext _context;
    protected readonly DbSet<T> _dbSet;

    public Repository(AppDbContext context)
    {
        _context = context;
        _dbSet = _context.Set<T>();
    }

    // 根据ID获取实体
    public virtual async Task<T?> GetByIdAsync(int id)
    {
        return await _dbSet.FindAsync(id);
    }

    // 获取所有实体
    public virtual async Task<IEnumerable<T>> GetAllAsync()
    {
        return await _dbSet.ToListAsync();
    }

    // 分页获取实体，返回数据列表和总记录数
    public virtual async Task<(IEnumerable<T> Items, int TotalCount)> GetPagedAsync(int pageNumber, int pageSize)
    {
        var totalCount = await _dbSet.CountAsync();
        var items = await _dbSet
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
        return (items, totalCount);
    }

    // 根据条件查找实体
    public virtual async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate)
    {
        return await _dbSet.Where(predicate).ToListAsync();
    }

    // 添加实体
    public virtual async Task<T> AddAsync(T entity)
    {
        await _dbSet.AddAsync(entity);
        return entity;
    }

    // 更新实体
    public virtual void Update(T entity)
    {
        _dbSet.Update(entity);
    }

    // 删除实体
    public virtual void Delete(T entity)
    {
        _dbSet.Remove(entity);
    }

    // 获取可查询对象
    public virtual IQueryable<T> GetQueryable()
    {
        return _dbSet.AsQueryable();
    }
}
