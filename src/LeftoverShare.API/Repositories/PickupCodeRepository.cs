using LeftoverShare.API.Data;
using LeftoverShare.API.Entities;
using Microsoft.EntityFrameworkCore;

namespace LeftoverShare.API.Repositories;

// 取餐码仓储实现
public class PickupCodeRepository : Repository<PickupCode>, IPickupCodeRepository
{
    public PickupCodeRepository(AppDbContext context) : base(context)
    {
    }

    // 根据预订ID获取取餐码（含预订、帖子、发布者信息）
    public async Task<PickupCode?> GetByReservationIdAsync(int reservationId)
    {
        return await _dbSet
            .Include(pc => pc.Reservation).ThenInclude(r => r.Post).ThenInclude(p => p.Poster)
            .FirstOrDefaultAsync(pc => pc.ReservationId == reservationId);
    }

    // 根据取餐码获取记录（含预订、帖子、发布者信息）
    public async Task<PickupCode?> GetByCodeAsync(string code)
    {
        return await _dbSet
            .Include(pc => pc.Reservation).ThenInclude(r => r.Post).ThenInclude(p => p.Poster)
            .FirstOrDefaultAsync(pc => pc.Code == code);
    }

    // 根据ID获取取餐码（含预订、帖子、发布者信息）
    public override async Task<PickupCode?> GetByIdAsync(int id)
    {
        return await _dbSet
            .Include(pc => pc.Reservation).ThenInclude(r => r.Post).ThenInclude(p => p.Poster)
            .FirstOrDefaultAsync(pc => pc.Id == id);
    }

    // 获取所有取餐码（含预订、帖子、发布者信息）
    public override async Task<IEnumerable<PickupCode>> GetAllAsync()
    {
        return await _dbSet
            .Include(pc => pc.Reservation).ThenInclude(r => r.Post).ThenInclude(p => p.Poster)
            .OrderByDescending(pc => pc.ExpiresAt)
            .ToListAsync();
    }
}
