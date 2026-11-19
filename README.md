<h1 align="center">牛马计数器</h1>
<p align="center">一个原生 macOS 工资计数器。看看今天赚了多少，还要多久下班。</p>
<p align="center"><a href="https://github.com/QiYi92/off-duty-counter/releases/tag/v1.1">下载 v1.1</a> · <a href="https://github.com/QiYi92/off-duty-counter/issues">报告问题</a> · macOS 13+ / Windows 10–11</p>

<p align="center">
  <img src="docs/screenshots/main-window.png" alt="牛马计数器主窗口，展示工资、工时进度、今日和本月收获" width="380">
</p>

## 下载与安装（macOS）

1. 在 [v1.1 发布页](https://github.com/QiYi92/off-duty-counter/releases/tag/v1.1) 下载 macOS ZIP。
2. 解压，把 `牛马计数器.app` 拖到「应用程序」。
3. 首次打开后，填写月薪、工作时段和休息日。还可以设置每月发薪日。

安装包支持 Apple 芯片和 Intel Mac。此版本未经过 Apple 公证，如果 macOS 提示无法验证开发者，可在确认下载自本项目后，从「系统设置 → 隐私与安全性」选择「仍要打开」。

## 功能

- **工资实时计数**：按排班每秒估算今日已赚金额，显示当前赚钱速度、剩余金额和上下班倒计时。午休和下班停止累加。
- **自定义排班**：设置每天两个工作时段、单休或双休、每周休息日，以及按日历或固定天数计薪。
- **今日与本月收获**：把今日收入换算成可乐、外卖、火锅等物品，查看本月累计和发薪倒计时。
- **适合常驻**：窗口可以置顶、折叠或收进菜单栏，也可从 Dock 打开。内置 11 种主题。
- **摸鱼语录**：点击换一句，展开窗口时每五分钟自动轮换。
- **本地运行**：薪资和排班只保存在这台 Mac。金额是按排班推算的参考值。

## 从源码运行

需要 macOS 13 或更新版本，以及 Swift 5.9 或更新版本。

```bash
git clone https://github.com/QiYi92/off-duty-counter.git
cd off-duty-counter
swift run Counter
```

也可在 VS Code 打开项目，安装推荐的 CodeLLDB 扩展，选择「运行牛马计数器」后按 **F5**。运行 `./scripts/package.sh` 可生成通用 ZIP 和 SHA-256 校验文件。

## 摸鱼语录来源

- [新京报：早安，打工人](https://m.bjnews.com.cn/detail/160376739315502.html)
- [澎湃新闻：除了打工却不读书，你可能是假的打工人](https://m.thepaper.cn/newsDetail_forward_9710622)
- [新华网：上班摸鱼](https://www.xinhuanet.com/politics/2021-07/29/c_1127706637.htm)
- [澎湃号：打工人浪潮下的大学生独白](https://www.thepaper.cn/newsDetail_forward_10272303)
- [爱范儿：摸鱼学导论](https://www.ifanr.com/1405965)
- [澎湃号：摸鱼摸得好，也能反过来剥削老板](https://www.thepaper.cn/newsDetail_forward_12542753)
- [查新词：带薪摸鱼](https://chaxinci.com/ci/daixinmoyu)

## Windows 便携版

从 [v1.1 发布页](https://github.com/QiYi92/off-duty-counter/releases/tag/v1.1) 下载对应的便携 EXE。Windows 10/11 64 位版本提供独立 EXE。Intel 或 AMD 电脑使用 x64，Windows on Arm 使用 ARM64。双击下载的 EXE 即可打开，无需安装 .NET 或获取管理员权限。首次运行时填写月薪和排班。

点击「收进托盘」、最小化或关闭窗口，应用会留在任务栏右下角的通知区域。左键点击秒表图标恢复窗口，右键可以打开设置或退出。如果没看到图标，点通知区域的上箭头。设置保存在当前 Windows 用户的本地应用数据目录。

使用 .NET 10 SDK 运行 `./windows/publish.sh`，会在 `.build/windows/win-x64/` 和 `.build/windows/win-arm64/` 分别生成自包含的 EXE。
