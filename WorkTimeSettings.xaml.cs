using System;
using System.Windows;

namespace MideaProductionBoard
{
    public partial class WorkTimeSettings : Window
    {
        public WorkTimeInfo WorkTime { get; private set; }

        public WorkTimeSettings(WorkTimeInfo currentWorkTime)
        {
            InitializeComponent();
            WorkTime = currentWorkTime ?? new WorkTimeInfo();
            LoadCurrentSettings();
            UpdateWorkTimeInfo();
        }

        private void LoadCurrentSettings()
        {
            txtStartHour.Text = WorkTime.StartHour.ToString();
            txtStartMinute.Text = WorkTime.StartMinute.ToString();
            txtEndHour.Text = WorkTime.EndHour.ToString();
            txtEndMinute.Text = WorkTime.EndMinute.ToString();
            txtLunchStartHour.Text = WorkTime.LunchStartHour.ToString();
            txtLunchStartMinute.Text = WorkTime.LunchStartMinute.ToString();
            txtLunchEndHour.Text = WorkTime.LunchEndHour.ToString();
            txtLunchEndMinute.Text = WorkTime.LunchEndMinute.ToString();
        }

        private void UpdateWorkTimeInfo()
        {
            if (int.TryParse(txtStartHour.Text, out int startHour) &&
                int.TryParse(txtStartMinute.Text, out int startMinute) &&
                int.TryParse(txtEndHour.Text, out int endHour) &&
                int.TryParse(txtEndMinute.Text, out int endMinute) &&
                int.TryParse(txtLunchStartHour.Text, out int lunchStartHour) &&
                int.TryParse(txtLunchStartMinute.Text, out int lunchStartMinute) &&
                int.TryParse(txtLunchEndHour.Text, out int lunchEndHour) &&
                int.TryParse(txtLunchEndMinute.Text, out int lunchEndMinute))
            {
                DateTime startTime = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, startHour, startMinute, 0);
                DateTime endTime = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, endHour, endMinute, 0);
                DateTime lunchStartTime = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, lunchStartHour, lunchStartMinute, 0);
                DateTime lunchEndTime = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, lunchEndHour, lunchEndMinute, 0);

                TimeSpan workDuration = endTime - startTime - (lunchEndTime - lunchStartTime);

                txtWorkTimeInfo.Text = $"工作时间: {startTime:HH:mm} - {endTime:HH:mm}\n" +
                                      $"午休时间: {lunchStartTime:HH:mm} - {lunchEndTime:HH:mm}\n" +
                                      $"有效工作时长: {workDuration.TotalHours}小时";
            }
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (ValidateInputs())
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
            DialogResult = false;
            Close();
        }

        private bool ValidateInputs()
        {
            if (!int.TryParse(txtStartHour.Text, out int startHour) || startHour < 0 || startHour > 23)
            {
                MessageBox.Show("请输入有效的开始小时(0-23)", "输入错误", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (!int.TryParse(txtStartMinute.Text, out int startMinute) || startMinute < 0 || startMinute > 59)
            {
                MessageBox.Show("请输入有效的开始分钟(0-59)", "输入错误", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            return true;
        }

        private void TextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            UpdateWorkTimeInfo();
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
    }
}