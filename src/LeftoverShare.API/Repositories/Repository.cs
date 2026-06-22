using System.Linq.Expressions;
using LeftoverShare.API.Data;
using LeftoverShare.API.Entities;
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

    // 根据ID获取实体（忽略软删除过滤器，可获取已删除的实体）
    public virtual async Task<T?> GetByIdIgnoreFilterAsync(int id)
    {
        return await _dbSet.IgnoreQueryFilters().FirstOrDefaultAsync(e => EF.Property<int>(e, "Id") == id);
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

    // 获取回收站中的软删除记录（分页）
    public virtual async Task<(IEnumerable<T> Items, int TotalCount)> GetDeletedPagedAsync(int pageNumber, int pageSize)
    {
        if (!typeof(ISoftDeletable).IsAssignableFrom(typeof(T)))
        {
            return (new List<T>(), 0);
        }

        var query = _dbSet.IgnoreQueryFilters();
        var parameter = Expression.Parameter(typeof(T), "e");
        var isDeletedProperty = Expression.Property(parameter, "IsDeleted");
        var trueConstant = Expression.Constant(true);
        var equal = Expression.Equal(isDeletedProperty, trueConstant);
        var lambda = Expression.Lambda<Func<T, bool>>(equal, parameter);

        var filteredQuery = query.Where(lambda);
        var totalCount = await filteredQuery.CountAsync();
        var items = await filteredQuery
            .OrderByDescending(e => EF.Property<DateTime?>(e, "DeletedAt"))
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    // 获取回收站中的软删除记录（按条件，分页）
    public virtual async Task<(IEnumerable<T> Items, int TotalCount)> GetDeletedPagedAsync(
        Expression<Func<T, bool>> predicate, int pageNumber, int pageSize)
    {
        if (!typeof(ISoftDeletable).IsAssignableFrom(typeof(T)))
        {
            return (new List<T>(), 0);
        }

        var query = _dbSet.IgnoreQueryFilters();
        var parameter = Expression.Parameter(typeof(T), "e");
        var isDeletedProperty = Expression.Property(parameter, "IsDeleted");
        var trueConstant = Expression.Constant(true);
        var equal = Expression.Equal(isDeletedProperty, trueConstant);
        var isDeletedLambda = Expression.Lambda<Func<T, bool>>(equal, parameter);

        var combined = Expression.Lambda<Func<T, bool>>(
            Expression.AndAlso(isDeletedLambda.Body, new ExpressionParameterReplacer(predicate.Parameters.ToArray(), parameter).Visit(predicate.Body)),
            parameter);

        var filteredQuery = query.Where(combined);
        var totalCount = await filteredQuery.CountAsync();
        var items = await filteredQuery
            .OrderByDescending(e => EF.Property<DateTime?>(e, "DeletedAt"))
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
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

    // 批量删除实体（软删除）
    public virtual void DeleteRange(IEnumerable<T> entities)
    {
        foreach (var entity in entities)
        {
            Delete(entity);
        }
    }

    // 物理删除（绕过软删除拦截器，用于6个月自动清理）
    public virtual void HardDelete(T entity)
    {
        if (entity is ISoftDeletable)
        {
            _context.MarkForHardDelete(entity);
        }
        _dbSet.Remove(entity);
    }

    // 批量物理删除（用于自动清理任务）
    public virtual void HardDeleteRange(IEnumerable<T> entities)
    {
        var entityList = entities.ToList();
        foreach (var entity in entityList)
        {
            HardDelete(entity);
        }
    }

    // 获取可查询对象
    public virtual IQueryable<T> GetQueryable()
    {
        return _dbSet.AsQueryable();
    }

    // 获取可查询对象（忽略软删除过滤器，可查询已删除记录）
    public virtual IQueryable<T> GetQueryableIgnoreFilter()
    {
        return _dbSet.IgnoreQueryFilters().AsQueryable();
    }

    private class ExpressionParameterReplacer : ExpressionVisitor
    {
        private readonly ParameterExpression _targetParameter;
        private readonly ParameterExpression[] _sourceParameters;

        public ExpressionParameterReplacer(ParameterExpression[] sourceParameters, ParameterExpression targetParameter)
        {
            _sourceParameters = sourceParameters;
            _targetParameter = targetParameter;
        }

        protected override Expression VisitParameter(ParameterExpression node)
        {
            if (_sourceParameters.Contains(node))
            {
                return _targetParameter;
            }
            return base.VisitParameter(node);
        }
    }
}
