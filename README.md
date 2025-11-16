# MinecraftConnectTool

基于OpenP2P,轻量便携简单易用的
高速不限速的MinecraftConnectTool联机工具
> [!tip]
> 加入 **MinecraftConnectTool** 官方交流群，获取联机教程、NAT 打洞技巧与即时帮助：  
> - 一群 **690625244**（答案：联机）  
> - 二群 **940606962**（答案：联机）  
> - 三群 **475264978**（答案：联机）  
> 任选一群加入，下载更新、插件配置、故障排查，一站搞定，欢迎来聊！

---

## 系统需求
- Windows 7 SP1 及以上 x64 / x86  
- **.NET Framework 4.7.2**（Win10 1803+ 已内置；低版本系统首次启动会弹微软官方安装向导，按提示一键装完即可）  
- 无需 Java、无需额外运行时、无网络端口手动放行要求

---

## 下载 & 运行
1. [Release](https://github.com/MCZLF/MinecraftConnectTool/releases) 页面下载唯一文件 `MCT.exe`（单文件，&lt; 5 MB）。  
2. 双击启动：  
   - 若系统缺少 .NET 4.7.2，会自动跳转微软官方安装页，装完重启工具即可。  
   - 首次启动会弹出 **WDF（Windows Defender 防火墙）授权** 提示，**建议把“专用 / 公用”两个框都勾上**，否则可能打洞失败。  
> [!CAUTION]
> 鉴于 PCL、PCLCE、HMCL 均已接入 EasyTier 联机，MinecraftConnectTool 在近版本（0.0.5.200+、0.0.6.091+）加入 **Probe 探针**：启动时会匿名上报当前版本号至 ProbeServer，用于统计真实用户存量并合理调配中继资源。  
> 数据仅含版本字符串，不含任何个人标识或存档信息。  
> 可在「设置 → 高级设置 → 允许 Probe 探针」关闭，**默认开启，不建议关闭**。关闭后将无法参与真实负载评估。
---

## 使用流程（主机 & 加入方相同界面）
#### [Bilibili](https://www.bilibili.com/video/BV1sBXyYgE1j) | [图文教程](http://mcjavao.tttttttttt.top) | [官方知识库](https://mct.mczlf.loft.games)
| 主机（开房间） | 加入方（进房间） |
|---|---|
| ① 点「开启联机房间」→ 复制提示码 | ① 输入主机提示码与端口 |
| ② 进存档 → ESC → 对局域网开放 → 记下端口 | ② 点「加入联机房间」→ 获得本地地址 `127.0.0.1:xxxxx` |
| ③ 把「提示码:端口」发给好友 | ③ MC 多人游戏直连该地址，即可联机 |
---

## 常见问题
| 问题 | 一句话解决 |
|---|---|
| 双击没反应 | 先装 .NET Framework 4.7.2，再右键「以管理员身份运行」 |
| 防火墙提示 | 看见 WDF 弹窗全点「允许」；若错过，手动把 `MCT.exe` 加出入站白名单 |
| 一直“打洞中” | 双方重启工具 → 换 4G 热点 → 校园网/企业网可能被封 P2P，无解 |
| 游戏版本不同 | 保证 MC 版本、Mod 完全一致，否则连上也会秒掉 |

&gt; 更详细图文/视频/诊断脚本请直达官方知识库：  
&gt; [mct.mczlf.loft.games](https://mct.mczlf.loft.games)

---

## 已启用的MCTServer服务项
- LogServer
- CandyCreateService
- ProbeServer
- Frp(OnlyServerService)

## 开源协议
- 本项目：[MIT License](https://github.com/MCZLF/MinecraftConnectTool/blob/master/LICENSE)  
- 底层 P2P 组件 [OpenP2P](https://github.com/openp2p-cn/openp2p)  [MIT License](https://github.com/openp2p-cn/openp2p/blob/master/LICENSE)  

源码地址：  
[https://github.com/MCZLF/MinecraftConnectTool](https://github.com/MCZLF/MinecraftConnectTool)

---

## 致谢
NAT 穿透P2P联机功能由 OpenP2P 提供  
仓库地址：[https://github.com/openp2p-cn/openp2p](https://github.com/openp2p-cn/openp2p)  
特此注明并感谢原作者。

部分内容使用Ai生成(真的懒得写)
> [!warning]
> **致 MinecraftConnectTool 用户**  
> 项目 2023 年上线至今，始终免费、无商业化计划。开发与维护全靠业余时间与社区热情，对外曝光有限。  
> 若您认可本工具，请在合规渠道顺手转发（群聊、论坛、视频简介均可），帮助更多需要联机的玩家发现它。  
> 拒绝刷量与夸大宣传，真实分享即可，我们先行鞠躬感谢！  
> 
> 下载地址（长期有效）：  
> https://mct.mczlf.loft.games/function/download  
> 建议在简介备注：  
> “有问题带日志进 QQ 群 **690625244 / 940606962 / 475264978**（答案：联机），**勿在视频评论区提问**，以免遗漏。”