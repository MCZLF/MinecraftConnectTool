using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace MinecraftConnectTool.ViewModels.RightPage;

public partial class PanelAiAnalyzeViewModel : ObservableObject
{
    private readonly string _logContent;
    private readonly string _serverUrl;
    private readonly int _timeoutSeconds;
    private readonly Stopwatch _stopwatch = new();

    [ObservableProperty]
    private string _result = string.Empty;

    [ObservableProperty]
    private string _reasoningContent = string.Empty;

    [ObservableProperty]
    private bool _isAnalyzing = false;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private string _pageName = string.Empty;

    [ObservableProperty]
    private int _inputTokens;

    [ObservableProperty]
    private int _outputTokens;

    [ObservableProperty]
    private int _totalTokens;

    [ObservableProperty]
    private string _modelUsed = string.Empty;

    [ObservableProperty]
    private long _elapsedMs;

    [ObservableProperty]
    private string _elapsedText = string.Empty;

    [ObservableProperty]
    private bool _isReasoningExpanded = false;

    [ObservableProperty]
    private ObservableCollection<AiSection> _sections = new();

    public bool HasTokenInfo => InputTokens > 0 || OutputTokens > 0;

    public bool HasReasoning => !string.IsNullOrWhiteSpace(ReasoningContent);

    partial void OnInputTokensChanged(int value) => OnPropertyChanged(nameof(HasTokenInfo));
    partial void OnOutputTokensChanged(int value) => OnPropertyChanged(nameof(HasTokenInfo));
    partial void OnTotalTokensChanged(int value) => OnPropertyChanged(nameof(HasTokenInfo));
    partial void OnReasoningContentChanged(string value) => OnPropertyChanged(nameof(HasReasoning));

    public event EventHandler? CloseRequested;

    public PanelAiAnalyzeViewModel(string logContent, string serverUrl, string pageName, int timeoutSeconds = 120)
    {
        _logContent = logContent;
        _serverUrl = serverUrl;
        _pageName = pageName;
        _timeoutSeconds = timeoutSeconds;

        // 自动开始分析
        _ = AnalyzeAsync();
    }

    [RelayCommand]
    private async Task AnalyzeAsync()
    {
        if (IsAnalyzing) return;

        IsAnalyzing = true;
        ErrorMessage = null;
        Result = string.Empty;
        ReasoningContent = string.Empty;
        InputTokens = 0;
        OutputTokens = 0;
        TotalTokens = 0;
        ModelUsed = string.Empty;
        ElapsedMs = 0;
        ElapsedText = string.Empty;
        Sections.Clear();
        IsReasoningExpanded = false;

        _stopwatch.Restart();

        try
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(_timeoutSeconds) };
            var requestBody = new
            {
                logContent = _logContent
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await client.PostAsync($"{_serverUrl}/api/analyze/log", content);
            _stopwatch.Stop();
            ElapsedMs = _stopwatch.ElapsedMilliseconds;
            ElapsedText = FormatElapsed(ElapsedMs);

            var respJson = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                ErrorMessage = $"请求失败: {response.StatusCode}\n{respJson}";
                return;
            }

            using var doc = JsonDocument.Parse(respJson);
            var root = doc.RootElement;

            if (!root.GetProperty("success").GetBoolean())
            {
                ErrorMessage = root.GetProperty("message").GetString() ?? "分析失败";
                return;
            }

            var data = root.GetProperty("data");
            Result = data.GetProperty("result").GetString() ?? string.Empty;
            ModelUsed = data.GetProperty("model").GetString() ?? string.Empty;

            if (data.TryGetProperty("reasoningContent", out var reasoning))
            {
                ReasoningContent = reasoning.GetString() ?? string.Empty;
            }

            if (data.TryGetProperty("usage", out var usage))
            {
                if (usage.TryGetProperty("prompt_tokens", out var pt))
                    InputTokens = pt.GetInt32();
                if (usage.TryGetProperty("completion_tokens", out var ct))
                    OutputTokens = ct.GetInt32();
                if (usage.TryGetProperty("total_tokens", out var tt))
                    TotalTokens = tt.GetInt32();
            }

            // 解析分区
            ParseSections(Result);
        }
        catch (TaskCanceledException)
        {
            _stopwatch.Stop();
            ElapsedMs = _stopwatch.ElapsedMilliseconds;
            ErrorMessage = "请求超时，请检查 AiAnalyze 服务端是否运行";
        }
        catch (Exception ex)
        {
            _stopwatch.Stop();
            ElapsedMs = _stopwatch.ElapsedMilliseconds;
            ErrorMessage = $"分析异常: {ex.Message}";
        }
        finally
        {
            IsAnalyzing = false;
        }
    }

    private void ParseSections(string text)
    {
        Sections.Clear();
        if (string.IsNullOrWhiteSpace(text))
            return;

        var lines = text.Split('\n');
        string? currentTitle = null;
        var currentContent = new System.Text.StringBuilder();

        foreach (var rawLine in lines)
        {
            var line = rawLine.TrimEnd();
            if (line.StartsWith("## "))
            {
                if (currentTitle != null)
                {
                    var section = new AiSection { Title = currentTitle };
                    section.Blocks = ParseBlocks(currentContent.ToString().Trim());
                    Sections.Add(section);
                }
                currentTitle = line.Substring(3).Trim();
                currentContent.Clear();
            }
            else
            {
                currentContent.AppendLine(line);
            }
        }

        if (currentTitle != null)
        {
            var section = new AiSection { Title = currentTitle };
            section.Blocks = ParseBlocks(currentContent.ToString().Trim());
            Sections.Add(section);
        }

        if (Sections.Count == 0)
        {
            var section = new AiSection { Title = "分析结果" };
            section.Blocks = ParseBlocks(text.Trim());
            Sections.Add(section);
        }
    }

    private static ObservableCollection<ContentBlock> ParseBlocks(string text)
    {
        var blocks = new ObservableCollection<ContentBlock>();
        if (string.IsNullOrWhiteSpace(text)) return blocks;

        var lines = text.Split('\n');
        var textBuffer = new System.Text.StringBuilder();
        var listBuffer = new List<string>();
        bool isOrderedList = false;
        var tableBuffer = new List<string>();

        void FlushText()
        {
            if (textBuffer.Length > 0)
            {
                blocks.Add(new ContentBlock
                {
                    Type = BlockType.Text,
                    Text = textBuffer.ToString().TrimEnd()
                });
                textBuffer.Clear();
            }
        }

        void FlushList()
        {
            if (listBuffer.Count == 0) return;
            var prefixed = new ObservableCollection<string>();
            for (int i = 0; i < listBuffer.Count; i++)
            {
                if (isOrderedList)
                    prefixed.Add($"{i + 1}. {listBuffer[i]}");
                else
                    prefixed.Add($"• {listBuffer[i]}");
            }
            blocks.Add(new ContentBlock
            {
                Type = BlockType.List,
                ListItems = prefixed,
                IsOrderedList = isOrderedList
            });
            listBuffer.Clear();
        }

        void FlushTable()
        {
            if (tableBuffer.Count == 0) return;
            var tableBlock = new ContentBlock { Type = BlockType.Table };

            int sepIndex = -1;
            for (int i = 0; i < tableBuffer.Count; i++)
            {
                if (IsSeparatorLine(tableBuffer[i]))
                {
                    sepIndex = i;
                    break;
                }
            }

            var headerLines = sepIndex >= 0
                ? tableBuffer.Take(sepIndex).ToList()
                : new List<string>();
            var dataLines = sepIndex >= 0
                ? tableBuffer.Skip(sepIndex + 1).ToList()
                : tableBuffer.ToList();

            foreach (var line in headerLines)
                tableBlock.TableLines.Add(new TableLine { Cells = ParseTableCells(line) });
            foreach (var line in dataLines)
                tableBlock.TableLines.Add(new TableLine { Cells = ParseTableCells(line) });

            blocks.Add(tableBlock);
            tableBuffer.Clear();
        }

        foreach (var rawLine in lines)
        {
            var line = rawLine.TrimEnd();
            var trimmed = line.TrimStart();

            if (trimmed.StartsWith("|"))
            {
                FlushText();
                FlushList();
                tableBuffer.Add(line);
                continue;
            }

            if (TryParseListItem(trimmed, out var listText, out bool ordered))
            {
                FlushText();
                FlushTable();
                if (listBuffer.Count > 0 && isOrderedList != ordered)
                {
                    FlushList();
                }
                isOrderedList = ordered;
                listBuffer.Add(listText);
                continue;
            }

            FlushTable();
            FlushList();
            textBuffer.AppendLine(line);
        }

        FlushText();
        FlushList();
        FlushTable();

        return blocks;
    }

    private static bool TryParseListItem(string line, out string text, out bool ordered)
    {
        text = line;
        ordered = false;

        var orderedMatch = System.Text.RegularExpressions.Regex.Match(line, @"^(\d+)\.\s+(.*)$");
        if (orderedMatch.Success)
        {
            text = orderedMatch.Groups[2].Value;
            ordered = true;
            return true;
        }

        var unorderedMatch = System.Text.RegularExpressions.Regex.Match(line, @"^[-*]\s+(.*)$");
        if (unorderedMatch.Success)
        {
            text = unorderedMatch.Groups[1].Value;
            ordered = false;
            return true;
        }

        return false;
    }

    private static bool IsSeparatorLine(string line)
    {
        var trimmed = line.Trim();
        if (!trimmed.StartsWith("|") || !trimmed.EndsWith("|")) return false;
        var inner = trimmed.Trim('|').Trim();
        if (string.IsNullOrEmpty(inner)) return false;
        return inner.All(c => c == '-' || c == ':' || c == '|' || char.IsWhiteSpace(c));
    }

    private static List<string> ParseTableCells(string line)
    {
        var trimmed = line.Trim();
        if (trimmed.StartsWith("|")) trimmed = trimmed.Substring(1);
        if (trimmed.EndsWith("|")) trimmed = trimmed.Substring(0, trimmed.Length - 1);
        return trimmed.Split('|').Select(c => c.Trim()).ToList();
    }

    [RelayCommand]
    private async Task CopyAsync()
    {
        var textToCopy = Result;
        if (string.IsNullOrWhiteSpace(textToCopy))
            return;

        try
        {
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
                && desktop.MainWindow?.Clipboard is { } clipboard)
            {
                await clipboard.SetTextAsync(textToCopy);
            }
        }
        catch
        {
            // 忽略复制失败
        }
    }

    [RelayCommand]
    private void Close()
    {
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }

    private static string FormatElapsed(long ms)
    {
        if (ms < 1000) return $"{ms}ms";
        return $"{ms / 1000.0:F1}s";
    }
}

public class AiSection
{
    public string Title { get; set; } = string.Empty;
    public ObservableCollection<ContentBlock> Blocks { get; set; } = new();
}

public class ContentBlock
{
    public BlockType Type { get; set; }
    public string Text { get; set; } = string.Empty;
    public ObservableCollection<string> ListItems { get; set; } = new();
    public bool IsOrderedList { get; set; }
    public ObservableCollection<TableLine> TableLines { get; set; } = new();
    public int ColumnCount => TableLines.Count > 0 ? TableLines[0].Cells.Count : 0;
    public bool IsText => Type == BlockType.Text;
    public bool IsList => Type == BlockType.List;
    public bool IsTable => Type == BlockType.Table;
}

public class TableLine
{
    public List<string> Cells { get; set; } = new();
    public int ColumnCount => Cells.Count;
}

public enum BlockType
{
    Text,
    List,
    Table
}
