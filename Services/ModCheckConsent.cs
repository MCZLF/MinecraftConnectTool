using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using MinecraftConnectTool.ExtensionUI;

namespace MinecraftConnectTool.Services;

internal static class ModCheckConsent
{
    public const string Title = "允许上传模组列表用于比对客户端差异";
    public const string Purpose = "开启后，加入房间时将会自动上传模组列表，用于比对联机加入方客户端模组及版本是否相同。";

    private const string ConfigKey = "AllowModCheckUpload";
    private const string LegacyConfigKey = "ModCheckUploadConsentVersion";
    private const int LegacyConsentVersion = 4;
    private static readonly object Gate = new();
    private static CancellationTokenSource _revocation = new();

    public static bool IsAllowed => ReadDecision() == true;

    private static bool? ReadDecision()
    {
        return ConfigService.Read<JsonElement>(ConfigKey).ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            _ when !ConfigService.Exists(ConfigKey) => ConfigService.Read(LegacyConfigKey, -1) switch
            {
                LegacyConsentVersion => true,
                0 => false,
                _ => null
            },
            _ => null
        };
    }

    public static async Task PromptIfUnconfiguredAsync(Window owner)
    {
        if (ReadDecision() is not { } decision)
            await RequestAsync(owner);
        else if (!ConfigService.Exists(ConfigKey))
            ConfigService.Write(ConfigKey, decision);
    }

    public static string GetDisclosure()
    {
        return "由于涉及到非工作目录扫描及上传，需要您手动确认。\n\n"
            + "【自动扫描范围】\n"
            + "• P2P、ET 和 Link 开房或加入时识别运行中的 Minecraft。开房时也能识别单独运行的服务端。\n"
            + "• 读取该实例的启动参数、相关版本元数据，以及 mods 目录顶层的 .jar 文件。\n\n"
            + "【上传内容】\n"
            + "• 模组文件名和完整 MD5，以及可识别的 Minecraft、模组加载器、FML、MCP、NeoForm 和 Java 版本。\n"
            + "• 服务器通过房间提示码的哈希关联清单，并能看到您的连接 IP。\n"
            + "• 游戏账号凭据、JAR 本体、本机目录路径和完整进程参数都不会上传。\n\n"
            + "【比对和查看】\n"
            + "• 服务器完成比对。持有房间提示码的加入方可看到包含房主模组文件名、MD5 和版本信息的比对结果。\n\n"
            + "【保存与删除】\n"
            + "• 加入方清单仅用于本次请求。房主清单仅在服务端内存中暂存。\n"
            + "• P2P 和 Link 清单随房间释放。ET 清单在停止续传 1 小时后释放。\n"
            + "• 房间关闭或撤销授权时，客户端会尝试删除房主清单。关闭此设置后，客户端不会发起新的上传。";
    }

    public static async Task RequestAsync(Window owner)
    {
        var allowed = await MD3MessageDialog.ConfirmAsync(owner, GetDisclosure(), Title,
            "同意并开启", expanded: true, cancelText: "不同意", isCloseButtonEnabled: false,
            emphasisText: Purpose);
        SetDecision(allowed);
    }

    public static void SetDecision(bool allowed)
    {
        if (allowed)
            ConfigService.Write(ConfigKey, true);
        else
            Revoke();
    }

    public static bool TryGetAuthorization(out CancellationToken revocation)
    {
        lock (Gate)
        {
            revocation = _revocation.Token;
            return IsAllowed;
        }
    }

    public static void Revoke()
    {
        CancellationTokenSource revoked;
        lock (Gate)
        {
            ConfigService.Write(ConfigKey, false);
            if (ConfigService.Read(LegacyConfigKey, -1) == LegacyConsentVersion)
                ConfigService.Write(LegacyConfigKey, 0);
            revoked = _revocation;
            _revocation = new CancellationTokenSource();
        }

        revoked.Cancel();
        revoked.Dispose();
    }
}
