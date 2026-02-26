using MT.LightTask.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MT.LightTask.Extensions;

internal static class SchedulerExtensions
{
    extension(ITaskScheduler scheduler)
    {
        public TaskConfig Config
        {
            get
            {
                var config = new TaskConfig()
                {
                     Enabled = scheduler.ScheduleStatus == TaskScheduleStatus.Running,
                     Name = scheduler.Name,
                     TypeName = scheduler.TaskTypeName
                };
                scheduler.Strategy.SetConfig(config);
                return config;
            }
        }
    }
}