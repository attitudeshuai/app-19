using System.Text.Json;
using LeftoverShare.API.Entities;
using LeftoverShare.API.Entities.Enums;
using LeftoverShare.API.Helpers;
using LeftoverShare.API.Repositories;
using LeftoverShare.API.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LeftoverShare.API.BackgroundServices;

/// <summary>
/// 每月物理清理定时任务调度器
/// 负责在每月固定时间点自动清理超过保留期的软删除记录（物理删除）
/// 从数据库任务日志读取上次成功执行时间，支持应用重启后的补跑
/// </summary>
public class HardCleanupScheduler : BackgroundService
{
    private const string TaskName = "MonthlyHardCleanup";

    private readonly IServiceProvider _serviceProvider;
    private readonly DailyCleanupSettings _settings;
    private readonly ILogger<HardCleanupScheduler> _logger;

    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public HardCleanupScheduler(
        IServiceProvider serviceProvider,
        IOptions<DailyCleanupSettings> settings,
        ILogger<HardCleanupScheduler> logger)
    {
        _serviceProvider = serviceProvider;
        _settings = settings.Value;
        _logger = logger;
    }

    /// <summary>
    /// BackgroundService 执行入口
    /// </summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_settings.HardCleanupEnabled)
        {
            _logger.LogInformation("每月物理清理定时任务已被禁用，调度器未启动");
            return;
        }

        _logger.LogInformation(
            "每月物理清理定时任务调度器已启动，执行时间: {ExecuteTime}，软删除保留期: {RetentionMonths}个月",
            _settings.HardCleanupExecuteTime, _settings.SoftDeleteRetentionMonths);

        if (_settings.StartupDelaySeconds > 0)
        {
            _logger.LogInformation("等待 {Delay}s 后进行首次运行检查...", _settings.StartupDelaySeconds);
            await Task.Delay(TimeSpan.FromSeconds(_settings.StartupDelaySeconds), stoppingToken);
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var now = DateTime.UtcNow;

                if (await ShouldRunNowAsync(now))
                {
                    _logger.LogInformation("触发每月物理清理任务（定时到达或补跑），开始执行...");
                    await RunCleanupTaskAsync(stoppingToken);
                    _logger.LogInformation("物理清理任务执行流程结束，下次执行时间: {NextRun}",
                        CalculateNextRunTime(now));
                }

                var delay = CalculateDelayToNextCheck(now);
                _logger.LogDebug("距离下次检查还有 {Delay}", delay);
                await Task.Delay(delay, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("每月物理清理定时任务调度器正在停止...");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "每月物理清理调度器发生未预期的异常，将在 1 小时后重试");
                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
            }
        }

        _logger.LogInformation("每月物理清理定时任务调度器已停止");
    }

    /// <summary>
    /// 判断当前是否应该执行任务
    /// 通过查询数据库任务日志表判断本月是否已经成功执行过
    /// 支持补跑：只要本月已过计划执行时间且本月尚未成功执行，就触发执行
    /// </summary>
    private async Task<bool> ShouldRunNowAsync(DateTime utcNow)
    {
        using var scope = _serviceProvider.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var scheduledLocalTime = ParseScheduledTime();
        var localNow = utcNow.ToLocalTime();

        var scheduledLocalThisMonth = new DateTime(
            localNow.Year, localNow.Month, 1,
            scheduledLocalTime.Hours, scheduledLocalTime.Minutes, 0, DateTimeKind.Local);
        var scheduledUtcThisMonth = scheduledLocalThisMonth.ToUniversalTime();

        var thisMonthUtcStart = new DateTime(localNow.Year, localNow.Month, 1, 0, 0, 0, DateTimeKind.Local).ToUniversalTime();
        var nextMonthUtcStart = thisMonthUtcStart.AddMonths(1);

        var lastRunLog = await unitOfWork.ScheduledTaskLogs
            .GetQueryable()
            .Where(log => log.TaskName == TaskName
                      && (log.Status == ScheduledTaskStatus.Success
                          || log.Status == ScheduledTaskStatus.PartialSuccess))
            .OrderByDescending(log => log.StartedAt)
            .FirstOrDefaultAsync();

        var lastRunDate = lastRunLog?.StartedAt.Date;
        var hasRunThisMonth = lastRunDate.HasValue
            && lastRunDate.Value >= thisMonthUtcStart.Date
            && lastRunDate.Value < nextMonthUtcStart.Date;

        if (hasRunThisMonth)
        {
            _logger.LogDebug(
                "本月已执行过物理清理任务（最近一次: {LastRun}, 状态: {Status}），跳过以避免重复执行",
                lastRunLog!.StartedAt, lastRunLog.Status);
            return false;
        }

        var isPastScheduledTime = utcNow >= scheduledUtcThisMonth;

        if (isPastScheduledTime)
        {
            if (lastRunLog != null)
            {
                _logger.LogInformation(
                    "检测到本月尚未执行（最近一次: {LastRun}, 状态: {Status}），当前时间已过计划执行时间 {ScheduledTime}，触发执行",
                    lastRunLog.StartedAt, lastRunLog.Status, scheduledLocalThisMonth);
            }
            else
            {
                _logger.LogInformation(
                    "未找到任何成功/部分成功的执行记录，当前时间已过计划执行时间 {ScheduledTime}，触发首次执行",
                    scheduledLocalThisMonth);
            }
        }

        return isPastScheduledTime;
    }

    /// <summary>
    /// 解析配置的计划执行时间（本地时间）
    /// </summary>
    private TimeSpan ParseScheduledTime()
    {
        if (!TimeSpan.TryParseExact(_settings.HardCleanupExecuteTime, @"hh\:mm", null, out var scheduledTime))
        {
            _logger.LogWarning("物理清理执行时间格式错误: {ExecuteTime}，使用默认 03:00", _settings.HardCleanupExecuteTime);
            scheduledTime = new TimeSpan(3, 0, 0);
        }
        return scheduledTime;
    }

    /// <summary>
    /// 计算距离下次检查的延迟时间
    /// </summary>
    private TimeSpan CalculateDelayToNextCheck(DateTime utcNow)
    {
        var nextRun = CalculateNextRunTime(utcNow);
        var timeUntilNextRun = nextRun - utcNow;

        if (timeUntilNextRun <= TimeSpan.Zero)
        {
            return TimeSpan.FromMinutes(30);
        }

        if (timeUntilNextRun > TimeSpan.FromDays(7))
        {
            return TimeSpan.FromDays(1);
        }

        if (timeUntilNextRun > TimeSpan.FromDays(1))
        {
            return TimeSpan.FromHours(6);
        }

        if (timeUntilNextRun > TimeSpan.FromHours(2))
        {
            return TimeSpan.FromHours(1);
        }

        return TimeSpan.FromMinutes(15);
    }

    /// <summary>
    /// 计算下次计划运行时间（UTC）
    /// 每月1号执行
    /// </summary>
    private DateTime CalculateNextRunTime(DateTime utcNow)
    {
        var scheduledLocalTime = ParseScheduledTime();
        var localNow = utcNow.ToLocalTime();

        var scheduledLocalThisMonth = new DateTime(
            localNow.Year, localNow.Month, 1,
            scheduledLocalTime.Hours, scheduledLocalTime.Minutes, 0, DateTimeKind.Local);

        return localNow < scheduledLocalThisMonth
            ? scheduledLocalThisMonth.ToUniversalTime()
            : scheduledLocalThisMonth.AddMonths(1).ToUniversalTime();
    }

    /// <summary>
    /// 执行物理清理任务（带日志审计）
    /// </summary>
    private async Task RunCleanupTaskAsync(CancellationToken cancellationToken)
    {
        if (!await _semaphore.WaitAsync(0, cancellationToken))
        {
            _logger.LogWarning("检测到已有物理清理任务正在运行，跳过本次执行");
            return;
        }

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var cleanupService = scope.ServiceProvider.GetRequiredService<IHardCleanupService>();

            var startedAt = DateTime.UtcNow;
            var taskLog = new ScheduledTaskLog
            {
                TaskName = TaskName,
                Status = ScheduledTaskStatus.Running,
                StartedAt = startedAt
            };
            await unitOfWork.ScheduledTaskLogs.AddAsync(taskLog);
            await unitOfWork.SaveChangesAsync();

            try
            {
                var beforeDate = startedAt.AddMonths(-_settings.SoftDeleteRetentionMonths);
                var result = await cleanupService.CleanupExpiredSoftDeletesAsync(beforeDate);

                taskLog.ExpiredSharePostsCount = result.SharePostsCleanedCount;
                taskLog.ExpiredPickupCodesCount = result.PickupCodesCleanedCount;
                taskLog.NotificationsSentCount = result.ReservationsCleanedCount + result.KarmaPointsCleanedCount;
                taskLog.CompletedAt = DateTime.UtcNow;
                taskLog.DurationMs = (long)(taskLog.CompletedAt.Value - taskLog.StartedAt).TotalMilliseconds;
                taskLog.Status = result.HasErrors
                    ? (result.TotalCleanedCount > 0
                        ? ScheduledTaskStatus.PartialSuccess
                        : ScheduledTaskStatus.Failed)
                    : ScheduledTaskStatus.Success;

                if (result.Errors.Any())
                {
                    taskLog.ErrorMessage = string.Join(" | ", result.Errors.Take(10));
                }

                if (result.Details.Any())
                {
                    try
                    {
                        taskLog.Details = JsonSerializer.Serialize(result.Details,
                            new JsonSerializerOptions { WriteIndented = false });
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "序列化任务详情失败");
                    }
                }

                unitOfWork.ScheduledTaskLogs.Update(taskLog);
                await unitOfWork.SaveChangesAsync();

                _logger.LogInformation(
                    "物理清理任务执行完成: 状态={Status}, SharePost={Posts}, Reservation={Reservations}, PickupCode={Codes}, KarmaPoint={Points}, Snapshot={Snapshots}, 耗时={Duration}ms",
                    taskLog.Status, result.SharePostsCleanedCount,
                    result.ReservationsCleanedCount, result.PickupCodesCleanedCount,
                    result.KarmaPointsCleanedCount, result.SnapshotsCleanedCount,
                    taskLog.DurationMs);
            }
            catch (Exception ex)
            {
                taskLog.Status = ScheduledTaskStatus.Failed;
                taskLog.CompletedAt = DateTime.UtcNow;
                taskLog.DurationMs = (long)(taskLog.CompletedAt.Value - taskLog.StartedAt).TotalMilliseconds;
                taskLog.ErrorMessage = ex.Message;
                unitOfWork.ScheduledTaskLogs.Update(taskLog);
                await unitOfWork.SaveChangesAsync();
                _logger.LogError(ex, "物理清理任务执行失败");
                throw;
            }
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public override void Dispose()
    {
        _semaphore.Dispose();
        base.Dispose();
    }
}
