namespace LeftoverShare.API.Helpers;

/// <summary>
/// 每日清理定时任务配置
/// </summary>
public class DailyCleanupSettings
{
    /// <summary>
    /// 配置节名称
    /// </summary>
    public const string SectionName = "DailyCleanupSettings";

    /// <summary>
    /// 是否启用定时任务
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// 每日执行时间（24小时制，HH:mm格式，默认02:00）
    /// </summary>
    public string ExecuteTime { get; set; } = "02:00";

    /// <summary>
    /// 取餐码未使用的超时时间（小时，默认24小时）
    /// </summary>
    public int PickupCodeUnusedTimeoutHours { get; set; } = 24;

    /// <summary>
    /// 任务启动后延迟执行的时间（秒，默认60秒，给系统启动留出时间）
    /// </summary>
    public int StartupDelaySeconds { get; set; } = 60;
}
