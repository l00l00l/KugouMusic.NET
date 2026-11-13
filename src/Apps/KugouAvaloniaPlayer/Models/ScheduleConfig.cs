using System.Collections.Generic;

namespace KugouAvaloniaPlayer.Models;

public sealed class ScheduleConfig
{
    /// <summary>
    /// 调度总开关
    /// </summary>
    public bool Enabled { get; set; } = true;


    /// <summary>
    /// 节假日跳过
    /// </summary>
    public bool SkipHoliday { get; set; } = true;

    /// <summary>
    /// 节假日列表
    /// yyyy-MM-dd
    /// </summary>
    public List<string> Holidays { get; set; } = [];

    /// <summary>
    /// 调休工作日
    /// yyyy-MM-dd
    /// </summary>
    public List<string> Workdays { get; set; } = [];
    public List<ScheduleTask> Tasks { get; set; } = [];
}