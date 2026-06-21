using LeftoverShare.API.Entities;

namespace LeftoverShare.API.Repositories;

// 预订仓储接口，继承泛型仓储
public interface IReservationRepository : IRepository<Reservation>
{
    // 分页获取预订详情（含关联数据）
    Task<(IEnumerable<Reservation> Items, int TotalCount)> GetPagedWithDetailsAsync(int pageNumber, int pageSize);

    // 根据帖子ID获取预订列表
    Task<IEnumerable<Reservation>> GetByPostIdAsync(int postId);

    // 根据领取者ID获取预订列表
    Task<IEnumerable<Reservation>> GetByClaimerIdAsync(int claimerId);

    // 根据用户ID获取预订列表
    Task<IEnumerable<Reservation>> GetByUserIdAsync(int userId);
}
