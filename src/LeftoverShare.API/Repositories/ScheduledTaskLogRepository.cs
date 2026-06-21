using LeftoverShare.API.Data;
using LeftoverShare.API.Entities;
using LeftoverShare.API.Entities.Enums;
using Microsoft.EntityFrameworkCore;

namespace LeftoverShare.API.Repositories;

public class ScheduledTaskLogRepository : Repository<ScheduledTaskLog>, IScheduledTaskLogRepository
{
    public ScheduledTaskLogRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<ScheduledTaskLog?> GetLastSuccessfulLogAsync(string taskName)
    {
        return await _dbSet
            .Where(stl => stl.TaskName == taskName && stl.Status == ScheduledTaskStatus.Success)
            .OrderByDescending(stl => stl.StartedAt)
            .FirstOrDefaultAsync();
    }

    public async Task<IEnumerable<ScheduledTaskLog>> GetRecentLogsAsync(string taskName, int take)
    {
        return await _dbSet
            .Where(stl => stl.TaskName == taskName)
            .OrderByDescending(stl => stl.StartedAt)
            .Take(take)
            .ToListAsync();
    }
}
