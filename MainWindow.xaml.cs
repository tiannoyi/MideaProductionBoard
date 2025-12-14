using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Threading;
using HslCommunication;
using HslCommunication.Profinet.Melsec;

namespace MideaProductionBoard
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private MelsecMcNet plc;
        private DispatcherTimer dataTimer;
        private DispatcherTimer clockTimer;
        private DispatcherTimer reconnectTimer;
        private DispatcherTimer blinkTimer;
        private int lastCumulativeOutput = 0;
        private int currentHourOutput = 0;
        private int lastHourOutput = 0;
        private DateTime lastUpdateTime;
        private bool isMonitoring = false;
        private bool isPlcConnected = false;
        private bool isBlinking = false;
        private const string PLC_IP = "192.168.1.30";
        private const int PLC_PORT = 4998;
        private const string PLC_REGISTER = "D2600";
        private int planTotalOutput = 3400;
        private WorkTimeInfo workTime = new WorkTimeInfo();
        private DispatcherTimer workStatusTimer; // 新增：工作状态检查定时器

        public event PropertyChangedEventHandler PropertyChanged;

        public string CurrentDate => DateTime.Now.ToString("yyyy年MM月dd日 dddd");
        public string CurrentTime => DateTime.Now.ToString("HH:mm:ss");

        public bool IsPlcConnected
        {
            get => isPlcConnected;
            set
            {
                if (isPlcConnected != value)
                {
                    isPlcConnected = value;
                    OnPropertyChanged(nameof(IsPlcConnected));
                    UpdatePlcStatusDisplay();
                }
            }
        }

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
            txtPlcAddress.Text = $"{PLC_IP}:{PLC_PORT}";

            InitializeTimers();
            ResetDisplayToZero();
            UpdateDisplay();
            StartAutoConnect();

            // 初始化工作状态检查定时器
            InitializeWorkStatusTimer();

            // 添加窗口大小变化事件处理
            this.SizeChanged += MainWindow_SizeChanged;
        }

        // 窗口大小变化事件处理
        private void MainWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            this.UpdateLayout();
        }

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        // 初始化工作状态检查定时器
        private void InitializeWorkStatusTimer()
        {
            workStatusTimer = new DispatcherTimer();
            workStatusTimer.Interval = TimeSpan.FromSeconds(30); // 每30秒检查一次
            workStatusTimer.Tick += WorkStatusTimer_Tick;
            workStatusTimer.Start();
        }

        private void WorkStatusTimer_Tick(object sender, EventArgs e)
        {
            // 检查当前是否在休息时间，并更新状态
            CheckWorkTimeStatus();
        }

        private void CheckWorkTimeStatus()
        {
            // 这里可以添加逻辑来检查当前是否在休息时间
            // 例如，可以在状态栏显示信息
            bool isInRestTime = workTime.IsInRestTime(DateTime.Now);
            if (isInRestTime)
            {
                string restDescription = workTime.GetCurrentRestTimeDescription(DateTime.Now);
                if (!string.IsNullOrEmpty(restDescription))
                {
                    // 可以选择在状态栏显示休息信息
                    // ShowStatus(restDescription);
                }
            }
        }

        private void InitializePlc()
        {
            try
            {
                plc = new MelsecMcNet(PLC_IP, PLC_PORT);
                plc.ConnectTimeOut = 3000;
                plc.ReceiveTimeOut = 3000;
            }
            catch (Exception ex)
            {
                ShowStatus($"PLC初始化失败: {ex.Message}");
            }
        }

        private void ResetDisplayToZero()
        {
            Dispatcher.Invoke(() =>
            {
                txtTotalOutput.Text = "0";
                txtAverageUPH.Text = "0";
                txtActualUPH.Text = "0";
                txtLastHourUPH.Text = "0";
                txtThisHourUPH.Text = "0";
                txtCumulativeOutput.Text = "0";
            });
        }

        private bool CheckPlcConnection()
        {
            if (plc == null) return false;

            try
            {
                var testResult = plc.ReadInt32(PLC_REGISTER);
                return testResult.IsSuccess;
            }
            catch
            {
                return false;
            }
        }

        private void InitializeTimers()
        {
            // 数据更新定时器
            dataTimer = new DispatcherTimer();
            dataTimer.Interval = TimeSpan.FromSeconds(3);
            dataTimer.Tick += DataTimer_Tick;

            // 时钟定时器
            clockTimer = new DispatcherTimer();
            clockTimer.Interval = TimeSpan.FromSeconds(1);
            clockTimer.Tick += (s, e) =>
            {
                OnPropertyChanged(nameof(CurrentDate));
                OnPropertyChanged(nameof(CurrentTime));
            };
            clockTimer.Start();

            // 重连定时器
            reconnectTimer = new DispatcherTimer();
            reconnectTimer.Interval = TimeSpan.FromSeconds(5);
            reconnectTimer.Tick += ReconnectTimer_Tick;

            // 闪烁定时器
            blinkTimer = new DispatcherTimer();
            blinkTimer.Interval = TimeSpan.FromSeconds(0.5);
            blinkTimer.Tick += BlinkTimer_Tick;
        }

        private void BlinkTimer_Tick(object sender, EventArgs e)
        {
            if (!IsPlcConnected)
            {
                isBlinking = !isBlinking;
                borderPlcStatus.Opacity = isBlinking ? 1.0 : 0.3;
            }
            else
            {
                borderPlcStatus.Opacity = 1.0;
            }
        }

        private void StartAutoConnect()
        {
            reconnectTimer.Start();
            blinkTimer.Start();
            ConnectToPlc();
        }

        private void ReconnectTimer_Tick(object sender, EventArgs e)
        {
            if (!IsPlcConnected)
            {
                ConnectToPlc();
            }
        }

        private void ConnectToPlc()
        {
            try
            {
                if (plc == null)
                {
                    InitializePlc();
                }

                OperateResult connectResult = plc.ConnectServer();
                if (connectResult.IsSuccess)
                {
                    IsPlcConnected = true;
                    Dispatcher.Invoke(() =>
                    {
                        txtConnectionStatus.Text = "已连接";
                        txtConnectionStatus.Foreground = System.Windows.Media.Brushes.LightGreen;
                    });
                    ShowStatus("PLC连接成功");

                    // 连接成功后立即读取一次数据
                    ReadPlcData();
                }
                else
                {
                    IsPlcConnected = false;
                    ShowStatus($"PLC连接失败: {connectResult.Message}");
                }
            }
            catch (Exception ex)
            {
                IsPlcConnected = false;
                ShowStatus($"PLC连接异常: {ex.Message}");
            }
        }

        private void UpdatePlcStatusDisplay()
        {
            Dispatcher.Invoke(() =>
            {
                if (IsPlcConnected)
                {
                    borderPlcStatus.Visibility = Visibility.Collapsed;
                    txtConnectionStatus.Text = "已连接";
                    txtConnectionStatus.Foreground = System.Windows.Media.Brushes.LightGreen;
                }
                else
                {
                    borderPlcStatus.Visibility = Visibility.Visible;
                    txtConnectionStatus.Text = "未连接";
                    txtConnectionStatus.Foreground = System.Windows.Media.Brushes.Red;
                }
            });
        }

        private void DataTimer_Tick(object sender, EventArgs e)
        {
            if (isMonitoring && IsPlcConnected)
            {
                ReadPlcData();
            }
        }

        private void ReadPlcData()
        {
            if (plc == null) return;

            try
            {
                var result = plc.ReadInt32(PLC_REGISTER);
                if (result.IsSuccess)
                {
                    IsPlcConnected = true;
                    int cumulativeOutput = result.Content;

                    Dispatcher.Invoke(() =>
                    {
                        // 更新累计到达量
                        txtCumulativeOutput.Text = cumulativeOutput.ToString();

                        // 检查是否需要重置小时产量
                        DateTime now = DateTime.Now;
                        if (lastUpdateTime.Hour != now.Hour)
                        {
                            // 小时切换，将当前小时产量保存为上小时产量
                            lastHourOutput = currentHourOutput;
                            currentHourOutput = 0;
                            lastUpdateTime = now;
                        }

                        int outputThisPeriod = cumulativeOutput - lastCumulativeOutput;
                        if (outputThisPeriod >= 0) // 防止负数
                        {
                            currentHourOutput += outputThisPeriod;
                        }
                        lastCumulativeOutput = cumulativeOutput;

                        // 更新小时产量显示
                        txtThisHourUPH.Text = currentHourOutput.ToString();
                        txtLastHourUPH.Text = lastHourOutput.ToString();

                        // 计算实际UPH（支持多个休息时间）
                        CalculateActualUPH(cumulativeOutput);

                        ShowStatus($"数据更新成功 - {DateTime.Now:HH:mm:ss}");
                    });
                }
                else
                {
                    IsPlcConnected = false;
                    Dispatcher.Invoke(() => ShowStatus($"读取PLC数据失败: {result.Message}"));
                }
            }
            catch (Exception ex)
            {
                IsPlcConnected = false;
                Dispatcher.Invoke(() => ShowStatus($"读取数据异常: {ex.Message}"));
            }
        }

        // 修改：支持多个休息时间的CalculateActualUPH方法
        private void CalculateActualUPH(int cumulativeOutput)
        {
            try
            {
                DateTime currentTime = DateTime.Now;
                DateTime startTime = new DateTime(currentTime.Year, currentTime.Month, currentTime.Day,
                                                workTime.StartHour, workTime.StartMinute, 0);
                DateTime endTime = new DateTime(currentTime.Year, currentTime.Month, currentTime.Day,
                                              workTime.EndHour, workTime.EndMinute, 0);

                // 如果当前时间不在工作时间内，则实际UPH为0
                if (currentTime < startTime || currentTime > endTime)
                {
                    Dispatcher.Invoke(() => txtActualUPH.Text = "0");
                    return;
                }

                // 计算已工作时间（从开始时间到现在）
                TimeSpan workedTime = currentTime - startTime;

                // 减去所有已经过去或正在进行的休息时间
                foreach (var rest in workTime.RestTimes)
                {
                    DateTime restStart = new DateTime(currentTime.Year, currentTime.Month, currentTime.Day,
                                                    rest.StartHour, rest.StartMinute, 0);
                    DateTime restEnd = new DateTime(currentTime.Year, currentTime.Month, currentTime.Day,
                                                  rest.EndHour, rest.EndMinute, 0);

                    // 如果休息时间在当前时间之前已经完全过去
                    if (currentTime > restEnd)
                    {
                        // 减去整个休息时间段
                        TimeSpan restDuration = restEnd - restStart;
                        workedTime -= restDuration;
                    }
                    // 如果当前时间在休息时间内
                    else if (currentTime >= restStart && currentTime <= restEnd)
                    {
                        // 减去从休息开始到当前时间的时间段
                        TimeSpan restSoFar = currentTime - restStart;
                        workedTime -= restSoFar;
                    }
                    // 如果休息时间还没开始，不做处理
                }

                double workedHours = workedTime.TotalHours;

                if (workedHours > 0)
                {
                    int actualUPH = (int)(cumulativeOutput / workedHours);
                    Dispatcher.Invoke(() => txtActualUPH.Text = actualUPH.ToString());
                }
                else
                {
                    Dispatcher.Invoke(() => txtActualUPH.Text = "0");
                }
            }
            catch (Exception ex)
            {
                ShowStatus($"计算实际UPH错误: {ex.Message}");
            }
        }

        // 修改：支持多个休息时间的UpdateDisplay方法
        private void UpdateDisplay()
        {
            Dispatcher.Invoke(() =>
            {
                // 更新计划总产量
                txtTotalOutput.Text = planTotalOutput.ToString();

                // 计算有效工作时间和平均UPH
                DateTime startTime = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day,
                                                workTime.StartHour, workTime.StartMinute, 0);
                DateTime endTime = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day,
                                              workTime.EndHour, workTime.EndMinute, 0);

                // 计算总工作时间
                TimeSpan totalWorkDuration = endTime - startTime;

                // 计算总休息时间
                TimeSpan totalRestDuration = TimeSpan.Zero;
                foreach (var rest in workTime.RestTimes)
                {
                    DateTime restStart = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day,
                                                    rest.StartHour, rest.StartMinute, 0);
                    DateTime restEnd = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day,
                                                  rest.EndHour, rest.EndMinute, 0);
                    totalRestDuration += (restEnd - restStart);
                }

                // 计算净工作时间
                TimeSpan netWorkDuration = totalWorkDuration - totalRestDuration;
                double netWorkHours = netWorkDuration.TotalHours;

                // 计算平均UPH
                int averageUPH = netWorkHours > 0 ? (int)(planTotalOutput / netWorkHours) : 0;

                txtAverageUPH.Text = averageUPH.ToString();
                txtEffectiveWorkTime.Text = $"{netWorkHours:F1}小时";
            });
        }

        private void ShowStatus(string message)
        {
            Dispatcher.Invoke(() =>
            {
                txtStatus.Text = $"{DateTime.Now:HH:mm:ss} - {message}";
            });
        }

        private void BtnWorkTimeSettings_Click(object sender, RoutedEventArgs e)
        {
            var settingsWindow = new WorkTimeSettings(workTime);
            if (settingsWindow.ShowDialog() == true)
            {
                workTime = settingsWindow.WorkTime;
                UpdateDisplay();
                CheckWorkTimeStatus(); // 更新工作状态
                ShowStatus($"工作时间设置已更新，共 {workTime.RestTimes.Count} 个休息时间段");
            }
        }

        private void BtnUpdatePlan_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (int.TryParse(txtPlanTotalOutput.Text, out int totalOutput))
                {
                    planTotalOutput = totalOutput;
                    UpdateDisplay();
                    Dispatcher.Invoke(() => txtPlanStatus.Text = "计划已更新");
                    ShowStatus("生产计划更新成功");
                }
                else
                {
                    MessageBox.Show("请输入有效的数字", "输入错误", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                ShowStatus($"更新计划失败: {ex.Message}");
            }
        }

        private void BtnStart_Click(object sender, RoutedEventArgs e)
        {
            if (!isMonitoring)
            {
                if (IsPlcConnected)
                {
                    dataTimer.Start();
                    isMonitoring = true;
                    lastUpdateTime = DateTime.Now;
                    ShowStatus("开始监控PLC数据");

                    // 立即读取一次数据
                    ReadPlcData();
                }
                else
                {
                    MessageBox.Show("PLC未连接，无法开始监控", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }

        private void BtnStop_Click(object sender, RoutedEventArgs e)
        {
            if (isMonitoring)
            {
                dataTimer.Stop();
                isMonitoring = false;
                ShowStatus("停止监控PLC数据");
            }
        }

        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            if (IsPlcConnected)
            {
                ReadPlcData();
            }
            else
            {
                MessageBox.Show("PLC未连接，无法刷新数据", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        // XAML中绑定的Window_SizeChanged事件处理程序
        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            this.UpdateLayout();
        }

        protected override void OnClosed(EventArgs e)
        {
            dataTimer?.Stop();
            clockTimer?.Stop();
            reconnectTimer?.Stop();
            blinkTimer?.Stop();
            workStatusTimer?.Stop(); // 停止工作状态检查定时器
            plc?.ConnectClose();
            base.OnClosed(e);
        }
    }

    // 字体大小转换器 - 根据窗口高度动态计算字体大小
    public class FontSizeConverter : System.Windows.Data.IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is double windowHeight && parameter is string baseSizeStr)
            {
                if (double.TryParse(baseSizeStr, out double baseSize))
                {
                    // 根据窗口高度动态调整字体大小
                    // 基准高度为915像素（窗口默认高度）
                    double scaleFactor = windowHeight / 915.0;

                    // 对于全屏显示，更积极地放大字体
                    // 全屏时（假设1920x1080，窗口高度约1000像素），scaleFactor约为1.1
                    // 为了让字体更大，我们增加放大系数
                    if (scaleFactor > 1.0)
                    {
                        // 当窗口放大时，字体放大的更明显
                        scaleFactor = 1.0 + (scaleFactor - 1.0) * 2.0; // 增加放大系数到2.0
                    }
                    else if (scaleFactor < 1.0)
                    {
                        // 当窗口缩小时，字体缩小幅度减小
                        scaleFactor = 1.0 - (1.0 - scaleFactor) * 0.5;
                    }

                    // 限制最小和最大缩放比例
                    scaleFactor = Math.Max(0.7, Math.Min(scaleFactor, 2.0));

                    // 返回缩放后的字体大小
                    double finalSize = baseSize * scaleFactor;

                    // 确保字体大小不会太小（最小12像素）
                    return Math.Max(12, finalSize);
                }
            }
            return 16; // 默认值
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}