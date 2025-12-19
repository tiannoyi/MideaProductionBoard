# Midea Production Board - 三菱PLC生产看板监控系统

## 📖 项目简介

这是一个基于 **WPF (.NET 8)** 开发的生产监控看板系统，专为**三菱PLC (Mitsubishi PLC)** 设计。该系统实时监控生产线产量数据，计算关键生产效率指标（UPH），并提供了直观的可视化界面。项目已从商业库 **HslCommunication** 成功迁移至完全开源的 **IoTClient** 库。

## ✨ 核心功能

- **🔌 实时PLC通信**：通过以太网（MC协议）连接三菱PLC，实时读取生产数据。
- **📊 生产数据监控**：动态显示累计产量、小时产量、实际UPH等关键指标。
- **⏰ 工作时间管理**：支持配置多个工作时间段和休息时间，准确计算有效工时。
- **📈 效率智能分析**：自动计算平均UPH、实际UPH、计划完成率。
- **🔁 智能连接管理**：具备自动重连机制，确保网络波动时监控不中断。
- **🎨 自适应界面**：支持窗口缩放，字体大小根据窗口尺寸动态调整。

## 🛠️ 技术栈与依赖

- **框架**: .NET 8.0, WPF
- **PLC通信库**: **[IoTClient](https://www.nuget.org/packages/IoTClient)** (版本：1.0.1，开源免费)
- **编程模式**: MVVM (Model-View-ViewModel) 数据绑定
- **UI组件**: 标准WPF控件，使用 `DispatcherTimer` 进行UI线程定时更新

## 🚀 快速开始

### 环境准备
1. **开发环境**: Windows 10/11 + Visual Studio 2022 或更高版本。
2. **运行时**: 安装 [.NET 8.0 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/8.0)。
3. **硬件**: 确保PLC设备与运行本软件的PC处于同一局域网。

### 安装与运行
1. **克隆项目**
   ```bash
   git clone https://github.com/YOUR_USERNAME/MideaProductionBoard.git
   cd MideaProductionBoard
   
## 还原NuGet包
  使用Visual Studio打开 MideaProductionBoard.sln，IDE将自动还原包。
  或通过命令行：dotnet restore。

## 配置PLC连接
  打开 MainWindow.xaml.cs 文件。
  修改以下常量以匹配你的PLC设置：
  ```bash
  private const string PLC_IP = "192.168.1.30"; // PLC的IP地址
  private const int PLC_PORT = 4998;           // 端口号（通常为4998）
  private const string PLC_REGISTER = "D2600"; // 存储累计产量的寄存器地址
  ```

## 编译运行
  
  在Visual Studio中按 F5 键。或通过命令行：dotnet run --project MideaProductionBoard.csproj。
  
## ⚙️ 配置说明
1. **PLC端配置**
型号与协议: 支持使用 MC协议（Qna-3E） 进行以太网通信的三菱PLC（如Q系列、L系列、FX5U等）。
2. **网络设置**
为PLC配置静态IP地址，确保 PLC_IP 和 PLC_PORT 与软件设置一致，并关闭相关防火墙策略。
3. **数据点**
确保 D2600 寄存器（或你修改后的地址）用于存储累计产量（32位整数）。

## 软件功能配置
1. **工作时间设置**
点击界面“工作时间设置”按钮，可配置每日起止时间及多个休息时段。
2. **生产计划**
在“计划总产量”输入框中输入当日目标，点击“更新计划”按钮。

## 📱 使用指南
主界面布局
    区域	      说明
顶部状态栏	  显示当前日期时间、PLC连接状态、工作状态。
核心数据看板	集中展示计划产量、累计产量、平均与实际UPH、完成率。
小时产量面板	实时显示“本小时产量”和“上小时产量”。
工作时间面板	显示配置的有效工作时间和理论平均UPH。
控制面板	    包含连接、开始/停止监控、刷新、设置等所有功能按钮。
状态日志	    显示详细的运行和通信日志，便于排查问题。
标准操作流程
启动应用：程序将自动尝试连接PLC。

验证连接：确认顶部状态栏显示“已连接”且无错误日志。

参数设置：根据当日排产，设置“工作时间”和“计划总产量”。

开始监控：点击“开始”按钮，系统将启动定时（默认3秒）读取PLC数据。

实时监控：观察各项生产指标随数据更新。

结束作业：当日生产结束后，点击“停止”按钮，然后关闭程序。

## 🔧 开发与扩展
项目结构概览
```bash
MideaProductionBoard
├── MainWindow.xaml          # 主窗口界面布局
├── MainWindow.xaml.cs       # 主窗口逻辑代码（核心文件）
├── WorkTimeSettings.xaml    # 工作时间设置对话框
├── WorkTimeInfo.cs          # 工作时间配置的数据模型
├── FontSizeConverter.cs     # 实现字体动态缩放的转换器
└── MideaProductionBoard.csproj # 项目配置文件
``` 
## PLC通信核心
程序通过 MitsubishiClient 类（来自IoTClient库）与PLC交互。主要流程封装在以下方法中：

InitializePlc() - 根据IP、端口和协议版本(MitsubishiVersion.Qna_3E)初始化客户端。

ConnectToPlc() - 调用 Open() 方法建立连接并验证。

ReadPlcData() - 定时读取 PLC_REGISTER 的32位整数值，并触发后续计算和UI更新。

## 为其他PLC型号适配
如果连接不同型号的PLC，可能需要在 InitializePlc() 中更改协议版本：

```bash 
plc = new MitsubishiClient(MitsubishiVersion.MC_3E, PLC_IP, PLC_PORT); // 尝试 MC_3E 协议
请参考IoTClient官方文档，选择适合你PLC的正确 MitsubishiVersion 枚举值。
```

## ❓ 常见问题 (FAQ)
1. **Q:** 程序启动后一直显示“未连接”，怎么办？  
**A:** 请按以下步骤排查：
检查 PLC_IP 和 PLC_PORT 配置是否正确。
在命令行使用 ping PLC_IP 测试网络连通性。
确认PLC已上电，且以太网配置（如MC协议使能）无误。
查看软件底部状态日志，是否有具体的错误信息（如“连接超时”、“拒绝访问”）。

2. **Q:** 能读到数据，但小时产量统计不准确？  
**A:** 程序在整点时刻会自动重置“本小时产量”。如果逻辑异常，请检查 ReadPlcData() 方法中基于 lastUpdateTime.Hour 的判断逻辑。

3. **Q:** 如何修改数据读取的频率？  
**A:** 在 InitializeTimers() 方法中，调整 dataTimer.Interval 的值，例如改为 TimeSpan.FromSeconds(5) 则为5秒读取一次。

4. **Q:** 我可以监控更多的PLC数据点吗？  
**A:** 可以。在 ReadPlcData() 方法中，仿照现有代码调用 plc.ReadInt32("D新的地址") 或 plc.ReadInt16(...)、plc.ReadBoolean(...) 等方法读取其他寄存器或线圈，并更新到界面。

## 🤝 如何贡献
欢迎任何形式的贡献！
报告问题：在 GitHub Issues 页面提交Bug或功能建议。
提交代码：Fork本仓库，创建功能分支，完成开发后提交 Pull Request。
改进文档：帮助完善此 README.md 或代码注释。
## 📄 许可证
本项目采用 MIT 许可证。这意味着您可以自由地使用、复制、修改、合并、发布、分发本软件。详情请参阅 LICENSE 文件。

## 📞 联系与支持
项目主页：https://github.com/tiannoyi/MideaProductionBoard
问题反馈：请优先使用 GitHub Issues 进行交流。
邮件联系：tiannoyi@qq.com (如有敏感信息)
💡 提示：在生产环境首次部署前，强烈建议在测试环境中充分验证所有功能及PLC通信的稳定性。
