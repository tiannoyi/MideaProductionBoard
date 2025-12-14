using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Text.RegularExpressions;
using System.Windows.Threading;

namespace MideaProductionBoard
{
    public partial class WorkTimeSettings : Window
    {
        public WorkTimeInfo WorkTime { get; private set; }

        private readonly Regex _numberRegex = new Regex("[^0-9]+");
        private readonly DispatcherTimer _updateTimer;
        private bool _needsUpdate = false;

        public WorkTimeSettings(WorkTimeInfo currentWorkTime)
        {
            InitializeComponent();

            WorkTime = currentWorkTime ?? new WorkTimeInfo();

            // 初始化延迟更新计时器
            _updateTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(300)
            };
            _updateTimer.Tick += UpdateTimer_Tick;

            // 订阅窗口事件
            this.Loaded += WorkTimeSettings_Loaded;
            this.Closed += WorkTimeSettings_Closed;

            AttachTextChangedEvents();
        }

        private void WorkTimeSettings_Closed(object sender, EventArgs e)
        {
            // 清理计时器资源
            if (_updateTimer != null)
            {
                _updateTimer.Stop();
                _updateTimer.Tick -= UpdateTimer_Tick;
            }
        }

        private void WorkTimeSettings_Loaded(object sender, RoutedEventArgs e)
        {
            LoadCurrentSettings();
            UpdateWorkTimeInfo();
        }

        private void UpdateTimer_Tick(object sender, EventArgs e)
        {
            _updateTimer.Stop();
            if (_needsUpdate)
            {
                _needsUpdate = false;
                UpdateWorkTimeInfo();
            }
        }

        private void LoadCurrentSettings()
        {
            DetachTextChangedEvents();

            try
            {
                txtStartHour.Text = WorkTime.StartHour.ToString();
                txtStartMinute.Text = WorkTime.StartMinute.ToString("00");
                txtEndHour.Text = WorkTime.EndHour.ToString();
                txtEndMinute.Text = WorkTime.EndMinute.ToString("00");
                txtLunchStartHour.Text = WorkTime.LunchStartHour.ToString();
                txtLunchStartMinute.Text = WorkTime.LunchStartMinute.ToString("00");
                txtLunchEndHour.Text = WorkTime.LunchEndHour.ToString();
                txtLunchEndMinute.Text = WorkTime.LunchEndMinute.ToString("00");
            }
            finally
            {
                AttachTextChangedEvents();
            }
        }

        private void AttachTextChangedEvents()
        {
            txtStartHour.TextChanged += TimeTextBox_TextChanged;
            txtStartMinute.TextChanged += TimeTextBox_TextChanged;
            txtEndHour.TextChanged += TimeTextBox_TextChanged;
            txtEndMinute.TextChanged += TimeTextBox_TextChanged;
            txtLunchStartHour.TextChanged += TimeTextBox_TextChanged;
            txtLunchStartMinute.TextChanged += TimeTextBox_TextChanged;
            txtLunchEndHour.TextChanged += TimeTextBox_TextChanged;
            txtLunchEndMinute.TextChanged += TimeTextBox_TextChanged;

            txtStartMinute.LostFocus += MinuteTextBox_LostFocus;
            txtEndMinute.LostFocus += MinuteTextBox_LostFocus;
            txtLunchStartMinute.LostFocus += MinuteTextBox_LostFocus;
            txtLunchEndMinute.LostFocus += MinuteTextBox_LostFocus;
        }

        private void DetachTextChangedEvents()
        {
            txtStartHour.TextChanged -= TimeTextBox_TextChanged;
            txtStartMinute.TextChanged -= TimeTextBox_TextChanged;
            txtEndHour.TextChanged -= TimeTextBox_TextChanged;
            txtEndMinute.TextChanged -= TimeTextBox_TextChanged;
            txtLunchStartHour.TextChanged -= TimeTextBox_TextChanged;
            txtLunchStartMinute.TextChanged -= TimeTextBox_TextChanged;
            txtLunchEndHour.TextChanged -= TimeTextBox_TextChanged;
            txtLunchEndMinute.TextChanged -= TimeTextBox_TextChanged;

            txtStartMinute.LostFocus -= MinuteTextBox_LostFocus;
            txtEndMinute.LostFocus -= MinuteTextBox_LostFocus;
            txtLunchStartMinute.LostFocus -= MinuteTextBox_LostFocus;
            txtLunchEndMinute.LostFocus -= MinuteTextBox_LostFocus;
        }

        private void TriggerDelayedUpdate()
        {
            _needsUpdate = true;
            _updateTimer.Stop();
            _updateTimer.Start();
        }

        private void UpdateWorkTimeInfo()
        {
            Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
            {
                if (!TryParseTimeInputs(out DateTime startTime, out DateTime endTime,
                                        out DateTime lunchStartTime, out DateTime lunchEndTime))
                {
                    txtWorkTimeInfo.Text = "请输入有效的时间";
                    txtWorkTimeInfo.Foreground = System.Windows.Media.Brushes.Orange;
                    return;
                }

                string validationMessage = ValidateTimeLogic(startTime, endTime, lunchStartTime, lunchEndTime);
                if (!string.IsNullOrEmpty(validationMessage))
                {
                    txtWorkTimeInfo.Text = validationMessage;
                    txtWorkTimeInfo.Foreground = System.Windows.Media.Brushes.Orange;
                    return;
                }

                TimeSpan totalWorkDuration = endTime - startTime;
                TimeSpan lunchDuration = lunchEndTime - lunchStartTime;
                TimeSpan netWorkDuration = totalWorkDuration - lunchDuration;

                string infoText = $"工作时间: {startTime:HH:mm} - {endTime:HH:mm}\n" +
                                 $"午休时间: {lunchStartTime:HH:mm} - {lunchEndTime:HH:mm}\n" +
                                 $"午休时长: {lunchDuration.TotalHours:F1}小时\n" +
                                 $"总工作时长: {totalWorkDuration.TotalHours:F1}小时\n" +
                                 $"净工作时长: {netWorkDuration.TotalHours:F1}小时";

                txtWorkTimeInfo.Text = infoText;
                txtWorkTimeInfo.Foreground = System.Windows.Media.Brushes.White;
            }));
        }

        private bool TryParseTimeInputs(out DateTime startTime, out DateTime endTime,
                                        out DateTime lunchStartTime, out DateTime lunchEndTime)
        {
            startTime = endTime = lunchStartTime = lunchEndTime = DateTime.MinValue;

            if (!int.TryParse(txtStartHour.Text, out int startHour) || startHour < 0 || startHour > 23) return false;
            if (!int.TryParse(txtStartMinute.Text, out int startMinute) || startMinute < 0 || startMinute > 59) return false;
            if (!int.TryParse(txtEndHour.Text, out int endHour) || endHour < 0 || endHour > 23) return false;
            if (!int.TryParse(txtEndMinute.Text, out int endMinute) || endMinute < 0 || endMinute > 59) return false;
            if (!int.TryParse(txtLunchStartHour.Text, out int lunchStartHour) || lunchStartHour < 0 || lunchStartHour > 23) return false;
            if (!int.TryParse(txtLunchStartMinute.Text, out int lunchStartMinute) || lunchStartMinute < 0 || lunchStartMinute > 59) return false;
            if (!int.TryParse(txtLunchEndHour.Text, out int lunchEndHour) || lunchEndHour < 0 || lunchEndHour > 23) return false;
            if (!int.TryParse(txtLunchEndMinute.Text, out int lunchEndMinute) || lunchEndMinute < 0 || lunchEndMinute > 59) return false;

            DateTime now = DateTime.Now;
            startTime = new DateTime(now.Year, now.Month, now.Day, startHour, startMinute, 0);
            endTime = new DateTime(now.Year, now.Month, now.Day, endHour, endMinute, 0);
            lunchStartTime = new DateTime(now.Year, now.Month, now.Day, lunchStartHour, lunchStartMinute, 0);
            lunchEndTime = new DateTime(now.Year, now.Month, now.Day, lunchEndHour, lunchEndMinute, 0);

            return true;
        }

        private string ValidateTimeLogic(DateTime startTime, DateTime endTime,
                                        DateTime lunchStartTime, DateTime lunchEndTime)
        {
            if (endTime <= startTime)
                return "结束时间必须晚于开始时间";

            if (lunchEndTime <= lunchStartTime)
                return "午休结束时间必须晚于午休开始时间";

            if (lunchStartTime < startTime || lunchEndTime > endTime)
                return "午休时间必须在工作时间内";

            return string.Empty;
        }

        private void TimeTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox textBox = sender as TextBox;
            if (textBox != null && !string.IsNullOrEmpty(textBox.Text))
            {
                if (int.TryParse(textBox.Text, out int value))
                {
                    bool isValid = textBox.Name.Contains("Hour")
                        ? (value >= 0 && value <= 23)
                        : (value >= 0 && value <= 59);

                    textBox.BorderBrush = isValid
                        ? System.Windows.Media.Brushes.LightGray
                        : System.Windows.Media.Brushes.Red;
                }
                else
                {
                    textBox.BorderBrush = System.Windows.Media.Brushes.Red;
                }
            }
            else
            {
                textBox.BorderBrush = System.Windows.Media.Brushes.LightGray;
            }

            TriggerDelayedUpdate();
        }

        private void MinuteTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            TextBox textBox = sender as TextBox;
            if (textBox != null && textBox.Name.Contains("Minute") && !string.IsNullOrEmpty(textBox.Text))
            {
                if (int.TryParse(textBox.Text, out int minute))
                {
                    textBox.Text = minute.ToString("00");
                }
            }
        }

        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            e.Handled = _numberRegex.IsMatch(e.Text);
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            _updateTimer.Stop();

            if (ValidateAllInputs())
            {
                WorkTime.StartHour = int.Parse(txtStartHour.Text);
                WorkTime.StartMinute = int.Parse(txtStartMinute.Text);
                WorkTime.EndHour = int.Parse(txtEndHour.Text);
                WorkTime.EndMinute = int.Parse(txtEndMinute.Text);
                WorkTime.LunchStartHour = int.Parse(txtLunchStartHour.Text);
                WorkTime.LunchStartMinute = int.Parse(txtLunchStartMinute.Text);
                WorkTime.LunchEndHour = int.Parse(txtLunchEndHour.Text);
                WorkTime.LunchEndMinute = int.Parse(txtLunchEndMinute.Text);

                DialogResult = true;
                Close();
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            _updateTimer.Stop();
            DialogResult = false;
            Close();
        }

        private bool ValidateAllInputs()
        {
            if (!ValidateTimeTextBox(txtStartHour, "开始小时", 0, 23)) return false;
            if (!ValidateTimeTextBox(txtStartMinute, "开始分钟", 0, 59)) return false;
            if (!ValidateTimeTextBox(txtEndHour, "结束小时", 0, 23)) return false;
            if (!ValidateTimeTextBox(txtEndMinute, "结束分钟", 0, 59)) return false;
            if (!ValidateTimeTextBox(txtLunchStartHour, "午休开始小时", 0, 23)) return false;
            if (!ValidateTimeTextBox(txtLunchStartMinute, "午休开始分钟", 0, 59)) return false;
            if (!ValidateTimeTextBox(txtLunchEndHour, "午休结束小时", 0, 23)) return false;
            if (!ValidateTimeTextBox(txtLunchEndMinute, "午休结束分钟", 0, 59)) return false;

            if (!TryParseTimeInputs(out DateTime startTime, out DateTime endTime,
                                   out DateTime lunchStartTime, out DateTime lunchEndTime))
            {
                MessageBox.Show("时间格式错误，请检查输入", "验证错误",
                                MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            string validationMessage = ValidateTimeLogic(startTime, endTime, lunchStartTime, lunchEndTime);
            if (!string.IsNullOrEmpty(validationMessage))
            {
                MessageBox.Show(validationMessage, "时间逻辑错误",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            return true;
        }

        private bool ValidateTimeTextBox(TextBox textBox, string fieldName, int minValue, int maxValue)
        {
            if (!int.TryParse(textBox.Text, out int value))
            {
                MessageBox.Show($"{fieldName}必须为数字", "输入错误",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                textBox.Focus();
                return false;
            }

            if (value < minValue || value > maxValue)
            {
                MessageBox.Show($"{fieldName}必须在{minValue}到{maxValue}之间", "输入错误",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                textBox.Focus();
                return false;
            }

            return true;
        }
    }

    public class WorkTimeInfo
    {
        public int StartHour { get; set; } = 8;
        public int StartMinute { get; set; } = 0;
        public int EndHour { get; set; } = 18;
        public int EndMinute { get; set; } = 0;
        public int LunchStartHour { get; set; } = 12;
        public int LunchStartMinute { get; set; } = 0;
        public int LunchEndHour { get; set; } = 13;
        public int LunchEndMinute { get; set; } = 0;

        public double CalculateNetWorkHours()
        {
            DateTime startTime = DateTime.Today.AddHours(StartHour).AddMinutes(StartMinute);
            DateTime endTime = DateTime.Today.AddHours(EndHour).AddMinutes(EndMinute);
            DateTime lunchStart = DateTime.Today.AddHours(LunchStartHour).AddMinutes(LunchStartMinute);
            DateTime lunchEnd = DateTime.Today.AddHours(LunchEndHour).AddMinutes(LunchEndMinute);

            TimeSpan totalDuration = endTime - startTime;
            TimeSpan lunchDuration = lunchEnd - lunchStart;

            return (totalDuration - lunchDuration).TotalHours;
        }

        public bool IsValid()
        {
            return StartHour >= 0 && StartHour <= 23 &&
                   StartMinute >= 0 && StartMinute <= 59 &&
                   EndHour >= 0 && EndHour <= 23 &&
                   EndMinute >= 0 && EndMinute <= 59 &&
                   LunchStartHour >= 0 && LunchStartHour <= 23 &&
                   LunchStartMinute >= 0 && LunchStartMinute <= 59 &&
                   LunchEndHour >= 0 && LunchEndHour <= 23 &&
                   LunchEndMinute >= 0 && LunchEndMinute <= 59 &&
                   CalculateNetWorkHours() > 0;
        }
    }
}