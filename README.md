<div align="center">

<img src="https://free.picui.cn/free/2026/05/04/69f829881dbab.ico" width="120" alt="MCT Icon">

# 🎮 Minecraft Connect Tool

**基于 OpenP2P，轻量便携简单易用的高速不限速联机工具**

[![Version](https://img.shields.io/badge/version-0.0.7-blue?style=flat-square)](https://github.com/MCZLF/MinecraftConnectTool)
[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?style=flat-square&logo=dotnet)](https://dotnet.microsoft.com/)
[![Avalonia](https://img.shields.io/badge/Avalonia-UI-8B00FF?style=flat-square)](https://avaloniaui.net/)
[![License](https://img.shields.io/badge/license-MIT-green?style=flat-square)](LICENSE)

</div>

---

## 🖼️ 界面展示

<div align="center">

### 🏠 主界面
<img src="https://free.picui.cn/free/2026/05/04/69f8288d99cdd.png" width="800" alt="主界面">

### 🔗 P2P 联机界面
<img src="https://free.picui.cn/free/2026/05/04/69f8289d6068e.png" width="800" alt="P2P界面">

### 🏢 Link & 房间管理
<img src="https://free.picui.cn/free/2026/05/04/69f82882d2085.png" width="800" alt="Link&房间管理">

### ⚙️ 设置
<img src="https://free.picui.cn/free/2026/05/04/69f8289cd7103.png" width="800" alt="设置">

### 🚀 网络优化
<img src="https://free.picui.cn/free/2026/05/04/69f828a55e5a3.png" width="800" alt="网络优化">

</div>

---

> [!TIP]
> 加入 **MinecraftConnectTool** 官方交流群，获取联机教程、NAT 打洞技巧与即时帮助：
> - 一群 **690625244**（答案：联机）
> - 二群 **940606962**（答案：联机）
> - 三群 **475264978**（答案：联机）
> 任选一群加入，下载更新、插件配置、故障排查，一站搞定，欢迎来聊！

---

## 📋 系统需求

- Windows 7 SP1 及以上 x64 / x86
- **.NET Framework 4.7.2**（Win10 1803+ 已内置；低版本系统首次启动会弹微软官方安装向导，按提示一键装完即可）
- 无需 Java、无需额外运行时、无网络端口手动放行要求

---

## 🚀 下载 & 运行

1. 访问 [`https://github.com/MCZLF/MinecraftConnectTool/releases`](https://github.com/MCZLF/MinecraftConnectTool/releases) 页面下载唯一文件 `MCT.exe`（单文件，< 5 MB）。
2. 双击启动：
   - 若系统缺少 .NET 4.7.2，会自动跳转微软官方安装页，装完重启工具即可。
   - 首次启动会弹出 **WDF（Windows Defender 防火墙）授权** 提示，**建议把"专用 / 公用"两个框都勾上**，否则可能打洞失败。

> [!CAUTION]
> 鉴于 PCL、PCLCE、HMCL 均已接入联机功能，MinecraftConnectTool 在近版本（0.0.5.200+、0.0.6.091+）加入 **Probe 探针**：启动时会匿名上报当前版本号至 ProbeServer，用于统计真实用户存量并合理调配中继资源。
> 数据仅含版本字符串，不含任何个人标识或存档信息。
> 可在「设置 → 高级设置 → 允许 Probe 探针」关闭，**默认开启，不建议关闭**。关闭后将无法参与真实负载评估。

---

## 📖 使用流程（主机 & 加入方相同界面）

📺 视频教程：[`https://www.bilibili.com/video/BV1sBXyYgE1j`](https://www.bilibili.com/video/BV1sBXyYgE1j)

🌐 官方网站：
- [`http://mcjavao.tttttttttt.top`](http://mcjavao.tttttttttt.top)
- [`https://mct.mczlf.loft.games`](https://mct.mczlf.loft.games)

| 主机（开房间） | 加入方（进房间） |
|:---:|:---:|
| ① 点「开启联机房间」→ 复制提示码 | ① 输入主机提示码与端口 |
| ② 进存档 → ESC → 对局域网开放 → 记下端口 | ② 点「加入联机房间」→ 获得本地地址 `127.0.0.1:xxxxx` |
| ③ 把「提示码:端口」发给好友 | ③ MC 多人游戏直连该地址，即可联机 |

---

## ❓ 常见问题

| 问题 | 一句话解决 |
|:---|:---|
| 双击没反应 | 先装 .NET Framework 4.7.2，再右键「以管理员身份运行」 |
| 防火墙提示 | 看见 WDF 弹窗全点「允许」；若错过，手动把 `MCT.exe` 加出入站白名单 |
| 一直"打洞中" | 双方重启工具 → 换 4G 热点 → 校园网/企业网可能被封 P2P，无解 |
| 游戏版本不同 | 保证 MC 版本、Mod 完全一致，否则连上也会秒掉 |

> 更详细图文/视频/诊断脚本请直达官方知识库：
> [`https://mct.mczlf.loft.games`](https://mct.mczlf.loft.games)

---

## 🖥️ 已启用的 MCTServer 服务项

| 服务 | 状态 | 说明 |
|:---|:---:|:---|
| LogServer | ✅ 运行中 | 日志收集服务 |
| ~~CandyCreateService~~ | ❌ 已停止 | Candy 联机模式已停止支持 |
| ProbeServer | ✅ 运行中 | 用户统计探针 |
| Frp (OnlyServerService) | ✅ 运行中 | Frp 中继服务 |

---

## 🛠️ 技术栈

```
┌─────────────────────────────────────────┐
│         Minecraft Connect Tool          │
├─────────────────────────────────────────┤
│  🎨 UI Framework: Avalonia UI           │
│  ⚙️ Runtime: .NET 8.0                   │
│  🔧 Language: C# 12                     │
│  📦 Package: CommunityToolkit.MVVM      │
│  🌐 Network: OpenP2P / MCILM-Link       │
└─────────────────────────────────────────┘
```

---

## 📦 构建项目

### 环境要求

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Visual Studio 2022](https://visualstudio.microsoft.com/) 或 [Rider](https://www.jetbrains.com/rider/)

### 克隆仓库

```bash
git clone -b 0.0.7 https://github.com/MCZLF/MinecraftConnectTool.git
cd MinecraftConnectTool
```

### 运行项目

```bash
# 还原依赖
dotnet restore

# 运行
dotnet run

# 发布（Windows）
dotnet publish -c Release -r win-x64 --self-contained false /p:PublishSingleFile=true
```

---

## 📁 项目结构

```
MinecraftConnectTool/
├── 📂 Assets/              # 图标、图片资源
├── 📂 Models/              # 数据模型
├── 📂 Services/            # 业务服务
├── 📂 ViewModels/          # MVVM 视图模型
│   ├── 📂 Pages/           # 页面视图模型
│   └── 📂 RightPage/       # 右侧面板视图模型
├── 📂 Views/               # Avalonia 视图
│   ├── 📂 Pages/           # 页面视图
│   ├── 📂 RightPage/       # 右侧面板视图
│   └── 📄 MainWindow.axaml # 主窗口
├── 📄 App.axaml            # 应用入口
└── 📄 README.md            # 本文件
```

---

## 📜 开源协议及第三方项目引用

| 项目 | 协议 | 地址 |
|:---|:---:|:---|
| 本项目 | MIT | [`LICENSE`](https://github.com/MCZLF/MinecraftConnectTool/blob/master/LICENSE) |
| OpenP2P | MIT | [`LICENSE`](https://github.com/openp2p-cn/openp2p/blob/master/LICENSE) |
| MCILM-Link | 闭源 | [`https://link.mcilm.top/`](https://link.mcilm.top/) |

源码地址：
[`https://github.com/MCZLF/MinecraftConnectTool`](https://github.com/MCZLF/MinecraftConnectTool)

---

## 🙏 致谢

### P2PMode - NAT 穿透 P2P 联机功能
由 **OpenP2P** 提供
- 仓库地址：[`https://github.com/openp2p-cn/openp2p`](https://github.com/openp2p-cn/openp2p)
- 本项目使用 OpenP2P 作为主要联机方案，特此注明并感谢原作者。

### LinkMode - Frp/P2P 混合联机功能
由 **MCILM-Link** 提供
- 官网地址：[`https://link.mcilm.top/`](https://link.mcilm.top/)
- 本项目使用 MCILM-Link 作为备用联机方案之一，特此注明并感谢原作者。

> 部分内容使用 AI 生成（真的懒得写）

---

> [!WARNING]
> **致 MinecraftConnectTool 用户**
> 
> 项目 2023 年上线至今，始终免费、无商业化计划。开发与维护全靠业余时间与社区热情，对外曝光有限。
> 若您认可本工具，请在合规渠道顺手转发（群聊、论坛、视频简介均可），帮助更多需要联机的玩家发现它。
> 拒绝刷量与夸大宣传，真实分享即可，我们先行鞠躬感谢！
>
> **下载地址（长期有效）：**
> [`https://mct.mczlf.loft.games/function/download`](https://mct.mczlf.loft.games/function/download)
>
> 建议在简介备注：
> "有问题带日志进 QQ 群 **690625244 / 940606962 / 475264978**（答案：联机），**勿在视频评论区提问**，以免遗漏。"

---

<div align="center">

**Made with 💚 by MCZLF Studio**

[🌟 Star](https://github.com/MCZLF/MinecraftConnectTool) · [🐛 Issues](https://github.com/MCZLF/MinecraftConnectTool/issues) · [📖 Wiki](https://github.com/MCZLF/MinecraftConnectTool/wiki)

</div>
