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
        private int planTotalOutput = 2500;
        private WorkTimeInfo workTime = new WorkTimeInfo();

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
        }

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
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
            dataTimer = new DispatcherTimer();
            dataTimer.Interval = TimeSpan.FromSeconds(3);
            dataTimer.Tick += DataTimer_Tick;

      

            clockTimer = new DispatcherTimer();
            clockTimer.Interval = TimeSpan.FromSeconds(1);
            clockTimer.Tick += (s, e) =>
            {
                OnPropertyChanged(nameof(CurrentDate));
                OnPropertyChanged(nameof(CurrentTime));
            };
            clockTimer.Start();

      
            reconnectTimer = new DispatcherTimer();
            reconnectTimer.Interval = TimeSpan.FromSeconds(5);
            reconnectTimer.Tick += ReconnectTimer_Tick;

          
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
                    });
                    ShowStatus("PLC连接成功");

                   
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
                }
                else
                {
                    borderPlcStatus.Visibility = Visibility.Visible;
                    txtConnectionStatus.Text = "未连接";
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
                       
                        txtCumulativeOutput.Text = cumulativeOutput.ToString();

                        
                        DateTime now = DateTime.Now;
                        if (lastUpdateTime.Hour != now.Hour)
                        {
                          
                            lastHourOutput = currentHourOutput;
                            currentHourOutput = 0;
                            lastUpdateTime = now;
                        }

                        int outputThisPeriod = cumulativeOutput - lastCumulativeOutput;
                        if (outputThisPeriod >= 0) 
                        {
                            currentHourOutput += outputThisPeriod;
                        }
                        lastCumulativeOutput = cumulativeOutput;

                       
                        txtThisHourUPH.Text = currentHourOutput.ToString();
                        txtLastHourUPH.Text = lastHourOutput.ToString();

                      
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

        private void CalculateActualUPH(int cumulativeOutput)
        {
            try
            {
                DateTime currentTime = DateTime.Now;
                DateTime startTime = new DateTime(currentTime.Year, currentTime.Month, currentTime.Day,
                                                workTime.StartHour, workTime.StartMinute, 0);
                DateTime endTime = new DateTime(currentTime.Year, currentTime.Month, currentTime.Day,
                                              workTime.EndHour, workTime.EndMinute, 0);
                DateTime lunchStartTime = new DateTime(currentTime.Year, currentTime.Month, currentTime.Day,
                                                     workTime.LunchStartHour, workTime.LunchStartMinute, 0);
                DateTime lunchEndTime = new DateTime(currentTime.Year, currentTime.Month, currentTime.Day,
                                                   workTime.LunchEndHour, workTime.LunchEndMinute, 0);

                if (currentTime < startTime || currentTime > endTime)
                {
                    Dispatcher.Invoke(() => txtActualUPH.Text = "0");
                    return;
                }

             
                TimeSpan workedTime = currentTime - startTime;

                
                if (currentTime > lunchEndTime)
                {
                    workedTime -= (lunchEndTime - lunchStartTime);
                }
                else if (currentTime > lunchStartTime)
                {
                    workedTime -= (currentTime - lunchStartTime);
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

        private void UpdateDisplay()
        {
            Dispatcher.Invoke(() =>
            {
                txtTotalOutput.Text = planTotalOutput.ToString();

              
                DateTime startTime = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day,
                                                workTime.StartHour, workTime.StartMinute, 0);
                DateTime endTime = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day,
                                              workTime.EndHour, workTime.EndMinute, 0);
                DateTime lunchStartTime = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day,
                                                     workTime.LunchStartHour, workTime.LunchStartMinute, 0);
                DateTime lunchEndTime = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day,
                                                   workTime.LunchEndHour, workTime.LunchEndMinute, 0);

                TimeSpan workDuration = endTime - startTime - (lunchEndTime - lunchStartTime);
                int averageUPH = workDuration.TotalHours > 0 ? (int)(planTotalOutput / workDuration.TotalHours) : 0;

                txtAverageUPH.Text = averageUPH.ToString();
                txtEffectiveWorkTime.Text = $"{workDuration.TotalHours:F1}小时";
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
                ShowStatus("工作时间设置已更新");
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
        }

        protected override void OnClosed(EventArgs e)
        {
            dataTimer?.Stop();
            clockTimer?.Stop();
            reconnectTimer?.Stop();
            blinkTimer?.Stop();
            plc?.ConnectClose();
            base.OnClosed(e);
        }
    }
}