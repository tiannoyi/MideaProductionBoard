using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace MideaProductionBoard
{
    public class WorkTimeInfo
    {
        public int StartHour { get; set; } = 8;
        public int StartMinute { get; set; } = 0;
        public int EndHour { get; set; } = 18;
        public int EndMinute { get; set; } = 0;

        // 支持多个休息时间段
        public ObservableCollection<RestTime> RestTimes { get; set; } = new ObservableCollection<RestTime>();

        public WorkTimeInfo()
        {
            // 默认添加一个午休时间
            RestTimes.Add(new RestTime { StartHour = 12, StartMinute = 0, EndHour = 13, EndMinute = 0 });
        }

        // 计算净工作时间（小时）
        public double CalculateNetWorkHours()
        {
            DateTime startTime = DateTime.Today.AddHours(StartHour).AddMinutes(StartMinute);
            DateTime endTime = DateTime.Today.AddHours(EndHour).AddMinutes(EndMinute);

            TimeSpan totalDuration = endTime - startTime;
            TimeSpan totalRestDuration = TimeSpan.Zero;

            // 减去所有休息时间
            foreach (var rest in RestTimes)
            {
                DateTime restStart = DateTime.Today.AddHours(rest.StartHour).AddMinutes(rest.StartMinute);
                DateTime restEnd = DateTime.Today.AddHours(rest.EndHour).AddMinutes(rest.EndMinute);
                totalRestDuration += (restEnd - restStart);
            }

            return (totalDuration - totalRestDuration).TotalHours;
        }

        // 检查工作时间是否有效
        public bool IsValid()
        {
            if (StartHour < 0 || StartHour > 23) return false;
            if (StartMinute < 0 || StartMinute > 59) return false;
            if (EndHour < 0 || EndHour > 23) return false;
            if (EndMinute < 0 || EndMinute > 59) return false;

            // 检查每个休息时间
            foreach (var rest in RestTimes)
            {
                if (rest.StartHour < 0 || rest.StartHour > 23) return false;
                if (rest.StartMinute < 0 || rest.StartMinute > 59) return false;
                if (rest.EndHour < 0 || rest.EndHour > 23) return false;
                if (rest.EndMinute < 0 || rest.EndMinute > 59) return false;
            }

            return CalculateNetWorkHours() > 0;
        }

        // 检查当前时间是否在休息时间内
        public bool IsInRestTime(DateTime currentTime)
        {
            foreach (var rest in RestTimes)
            {
                DateTime restStart = new DateTime(currentTime.Year, currentTime.Month, currentTime.Day,
                                                rest.StartHour, rest.StartMinute, 0);
                DateTime restEnd = new DateTime(currentTime.Year, currentTime.Month, currentTime.Day,
                                              rest.EndHour, rest.EndMinute, 0);

                if (currentTime >= restStart && currentTime <= restEnd)
                {
                    return true;
                }
            }
            return false;
        }

        // 获取当前休息时间的描述（如果有）
        public string GetCurrentRestTimeDescription(DateTime currentTime)
        {
            foreach (var rest in RestTimes)
            {
                DateTime restStart = new DateTime(currentTime.Year, currentTime.Month, currentTime.Day,
                                                rest.StartHour, rest.StartMinute, 0);
                DateTime restEnd = new DateTime(currentTime.Year, currentTime.Month, currentTime.Day,
                                              rest.EndHour, rest.EndMinute, 0);

                if (currentTime >= restStart && currentTime <= restEnd)
                {
                    return $"当前处于休息时间: {restStart:HH:mm} - {restEnd:HH:mm}";
                }
            }
            return "";
        }

        // 计算到下一个休息时间还有多久
        public TimeSpan? GetTimeToNextRestTime(DateTime currentTime)
        {
            TimeSpan? minTime = null;

            foreach (var rest in RestTimes)
            {
                DateTime restStart = new DateTime(currentTime.Year, currentTime.Month, currentTime.Day,
                                                rest.StartHour, rest.StartMinute, 0);

                if (currentTime < restStart)
                {
                    TimeSpan timeToRest = restStart - currentTime;
                    if (minTime == null || timeToRest < minTime.Value)
                    {
                        minTime = timeToRest;
                    }
                }
            }

            return minTime;
        }

        // 克隆方法
        public WorkTimeInfo Clone()
        {
            var clone = new WorkTimeInfo
            {
                StartHour = this.StartHour,
                StartMinute = this.StartMinute,
                EndHour = this.EndHour,
                EndMinute = this.EndMinute
            };

            clone.RestTimes.Clear();
            foreach (var rest in this.RestTimes)
            {
                clone.RestTimes.Add(rest.Clone());
            }

            return clone;
        }
    }
}