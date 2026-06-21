namespace LeftoverShare.API.Entities.Enums;

/// <summary>
/// 定时任务执行状态枚举
/// </summary>
public enum ScheduledTaskStatus
{
    /// <summary>
    /// 执行中
    /// </summary>
    Running = 0,

    /// <summary>
    /// 成功
    /// </summary>
    Success = 1,

    /// <summary>
    /// 部分成功
    /// </summary>
    PartialSuccess = 2,

    /// <summary>
    /// 失败
    /// </summary>
    Failed = 3
}
