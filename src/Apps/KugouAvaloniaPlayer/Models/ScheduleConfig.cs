using System.Collections.Generic;

namespace KugouAvaloniaPlayer.Models;

public sealed class ScheduleConfig
{
    public List<ScheduleTask> Tasks { get; set; } = [];
}