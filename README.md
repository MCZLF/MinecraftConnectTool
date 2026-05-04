<div align="center">

# 🎮 Minecraft Connect Tool

**让 Minecraft 联机变得简单** · **跨平台** · **开源免费**

[![Version](https://img.shields.io/badge/version-0.0.7-blue?style=flat-square)](https://github.com/MCZLF/MinecraftConnectTool)
[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?style=flat-square&logo=dotnet)](https://dotnet.microsoft.com/)
[![Avalonia](https://img.shields.io/badge/Avalonia-UI-8B00FF?style=flat-square)](https://avaloniaui.net/)
[![License](https://img.shields.io/badge/license-MIT-green?style=flat-square)](LICENSE)

<img src="https://raw.githubusercontent.com/MCZLF/MinecraftConnectTool/0.0.7/docs/screenshots/preview.png" width="800" alt="MCT Preview">

</div>

---

## ✨ 功能特性

| 功能 | 描述 | 状态 |
|------|------|------|
| 🏠 **首页** | 快速开始向导，一键创建/加入房间 | ✅ |
| 🔗 **联机大厅** | 浏览公开房间，快速加入游戏 | ✅ |
| ⚡ **ET 联机** | EasyTier 内网穿透，无需公网 IP | ✅ |
| 🌐 **P2P 联机** | 点对点直连，低延迟高稳定 | ✅ |
| 🚀 **游戏优化** | 一键优化 Minecraft 性能设置 | ✅ |
| ⚙️ **设置中心** | 个性化配置，主题切换 | ✅ |
| 🆘 **帮助文档** | 详细的使用教程与 FAQ | ✅ |

---

## 🖼️ 界面预览

<div align="center">

### 🏠 首页
<img src="https://raw.githubusercontent.com/MCZLF/MinecraftConnectTool/0.0.7/docs/screenshots/home.png" width="700">

### 🔗 联机大厅
<img src="https://raw.githubusercontent.com/MCZLF/MinecraftConnectTool/0.0.7/docs/screenshots/link.png" width="700">

### ⚡ ET 联机
<img src="https://raw.githubusercontent.com/MCZLF/MinecraftConnectTool/0.0.7/docs/screenshots/et.png" width="700">

### ⚙️ 设置
<img src="https://raw.githubusercontent.com/MCZLF/MinecraftConnectTool/0.0.7/docs/screenshots/settings.png" width="700">

</div>

---

## 🚀 快速开始

### 下载安装

| 平台 | 下载 | 说明 |
|------|------|------|
| 🪟 Windows | [MCT-Windows-x64.exe](https://github.com/MCZLF/MinecraftConnectTool/releases) | Windows 10/11 |
| 🐧 Linux | [MCT-Linux-x64](https://github.com/MCZLF/MinecraftConnectTool/releases) | 主流发行版 |
| 🍎 macOS | [MCT-macOS](https://github.com/MCZLF/MinecraftConnectTool/releases) | Intel/Apple Silicon |

### 使用步骤

```
1️⃣ 下载对应平台的安装包
2️⃣ 运行程序，完成首次启动向导
3️⃣ 选择联机方式（ET / P2P / 本地）
4️⃣ 创建房间或输入邀请码加入
5️⃣ 启动 Minecraft，开始游戏！
```

---

## 🛠️ 技术栈

```
┌─────────────────────────────────────┐
│           Minecraft Connect Tool     │
├─────────────────────────────────────┤
│  🎨 UI Framework: Avalonia UI        │
│  ⚙️ Runtime: .NET 8.0                │
│  🔧 Language: C# 12                  │
│  📦 Package: CommunityToolkit.MVVM   │
│  🌐 Network: EasyTier / P2P          │
└─────────────────────────────────────┘
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

# 发布（Linux）
dotnet publish -c Release -r linux-x64 --self-contained false /p:PublishSingleFile=true

# 发布（macOS）
dotnet publish -c Release -r osx-x64 --self-contained false /p:PublishSingleFile=true
dotnet publish -c Release -r osx-arm64 --self-contained false /p:PublishSingleFile=true
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

## 🤝 参与贡献

我们欢迎所有形式的贡献！

```
🐛 提交 Issue - 报告 Bug 或提出新功能建议
🔀 提交 PR - 修复问题或添加新功能
⭐ Star 项目 - 给项目点个星星支持我们
📢 分享推广 - 告诉你的朋友这个项目
```

### 贡献流程

1. Fork 本仓库
2. 创建特性分支 (`git checkout -b feature/AmazingFeature`)
3. 提交更改 (`git commit -m 'Add some AmazingFeature'`)
4. 推送分支 (`git push origin feature/AmazingFeature`)
5. 创建 Pull Request

---

## 📜 开源协议

本项目基于 [MIT](LICENSE) 协议开源。

```
MIT License

Copyright (c) 2024 MCZLF Studio

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.
```

---

## 💖 致谢

感谢以下开源项目让 MCT 成为可能：

- [Avalonia UI](https://avaloniaui.net/) - 跨平台 UI 框架
- [EasyTier](https://github.com/EasyTier/EasyTier) - 内网穿透工具
- [CommunityToolkit.MVVM](https://learn.microsoft.com/dotnet/communitytoolkit/mvvm/) - MVVM 工具包

---

<div align="center">

**Made with 💚 by MCZLF Studio**

[🌟 Star](https://github.com/MCZLF/MinecraftConnectTool) · [🐛 Issues](https://github.com/MCZLF/MinecraftConnectTool/issues) · [📖 Wiki](https://github.com/MCZLF/MinecraftConnectTool/wiki)

</div>
