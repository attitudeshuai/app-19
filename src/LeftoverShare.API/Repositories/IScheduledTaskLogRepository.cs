using LeftoverShare.API.Entities;

namespace LeftoverShare.API.Repositories;

public interface IScheduledTaskLogRepository : IRepository<ScheduledTaskLog>
{
    Task<ScheduledTaskLog?> GetLastSuccessfulLogAsync(string taskName);
    Task<IEnumerable<ScheduledTaskLog>> GetRecentLogsAsync(string taskName, int take);
}
