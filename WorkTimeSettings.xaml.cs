using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Text.RegularExpressions;
using System.Windows.Threading;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;

namespace MideaProductionBoard
{
    public partial class WorkTimeSettings : Window
    {
        public WorkTimeInfo WorkTime { get; private set; }

        private readonly Regex _numberRegex = new Regex("[^0-9]+");
        private DispatcherTimer _updateTimer;  // 去掉 readonly 修饰符
        private bool _needsUpdate = false;

        public WorkTimeSettings(WorkTimeInfo currentWorkTime)
        {
            InitializeComponent();

            // 创建副本，避免修改原数据
            WorkTime = currentWorkTime?.Clone() ?? new WorkTimeInfo();

            // 初始化延迟更新计时器
            _updateTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(500)
            };
            _updateTimer.Tick += UpdateTimer_Tick;

            // 订阅窗口事件
            this.Loaded += WorkTimeSettings_Loaded;
            this.Closed += WorkTimeSettings_Closed;

            // 绑定休息时间列表
            RestTimesItemsControl.ItemsSource = WorkTime.RestTimes;

            // 不再订阅 PropertyChanged 事件，因为 RestTime 类没有实现 INotifyPropertyChanged
            // 改为在 XAML 中为文本框添加 TextChanged 事件
        }

        private void WorkTimeSettings_Closed(object sender, EventArgs e)
        {
            if (_updateTimer != null)  // 添加 null 检查
            {
                _updateTimer.Stop();
                _updateTimer.Tick -= UpdateTimer_Tick;
                _updateTimer = null;  // 设置为 null，防止后续访问
            }
        }

        private void WorkTimeSettings_Loaded(object sender, RoutedEventArgs e)
        {
            LoadCurrentSettings();
            UpdateWorkTimeInfo();
        }

        private void LoadCurrentSettings()
        {
            txtStartHour.Text = WorkTime.StartHour.ToString();
            txtStartMinute.Text = WorkTime.StartMinute.ToString("00");
            txtEndHour.Text = WorkTime.EndHour.ToString();
            txtEndMinute.Text = WorkTime.EndMinute.ToString("00");
        }

        private void UpdateTimer_Tick(object sender, EventArgs e)
        {
            if (_updateTimer == null) return;  // 添加 null 检查

            _updateTimer.Stop();
            if (_needsUpdate)
            {
                _needsUpdate = false;
                UpdateWorkTimeInfo();
            }
        }

        private void TriggerDelayedUpdate()
        {
            if (_updateTimer == null) return;  // 添加 null 检查

            _needsUpdate = true;
            _updateTimer.Stop();
            _updateTimer.Start();
        }

        private void UpdateWorkTimeInfo()
        {
            try
            {
                Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
                {
                    // 先清空错误信息
                    txtWorkTimeInfo.Text = "";

                    if (!TryParseWorkTimeInputs())
                    {
                        txtWorkTimeInfo.Text = "请输入有效的工作时间";
                        txtWorkTimeInfo.Foreground = System.Windows.Media.Brushes.Orange;
                        return;
                    }

                    if (!ValidateAllRestTimes())
                    {
                        txtWorkTimeInfo.Text = "请检查休息时间设置";
                        txtWorkTimeInfo.Foreground = System.Windows.Media.Brushes.Orange;
                        return;
                    }

                    // 验证时间逻辑
                    string validationMessage = ValidateTimeLogic();
                    if (!string.IsNullOrEmpty(validationMessage))
                    {
                        txtWorkTimeInfo.Text = validationMessage;
                        txtWorkTimeInfo.Foreground = System.Windows.Media.Brushes.Orange;
                        return;
                    }

                    // 如果没有错误，计算并显示工作时间信息
                    CalculateAndDisplayWorkTimeInfo();
                }));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"UpdateWorkTimeInfo error: {ex.Message}");
            }
        }

        private bool TryParseWorkTimeInputs()
        {
            if (!int.TryParse(txtStartHour.Text, out int startHour) || !IsValidHour(startHour)) return false;
            if (!int.TryParse(txtStartMinute.Text, out int startMinute) || !IsValidMinute(startMinute)) return false;
            if (!int.TryParse(txtEndHour.Text, out int endHour) || !IsValidHour(endHour)) return false;
            if (!int.TryParse(txtEndMinute.Text, out int endMinute) || !IsValidMinute(endMinute)) return false;

            WorkTime.StartHour = startHour;
            WorkTime.StartMinute = startMinute;
            WorkTime.EndHour = endHour;
            WorkTime.EndMinute = endMinute;

            return true;
        }

        private bool ValidateAllRestTimes()
        {
            foreach (var restTime in WorkTime.RestTimes)
            {
                if (!IsValidHour(restTime.StartHour) || !IsValidMinute(restTime.StartMinute) ||
                    !IsValidHour(restTime.EndHour) || !IsValidMinute(restTime.EndMinute))
                {
                    return false;
                }
            }
            return true;
        }

        private string ValidateTimeLogic()
        {
            DateTime startTime = DateTime.Today.AddHours(WorkTime.StartHour).AddMinutes(WorkTime.StartMinute);
            DateTime endTime = DateTime.Today.AddHours(WorkTime.EndHour).AddMinutes(WorkTime.EndMinute);

            if (endTime <= startTime)
                return "结束时间必须晚于开始时间";

            // 验证每个休息时间
            foreach (var restTime in WorkTime.RestTimes)
            {
                DateTime restStart = DateTime.Today.AddHours(restTime.StartHour).AddMinutes(restTime.StartMinute);
                DateTime restEnd = DateTime.Today.AddHours(restTime.EndHour).AddMinutes(restTime.EndMinute);

                if (restEnd <= restStart)
                {
                    // 添加调试日志，查看具体值
                    Console.WriteLine($"休息时间验证失败: Start={restTime.StartHour}:{restTime.StartMinute}, End={restTime.EndHour}:{restTime.EndMinute}");
                    Console.WriteLine($"转换为DateTime: Start={restStart:HH:mm}, End={restEnd:HH:mm}");

                    return $"休息时间 {restStart:HH:mm} - {restEnd:HH:mm} 的结束时间必须晚于开始时间";
                }

                if (restStart < startTime || restEnd > endTime)
                    return $"休息时间 {restStart:HH:mm} - {restEnd:HH:mm} 必须在工作时间内";
            }

            // 检查休息时间之间是否有重叠
            for (int i = 0; i < WorkTime.RestTimes.Count; i++)
            {
                for (int j = i + 1; j < WorkTime.RestTimes.Count; j++)
                {
                    DateTime rest1Start = DateTime.Today.AddHours(WorkTime.RestTimes[i].StartHour).AddMinutes(WorkTime.RestTimes[i].StartMinute);
                    DateTime rest1End = DateTime.Today.AddHours(WorkTime.RestTimes[i].EndHour).AddMinutes(WorkTime.RestTimes[i].EndMinute);
                    DateTime rest2Start = DateTime.Today.AddHours(WorkTime.RestTimes[j].StartHour).AddMinutes(WorkTime.RestTimes[j].StartMinute);
                    DateTime rest2End = DateTime.Today.AddHours(WorkTime.RestTimes[j].EndHour).AddMinutes(WorkTime.RestTimes[j].EndMinute);

                    if (rest1Start < rest2End && rest2Start < rest1End)
                        return $"休息时间 {rest1Start:HH:mm} - {rest1End:HH:mm} 与 {rest2Start:HH:mm} - {rest2End:HH:mm} 有重叠";
                }
            }

            return string.Empty;
        }

        private void FixRestTimeBindings()
        {
            // 强制刷新 ItemsControl 的数据绑定
            RestTimesItemsControl.Items.Refresh();

            // 触发属性更改通知
            var items = RestTimesItemsControl.Items;
            var collection = items as System.Collections.Specialized.INotifyCollectionChanged;
            if (collection != null)
            {
                var collectionChanged = new System.Collections.Specialized.NotifyCollectionChangedEventArgs(
                    System.Collections.Specialized.NotifyCollectionChangedAction.Reset);

                // 强制集合更改通知
                // 注意：这里使用了反射来调用内部方法，这是最后的手段
                var method = collection.GetType().GetMethod("OnCollectionChanged",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                method?.Invoke(collection, new object[] { collectionChanged });
            }
        }

        private void ValidateImmediately()
        {
            // 停止计时器
            if (_updateTimer != null)
            {
                _updateTimer.Stop();
            }

            // 立即更新
            UpdateWorkTimeInfo();
        }

        private void CalculateAndDisplayWorkTimeInfo()
        {
            DateTime startTime = DateTime.Today.AddHours(WorkTime.StartHour).AddMinutes(WorkTime.StartMinute);
            DateTime endTime = DateTime.Today.AddHours(WorkTime.EndHour).AddMinutes(WorkTime.EndMinute);

            TimeSpan totalWorkDuration = endTime - startTime;
            TimeSpan totalRestDuration = TimeSpan.Zero;

            string infoText = $"工作时间: {startTime:HH:mm} - {endTime:HH:mm}\n";
            infoText += $"总工作时长: {totalWorkDuration.TotalHours:F1}小时\n\n";

            if (WorkTime.RestTimes.Count > 0)
            {
                infoText += "休息时间:\n";
                int index = 1;
                foreach (var restTime in WorkTime.RestTimes)
                {
                    DateTime restStart = DateTime.Today.AddHours(restTime.StartHour).AddMinutes(restTime.StartMinute);
                    DateTime restEnd = DateTime.Today.AddHours(restTime.EndHour).AddMinutes(restTime.EndMinute);
                    TimeSpan restDuration = restEnd - restStart;
                    totalRestDuration += restDuration;

                    infoText += $"{index}. {restStart:HH:mm} - {restEnd:HH:mm} ({restDuration.TotalHours:F1}小时)\n";
                    index++;
                }
            }
            else
            {
                infoText += "休息时间: 无\n";
            }

            TimeSpan netWorkDuration = totalWorkDuration - totalRestDuration;
            infoText += $"\n总休息时长: {totalRestDuration.TotalHours:F1}小时\n";
            infoText += $"净工作时长: {netWorkDuration.TotalHours:F1}小时";

            txtWorkTimeInfo.Text = infoText;
            txtWorkTimeInfo.Foreground = System.Windows.Media.Brushes.White;
        }

        // 事件处理程序
        private void BtnAddRestTime_Click(object sender, RoutedEventArgs e)
        {
            // 创建一个新的休息时间，默认为上一个休息时间结束后1小时，或者工作开始时间后2小时
            var newRestTime = new RestTime();

            if (WorkTime.RestTimes.Count > 0)
            {
                var lastRestTime = WorkTime.RestTimes.Last();
                newRestTime.StartHour = lastRestTime.EndHour;
                newRestTime.StartMinute = lastRestTime.EndMinute;
                newRestTime.EndHour = (newRestTime.StartHour + 1) % 24; // 默认休息1小时
                newRestTime.EndMinute = newRestTime.StartMinute;
            }
            else
            {
                newRestTime.StartHour = (WorkTime.StartHour + 2) % 24; // 工作开始后2小时
                newRestTime.StartMinute = 0;
                newRestTime.EndHour = (newRestTime.StartHour + 1) % 24; // 休息1小时
                newRestTime.EndMinute = 0;
            }

            WorkTime.RestTimes.Add(newRestTime);
            TriggerDelayedUpdate();
        }

        private void DeleteRestTime_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is RestTime restTime)
            {
                WorkTime.RestTimes.Remove(restTime);
                TriggerDelayedUpdate();
            }
        }

        // 添加这个方法来处理休息时间文本框的TextChanged事件
        private void RestTimeTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            TriggerDelayedUpdate();
        }

        private void TimeTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            TriggerDelayedUpdate();
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (_updateTimer != null)  // 添加 null 检查
            {
                _updateTimer.Stop();
            }

            if (ValidateAllInputs())
            {
                DialogResult = true;
                Close();
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            if (_updateTimer != null)  // 添加 null 检查
            {
                _updateTimer.Stop();
            }
            DialogResult = false;
            Close();
        }

        private bool ValidateAllInputs()
        {
            // 验证工作时间
            if (!int.TryParse(txtStartHour.Text, out int startHour) || !IsValidHour(startHour))
            {
                MessageBox.Show("请输入有效的开始小时(0-23)", "输入错误", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtStartHour.Focus();
                return false;
            }

            if (!int.TryParse(txtStartMinute.Text, out int startMinute) || !IsValidMinute(startMinute))
            {
                MessageBox.Show("请输入有效的开始分钟(0-59)", "输入错误", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtStartMinute.Focus();
                return false;
            }

            if (!int.TryParse(txtEndHour.Text, out int endHour) || !IsValidHour(endHour))
            {
                MessageBox.Show("请输入有效的结束小时(0-23)", "输入错误", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtEndHour.Focus();
                return false;
            }

            if (!int.TryParse(txtEndMinute.Text, out int endMinute) || !IsValidMinute(endMinute))
            {
                MessageBox.Show("请输入有效的结束分钟(0-59)", "输入错误", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtEndMinute.Focus();
                return false;
            }

            WorkTime.StartHour = startHour;
            WorkTime.StartMinute = startMinute;
            WorkTime.EndHour = endHour;
            WorkTime.EndMinute = endMinute;

            // 验证休息时间
            foreach (var restTime in WorkTime.RestTimes)
            {
                if (!IsValidHour(restTime.StartHour) || !IsValidMinute(restTime.StartMinute))
                {
                    MessageBox.Show($"休息时间开始时间无效: {restTime.StartHour}:{restTime.StartMinute}",
                                    "输入错误", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return false;
                }

                if (!IsValidHour(restTime.EndHour) || !IsValidMinute(restTime.EndMinute))
                {
                    MessageBox.Show($"休息时间结束时间无效: {restTime.EndHour}:{restTime.EndMinute}",
                                    "输入错误", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return false;
                }
            }

            // 验证时间逻辑
            string validationMessage = ValidateTimeLogic();
            if (!string.IsNullOrEmpty(validationMessage))
            {
                MessageBox.Show(validationMessage, "时间逻辑错误",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            return true;
        }

        // 辅助方法
        private bool IsValidHour(int hour) => hour >= 0 && hour <= 23;
        private bool IsValidMinute(int minute) => minute >= 0 && minute <= 59;

        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            e.Handled = _numberRegex.IsMatch(e.Text);
        }
    }


}