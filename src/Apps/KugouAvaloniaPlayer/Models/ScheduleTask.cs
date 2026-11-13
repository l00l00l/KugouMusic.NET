using System.Collections.Generic;

namespace KugouAvaloniaPlayer.Models;

public sealed class ScheduleTask
{
    /// <summary>
    /// 任务名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 是否启用
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// 星期
    /// 1=周一
    /// 2=周二
    /// 3=周三
    /// 4=周四
    /// 5=周五
    /// 6=周六
    /// 7=周日
    /// </summary>
    public List<int> WeekDays { get; set; } = [];

    /// <summary>
    /// 执行时间
    /// HH:mm
    /// 例如 08:00
    /// </summary>
    public string Time { get; set; } = string.Empty;

    /// <summary>
    /// 执行动作
    /// </summary>
    public ScheduleActionType Action { get; set; }

    /// <summary>
    /// 歌单ID
    /// Play时使用
    /// </summary>
    public string? PlaylistName { get; set; }

    /// <summary>
    /// 指定日期
    /// 格式：yyyy-MM-dd
    /// 为空表示按WeekDays执行
    /// </summary>
    public string? SpecificDate { get; set; }

    /// <summary>
    /// 是否忽略节假日
    /// </summary>
    public bool IgnoreHoliday { get; set; }
    /// <summary>
    /// 音量
    /// 0-100
    /// null表示不调整音量
    /// </summary>
    public int? Volume { get; set; }
}