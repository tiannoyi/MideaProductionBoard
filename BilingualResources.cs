using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// BilingualResources.cs
namespace MideaProductionBoard
{
    public static class BilingualResources
    {
        // 连接状态
        public static string Connected => "已连接 / Connected";
        public static string Disconnected => "未连接 / Disconnected";

        // 计划状态
        public static string PlanUpdated => "计划已更新 / Plan Updated";
        public static string PlanNotUpdated => "计划未更新 / Plan Not Updated";

        // PLC信息
        public static string PlcConnected => "PLC已连接 / PLC Connected";
        public static string PlcDisconnected => "PLC未连接 / PLC Disconnected";

        // 按钮文本
        public static string StartMonitoring => "开始监控 / Start Monitoring";
        public static string StopMonitoring => "停止监控 / Stop Monitoring";
        public static string ManualRefresh => "手动刷新 / Manual Refresh";
        public static string WorkTimeSettings => "工作时间设置 / Work Time Settings";
        public static string UpdatePlan => "更新计划 / Update Plan";

        // 状态消息
        public static string WaitingForPLC => "等待连接PLC... / Waiting for PLC connection...";
        public static string PlcConnectionSuccess => "PLC连接成功 / PLC connected successfully";
        public static string PlcConnectionFailed => "PLC连接失败 / PLC connection failed";
        public static string StartMonitoringPLC => "开始监控PLC数据 / Start monitoring PLC data";
        public static string StopMonitoringPLC => "停止监控PLC数据 / Stop monitoring PLC data";
        public static string PlanUpdateSuccess => "生产计划更新成功 / Production plan updated successfully";

        // 错误消息
        public static string InputValidNumber => "请输入有效的数字 / Please enter a valid number";
        public static string InputError => "输入错误 / Input Error";
        public static string PlcNotConnectedStart => "PLC未连接，无法开始监控 / PLC not connected, cannot start monitoring";
        public static string PlcNotConnectedRefresh => "PLC未连接，无法刷新数据 / PLC not connected, cannot refresh data";
        public static string Notice => "提示 / Notice";

        // 工作时间设置
        public static string WorkTimeUpdated => "工作时间设置已更新 / Work time settings updated";

        //--------------------------------------
        // 工作时间设置相关
        public static string WorkTimeSettingsTitle => "工作时间设置 / Work Time Settings";
        public static string InputErrorTitle => "输入错误 / Input Error";
        public static string TimeLogicErrorTitle => "时间逻辑错误 / Time Logic Error";

        // 验证消息
        public static string EnterValidHour => "请输入有效的开始小时(0-23) / Please enter a valid start hour (0-23)";
        public static string EnterValidMinute => "请输入有效的开始分钟(0-59) / Please enter a valid start minute (0-59)";
        public static string EnterValidEndHour => "请输入有效的结束小时(0-23) / Please enter a valid end hour (0-23)";
        public static string EnterValidEndMinute => "请输入有效的结束分钟(0-59) / Please enter a valid end minute (0-59)";

        // 休息时间验证消息
        public static string RestTimeStartInvalid => "休息时间开始时间无效: {0} / Rest time start time invalid: {0}";
        public static string RestTimeEndInvalid => "休息时间结束时间无效: {0} / Rest time end time invalid: {0}";

        // 工作时间信息
        public static string WorkTimeInfoPrefix => "工作时间: {0} - {1} / Work Time: {0} - {1}\n";
        public static string TotalWorkDuration => "总工作时长: {0:F1}小时 / Total Work Duration: {0:F1} hours\n\n";
        public static string RestTimeSection => "休息时间: / Rest Times:\n";
        public static string NoRestTime => "休息时间: 无 / Rest Times: None\n";
        public static string TotalRestDuration => "总休息时长: {0:F1}小时 / Total Rest Duration: {0:F1} hours\n";
        public static string NetWorkDuration => "净工作时长: {0:F1}小时 / Net Work Duration: {0:F1} hours";

        // 时间逻辑错误消息
        public static string EndTimeMustBeLater => "结束时间必须晚于开始时间 / End time must be later than start time";
        public static string RestTimeMustBeLater => "休息时间 {0} - {1} 的结束时间必须晚于开始时间 / Rest time {0} - {1} must have end time later than start time";
        public static string RestTimeWithinWork => "休息时间 {0} - {1} 必须在工作时间内 / Rest time {0} - {1} must be within work time";
        public static string RestTimeOverlap => "休息时间 {0} - {1} 与 {2} - {3} 有重叠 / Rest time {0} - {1} overlaps with {2} - {3}";

        // 其他消息
        public static string EnterValidWorkTime => "请输入有效的工作时间 / Please enter valid work time";
        public static string CheckRestTimeSettings => "请检查休息时间设置 / Please check rest time settings";
    }
}