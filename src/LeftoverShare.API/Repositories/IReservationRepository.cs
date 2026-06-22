using LeftoverShare.API.Entities;

namespace LeftoverShare.API.Repositories;

// 预订仓储接口，继承泛型仓储
public interface IReservationRepository : IRepository<Reservation>
{
    // 分页获取预订详情（含关联数据）
    Task<(IEnumerable<Reservation> Items, int TotalCount)> GetPagedWithDetailsAsync(int pageNumber, int pageSize);

    // 获取已删除的预订记录（按用户ID，分页）
    Task<(IEnumerable<Reservation> Items, int TotalCount)> GetDeletedPagedAsync(int userId, int pageNumber, int pageSize);

    // 根据ID获取预订（忽略软删除过滤器）
    new Task<Reservation?> GetByIdIgnoreFilterAsync(int id);

    // 根据帖子ID获取预订列表
    Task<IEnumerable<Reservation>> GetByPostIdAsync(int postId);

    // 根据领取者ID获取预订列表
    Task<IEnumerable<Reservation>> GetByClaimerIdAsync(int claimerId);

    // 根据用户ID获取预订列表
    Task<IEnumerable<Reservation>> GetByUserIdAsync(int userId);
}
