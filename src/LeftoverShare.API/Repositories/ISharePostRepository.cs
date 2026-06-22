using LeftoverShare.API.Entities;

namespace LeftoverShare.API.Repositories;

public interface ISharePostRepository : IRepository<SharePost>
{
    Task<(IEnumerable<SharePost> Items, int TotalCount)> GetPagedWithDetailsAsync(int pageNumber, int pageSize);
    Task<(IEnumerable<SharePost> Items, int TotalCount)> GetDeletedPagedWithDetailsAsync(int userId, int pageNumber, int pageSize);
    new Task<SharePost?> GetByIdIgnoreFilterAsync(int id);
    Task<IEnumerable<SharePost>> GetByUserIdAsync(int userId);
    Task<IEnumerable<SharePost>> GetExpiredPostsAsync(DateTime now);

    /// <summary>
    /// 带行级锁获取分享帖（用于高并发库存扣减）
    /// 业务意图：使用 SELECT ... FOR UPDATE 锁定行，防止并发更新
    /// </summary>
    Task<SharePost?> GetByIdWithLockAsync(int id);

    /// <summary>
    /// 原子性扣减库存
    /// 业务意图：使用原生 SQL 执行原子性的库存扣减操作，
    /// 确保在高并发场景下不会出现超卖
    /// </summary>
    /// <param name="postId">分享帖ID</param>
    /// <param name="quantity">扣减数量</param>
    /// <returns>是否扣减成功</returns>
    Task<bool> TryDecrementReservedQuantityAsync(int postId, int quantity);

    /// <summary>
    /// 原子性恢复库存
    /// 业务意图：使用原生 SQL 执行原子性的库存恢复操作，
    /// 用于取消预约或预约超时时恢复可用数量
    /// </summary>
    /// <param name="postId">分享帖ID</param>
    /// <param name="quantity">恢复数量</param>
    /// <returns>是否恢复成功</returns>
    Task<bool> TryIncrementReservedQuantityAsync(int postId, int quantity);

    /// <summary>
    /// 检查用户是否已预约该分享帖
    /// 业务意图：防止同一用户重复预约同一分享帖
    /// </summary>
    Task<bool> HasExistingReservationAsync(int postId, int userId);

    /// <summary>
    /// 获取分享帖的可用库存
    /// 业务意图：计算剩余可用数量（总数量 - 已预约数量）
    /// </summary>
    Task<int> GetAvailableQuantityAsync(int postId);
}
