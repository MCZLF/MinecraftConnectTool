using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MinecraftConnectTool.Services;

namespace MinecraftConnectTool.ViewModels.RightPage;

public partial class PanelInviteEditViewModel : ObservableObject
{
    #region 属性

    // 提示码更新频率选项
    public List<string> CodeUpdateOptions { get; } = new() { "每次", "每时", "每天", "永久" };

    [ObservableProperty]
    private int _selectedCodeUpdateIndex;

    [ObservableProperty]
    private string _customNode = "";

    [ObservableProperty]
    private bool _isCustomCodeInputVisible;

    [ObservableProperty]
    private bool _isSaveCodeButtonVisible;

    [ObservableProperty]
    private string _customCodeStatusText = "";

    [ObservableProperty]
    private bool _isCustomCodeStatusVisible;

    [ObservableProperty]
    private string _hostBadgeText = "默认";

    [ObservableProperty]
    private string _hostBadgeColor = "#808080"; // Gray

    [ObservableProperty]
    private bool _useCustomPort;

    [ObservableProperty]
    private string _customPort = "";

    [ObservableProperty]
    private bool _isSavePortButtonVisible;

    [ObservableProperty]
    private string _customPortStatusText = "";

    [ObservableProperty]
    private bool _isCustomPortStatusVisible;

    [ObservableProperty]
    private string _playerBadgeText = "默认";

    [ObservableProperty]
    private string _playerBadgeColor = "#808080"; // Gray

    // 帮助说明折叠状态
    [ObservableProperty]
    private bool _isHostHelpVisible;

    [ObservableProperty]
    private bool _isPlayerHelpVisible;

    // 原始值用于比较
    private string _originalCustomNode = "";
    private string _originalCustomPort = "";

    #endregion

    public PanelInviteEditViewModel()
    {
    }

    /// <summary>
    /// 加载设置
    /// </summary>
    public void LoadSettings()
    {
        // 加载提示码更新频率
        int codeUpdate = ConfigService.Read("codeupdate", 1);

        // 加载自定义提示码
        bool useCustomNode = ConfigService.Read("usecustomnode", false);
        string savedNode = ConfigService.Read("customnode", "");

        // 如果是永久模式(4)但提示码为空，自动切换回每次(1)
        if (codeUpdate == 4 && string.IsNullOrWhiteSpace(savedNode))
        {
            codeUpdate = 1;
            ConfigService.Write("codeupdate", codeUpdate);
            // 清除可能存在的空配置
            ConfigService.Delete("usecustomnode");
            ConfigService.Delete("customnode");
            useCustomNode = false;
            savedNode = "";
        }

        SelectedCodeUpdateIndex = codeUpdate - 1; // 1-based to 0-based

        if (useCustomNode)
        {
            // 显示时保留FO后缀
            // 先设置原始值，再设置当前值，避免触发保存按钮显示
            _originalCustomNode = savedNode;
            CustomNode = savedNode;
        }
        else
        {
            _originalCustomNode = "";
            CustomNode = "";
        }

        // 加载自定义端口
        UseCustomPort = ConfigService.Read("usecustomport", false);
        if (UseCustomPort)
        {
            string savedPort = ConfigService.Read("customport", "");
            // 先设置原始值，再设置当前值，避免触发保存按钮显示
            _originalCustomPort = savedPort;
            CustomPort = savedPort;
        }
        else
        {
            _originalCustomPort = "";
            CustomPort = "";
        }

        // 确保保存按钮初始状态为隐藏
        IsSaveCodeButtonVisible = false;
        IsSavePortButtonVisible = false;

        UpdateHostBadge();
        UpdatePlayerBadge();
    }

    #region 提示码相关

    partial void OnSelectedCodeUpdateIndexChanged(int value)
    {
        int codeUpdate = value + 1; // 0-based to 1-based
        ConfigService.Write("codeupdate", codeUpdate);

        // 显示/隐藏自定义提示码输入框
        IsCustomCodeInputVisible = codeUpdate == 4; // 4 = 永久

        // 如果不是永久模式，清除自定义提示码配置
        if (codeUpdate != 4)
        {
            ConfigService.Delete("usecustomnode");
            ConfigService.Delete("customnode");
            CustomNode = "";
            _originalCustomNode = "";
        }

        UpdateHostBadge();
    }

    partial void OnCustomNodeChanged(string value)
    {
        // 检查是否与原始值不同
        IsSaveCodeButtonVisible = value != _originalCustomNode && !string.IsNullOrEmpty(value);
        IsCustomCodeStatusVisible = false;
    }

    [RelayCommand]
    private async Task SaveCustomCodeAsync()
    {
        IsCustomCodeStatusVisible = false;

        // 验证输入格式
        if (!Regex.IsMatch(CustomNode, @"^[\u4e00-\u9fa5a-zA-Z0-9\-]+$"))
        {
            CustomCodeStatusText = "提示码仅可输入汉字、数字、字母或-连字符";
            IsCustomCodeStatusVisible = true;
            return;
        }

        // 验证长度
        if (CustomNode.Length < 8)
        {
            CustomCodeStatusText = "提示码过短，需要至少8位";
            IsCustomCodeStatusVisible = true;
            return;
        }

        // 敏感词检测
        bool profanityCheck = await CheckProfanityAsync(CustomNode);
        if (!profanityCheck)
        {
            return; // 检测失败或包含敏感词，错误信息已在方法中设置
        }

        // 保存配置（添加FO后缀）
        ConfigService.Write("usecustomnode", true);
        ConfigService.Write("customnode", CustomNode + "FO");

        _originalCustomNode = CustomNode;
        IsSaveCodeButtonVisible = false;
        UpdateHostBadge();
    }

    private async Task<bool> CheckProfanityAsync(string text)
    {
        try
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
            var content = new StringContent(
                JsonSerializer.Serialize(new { text }),
                System.Text.Encoding.UTF8,
                "application/json");

            var response = await client.PostAsync(
                "https://uapis.cn/api/v1/text/profanitycheck",
                content);

            response.EnsureSuccessStatusCode();
            var jsonString = await response.Content.ReadAsStringAsync();
            var result = JsonNode.Parse(jsonString);

            string status = result?["status"]?.GetValue<string>() ?? "";
            if (status == "forbidden")
            {
                var forbiddenWords = result?["forbidden_words"]?.AsArray();
                string words = forbiddenWords != null && forbiddenWords.Count > 0
                    ? string.Join(" | ", forbiddenWords.Select(w => w?.ToString()?.Trim() ?? "").Where(s => !string.IsNullOrEmpty(s)))
                    : "未知敏感词";

                CustomCodeStatusText = $"包含敏感词：{words}";
                IsCustomCodeStatusVisible = true;

                // 注意：这里不清除配置，因为用户可能只是修改时触发了检测
                // 配置只有在成功保存后才会更新
                return false;
            }

            return true;
        }
        catch (HttpRequestException ex)
        {
            CustomCodeStatusText = $"网络请求失败: {ex.Message}";
            IsCustomCodeStatusVisible = true;
            return false;
        }
        catch (Exception ex)
        {
            CustomCodeStatusText = $"检测失败: {ex.Message}";
            IsCustomCodeStatusVisible = true;
            return false;
        }
    }

    private void UpdateHostBadge()
    {
        int codeUpdate = SelectedCodeUpdateIndex + 1;
        switch (codeUpdate)
        {
            case 1:
                HostBadgeText = "每次";
                HostBadgeColor = "#4CAF50"; // Green
                break;
            case 2:
                HostBadgeText = "每时";
                HostBadgeColor = "#4CAF50"; // Green
                break;
            case 3:
                HostBadgeText = "每天";
                HostBadgeColor = "#4CAF50"; // Green
                break;
            case 4:
                bool useCustom = ConfigService.Read("usecustomnode", false);
                if (useCustom)
                {
                    HostBadgeText = "永久+自定义";
                    HostBadgeColor = "#2196F3"; // Blue
                }
                else
                {
                    HostBadgeText = "永久";
                    HostBadgeColor = "#FF9800"; // Orange
                }
                break;
        }
    }

    #endregion

    #region 端口相关

    partial void OnUseCustomPortChanged(bool value)
    {
        if (!value)
        {
            // 关闭开关时清除配置
            ConfigService.Delete("usecustomport");
            ConfigService.Delete("customport");
            CustomPort = "";
            _originalCustomPort = "";
            IsSavePortButtonVisible = false;
        }
        UpdatePlayerBadge();
    }

    partial void OnCustomPortChanged(string value)
    {
        // 检查是否与原始值不同
        IsSavePortButtonVisible = value != _originalCustomPort && !string.IsNullOrEmpty(value);
        IsCustomPortStatusVisible = false;
    }

    [RelayCommand]
    private void SaveCustomPort()
    {
        IsCustomPortStatusVisible = false;

        // 验证端口
        if (!int.TryParse(CustomPort, out int port) || port < 1 || port > 65535)
        {
            CustomPortStatusText = "请输入有效的端口(1-65535)";
            IsCustomPortStatusVisible = true;
            ConfigService.Write("usecustomport", false);
            return;
        }

        // 保存配置
        ConfigService.Write("usecustomport", true);
        ConfigService.Write("customport", CustomPort);

        _originalCustomPort = CustomPort;
        IsSavePortButtonVisible = false;
        UpdatePlayerBadge();
    }

    private void UpdatePlayerBadge()
    {
        if (UseCustomPort)
        {
            PlayerBadgeText = "固定";
            PlayerBadgeColor = "#2196F3"; // Blue
        }
        else
        {
            PlayerBadgeText = "默认";
            PlayerBadgeColor = "#808080"; // Gray
        }
    }

    #endregion

    #region 帮助命令

    [RelayCommand]
    private void ToggleHostHelp()
    {
        // 切换房主帮助说明的显示状态
        IsHostHelpVisible = !IsHostHelpVisible;
    }

    [RelayCommand]
    private void TogglePlayerHelp()
    {
        // 切换玩家帮助说明的显示状态
        IsPlayerHelpVisible = !IsPlayerHelpVisible;
    }

    #endregion
}
