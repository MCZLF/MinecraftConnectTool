using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MinecraftConnectTool
{
    public partial class PanelAiAnalyze : UserControl
    {
        private readonly string _logContent;
        private readonly string _serverUrl;
        private readonly int _timeoutSeconds;
        private readonly Stopwatch _stopwatch = new Stopwatch();

        private string _result = string.Empty;
        private string _reasoningContent = string.Empty;
        private string _modelUsed = string.Empty;
        private int _inputTokens;
        private int _outputTokens;
        private int _totalTokens;
        private long _elapsedMs;

        // 加载动画
        private DateTime _loadingStartTime;
        private float _spinAngle = 0f;
        private int _loadingDots = 0;

        // RTF 样式颜色
        private static readonly Color ColorPrimary = Color.FromArgb(66, 133, 244);
        private static readonly Color ColorHeading = Color.FromArgb(33, 33, 33);
        private static readonly Color ColorBody = Color.FromArgb(68, 68, 68);
        private static readonly Color ColorBullet = Color.FromArgb(66, 133, 244);
        private static readonly Color ColorTableBorder = Color.FromArgb(220, 220, 220);
        private static readonly Color ColorCode = Color.FromArgb(199, 37, 78);
        private static readonly Color ColorCodeBg = Color.FromArgb(246, 248, 250);
        private static readonly Color ColorQuiet = Color.FromArgb(140, 140, 140);

        public event EventHandler CloseRequested;

        public PanelAiAnalyze()
        {
            InitializeComponent();
        }

        public PanelAiAnalyze(string logContent, string serverUrl, string pageName, int timeoutSeconds = 120) : this()
        {
            _logContent = logContent;
            _serverUrl = serverUrl;
            _timeoutSeconds = timeoutSeconds;

            labelTitle.Text = "AI 日志分析";
            labelSource.Text = "来源: " + pageName;

            _ = AnalyzeAsync();
        }

        private void ShowPanel(Panel target)
        {
            panelLoading.Visible = (target == panelLoading);
            panelError.Visible = (target == panelError);
            panelResult.Visible = (target == panelResult);
            buttonCopy.Visible = false;
            buttonRetry.Visible = false;

            if (target == panelLoading)
                StartLoadingAnimation();
            else
                StopLoadingAnimation();
        }

        private void StartLoadingAnimation()
        {
            _loadingStartTime = DateTime.Now;
            _spinAngle = 0f;
            spinTimer.Start();
        }

        private void StopLoadingAnimation()
        {
            spinTimer.Stop();
        }

        private void spinTimer_Tick(object sender, EventArgs e)
        {
            _spinAngle = (_spinAngle + 2.5f) % 360f;
            panelSpin.Invalidate();

            // 每 0.75 秒增加一个点
            var elapsed = (DateTime.Now - _loadingStartTime).TotalSeconds;
            int newDots = ((int)(elapsed / 0.75)) % 4;
            if (newDots != _loadingDots)
            {
                _loadingDots = newDots;
                labelLoading.Text = "AI 正在分析日志" + new string('.', _loadingDots);
            }
        }

        private void panelSpin_Paint(object sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            var center = new PointF(panelSpin.Width / 2f, panelSpin.Height / 2f);
            float radius = 14f;
            int dotCount = 8;
            float dotRadius = 4f;

            for (int i = 0; i < dotCount; i++)
            {
                float angle = _spinAngle + (360f / dotCount) * i;
                float rad = angle * (float)Math.PI / 180f;
                float x = center.X + radius * (float)Math.Cos(rad) - dotRadius;
                float y = center.Y + radius * (float)Math.Sin(rad) - dotRadius;

                // 每个圆点透明度递减
                int alpha = 255 - (int)(200f * i / dotCount);
                using (var brush = new SolidBrush(Color.FromArgb(alpha, 66, 133, 244)))
                {
                    g.FillEllipse(brush, x, y, dotRadius * 2, dotRadius * 2);
                }
            }
        }

        private async Task AnalyzeAsync()
        {
            ShowPanel(panelLoading);

            _result = string.Empty;
            _reasoningContent = string.Empty;
            _modelUsed = string.Empty;
            _inputTokens = 0;
            _outputTokens = 0;
            _totalTokens = 0;
            _elapsedMs = 0;

            _stopwatch.Restart();

            try
            {
                using (var client = new HttpClient { Timeout = TimeSpan.FromSeconds(_timeoutSeconds) })
                {
                    var requestBody = new { logContent = _logContent };
                    var json = JsonConvert.SerializeObject(requestBody);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");

                    var response = await client.PostAsync($"{_serverUrl}/api/analyze/log", content);
                    _stopwatch.Stop();
                    _elapsedMs = _stopwatch.ElapsedMilliseconds;

                    var respJson = await response.Content.ReadAsStringAsync();

                    if (!response.IsSuccessStatusCode)
                    {
                        ShowError($"请求失败: {response.StatusCode}\n{respJson}");
                        return;
                    }

                    var root = JObject.Parse(respJson);

                    if (root["success"] == null || !root["success"].Value<bool>())
                    {
                        ShowError(root["message"]?.ToString() ?? "分析失败");
                        return;
                    }

                    var data = root["data"];
                    _result = data["result"]?.ToString() ?? string.Empty;
                    _modelUsed = data["model"]?.ToString() ?? string.Empty;

                    if (data["reasoningContent"] != null)
                    {
                        _reasoningContent = data["reasoningContent"].ToString();
                    }

                    var usage = data["usage"];
                    if (usage != null)
                    {
                        if (usage["prompt_tokens"] != null)
                            _inputTokens = usage["prompt_tokens"].Value<int>();
                        if (usage["completion_tokens"] != null)
                            _outputTokens = usage["completion_tokens"].Value<int>();
                        if (usage["total_tokens"] != null)
                            _totalTokens = usage["total_tokens"].Value<int>();
                    }

                    ShowResult();
                }
            }
            catch (TaskCanceledException)
            {
                _stopwatch.Stop();
                _elapsedMs = _stopwatch.ElapsedMilliseconds;
                ShowError("请求超时，请检查 AiAnalyze 服务端是否运行");
            }
            catch (Exception ex)
            {
                _stopwatch.Stop();
                _elapsedMs = _stopwatch.ElapsedMilliseconds;
                ShowError("分析异常: " + ex.Message);
            }
        }

        private void ShowError(string message)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => ShowError(message)));
                return;
            }
            ShowPanel(panelError);
            labelError.Text = message;
        }

        private void ShowResult()
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(ShowResult));
                return;
            }

            ShowPanel(panelResult);
            buttonCopy.Visible = true;
            buttonRetry.Visible = true;

            // 渲染 Markdown 到 RTF
            RenderMarkdownToRichTextBox(_result);

            // 信息标签
            if (!string.IsNullOrEmpty(_modelUsed))
            {
                labelModel.Text = "模型: " + _modelUsed;
                labelModel.Visible = true;
            }
            if (_inputTokens > 0)
            {
                labelInputTokens.Text = "输入token: " + _inputTokens;
                labelInputTokens.Visible = true;
            }
            if (_outputTokens > 0)
            {
                labelOutputTokens.Text = "输出token: " + _outputTokens;
                labelOutputTokens.Visible = true;
            }
            if (_elapsedMs > 0)
            {
                string elapsed = _elapsedMs < 1000 ? _elapsedMs + "ms" : (_elapsedMs / 1000.0).ToString("F1") + "s";
                labelElapsed.Text = "用时: " + elapsed;
                labelElapsed.Visible = true;
            }

            // 思考过程
            panelReasoning.Visible = !string.IsNullOrWhiteSpace(_reasoningContent);
        }

        #region Markdown -> RTF 渲染

        private void RenderMarkdownToRichTextBox(string markdown)
        {
            richTextBoxResult.Clear();
            if (string.IsNullOrWhiteSpace(markdown)) return;

            richTextBoxResult.BorderStyle = BorderStyle.None;
            richTextBoxResult.BackColor = Color.White;
            richTextBoxResult.WordWrap = true;
            SetLineSpacing(richTextBoxResult, 1.25f);

            foreach (var block in ParseMarkdownBlocks(markdown))
            {
                switch (block.Type)
                {
                    case BlockType.Heading:
                        AppendHeading(richTextBoxResult, block.Text, block.HeadingLevel);
                        break;
                    case BlockType.Text:
                        AppendParagraph(richTextBoxResult, block.Text);
                        break;
                    case BlockType.List:
                        AppendListBlock(richTextBoxResult, block.Items, block.IsOrdered);
                        break;
                    case BlockType.Table:
                        AppendTable(richTextBoxResult, block.TableRows);
                        break;
                    case BlockType.Code:
                        AppendCodeBlock(richTextBoxResult, block.Text);
                        break;
                }
            }

            ResetSelectionStyle(richTextBoxResult);
            richTextBoxResult.SelectionStart = 0;
            richTextBoxResult.ScrollToCaret();
        }

        private static List<ContentBlock> ParseMarkdownBlocks(string markdown)
        {
            var blocks = new List<ContentBlock>();
            var lines = markdown.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
            var paragraph = new StringBuilder();
            var listItems = new List<string>();
            var tableLines = new List<string>();
            var code = new StringBuilder();
            bool orderedList = false;
            bool inCode = false;

            void FlushParagraph()
            {
                var text = paragraph.ToString().Trim();
                if (text.Length > 0)
                {
                    blocks.Add(new ContentBlock { Type = BlockType.Text, Text = text });
                    paragraph.Clear();
                }
            }

            void FlushList()
            {
                if (listItems.Count == 0) return;
                blocks.Add(new ContentBlock { Type = BlockType.List, Items = new List<string>(listItems), IsOrdered = orderedList });
                listItems.Clear();
            }

            void FlushTable()
            {
                if (tableLines.Count == 0) return;
                var rows = new List<List<string>>();
                foreach (var tableLine in tableLines)
                {
                    if (IsSeparatorLine(tableLine)) continue;
                    var cells = ParseTableCells(tableLine);
                    if (cells.Count > 0) rows.Add(cells);
                }
                if (rows.Count > 0) blocks.Add(new ContentBlock { Type = BlockType.Table, TableRows = NormalizeTableRows(rows) });
                tableLines.Clear();
            }

            void FlushCode()
            {
                blocks.Add(new ContentBlock { Type = BlockType.Code, Text = code.ToString().TrimEnd() });
                code.Clear();
            }

            foreach (var raw in lines)
            {
                var line = raw.TrimEnd();
                var trimmed = line.Trim();

                if (trimmed.StartsWith("```"))
                {
                    if (inCode)
                    {
                        inCode = false;
                        FlushCode();
                    }
                    else
                    {
                        FlushParagraph();
                        FlushList();
                        FlushTable();
                        inCode = true;
                        code.Clear();
                    }
                    continue;
                }

                if (inCode)
                {
                    code.AppendLine(line);
                    continue;
                }

                if (string.IsNullOrWhiteSpace(line))
                {
                    FlushParagraph();
                    FlushList();
                    FlushTable();
                    continue;
                }

                var heading = Regex.Match(trimmed, "^(#{1,6})\\s+(.+)$");
                if (heading.Success)
                {
                    FlushParagraph();
                    FlushList();
                    FlushTable();
                    blocks.Add(new ContentBlock { Type = BlockType.Heading, HeadingLevel = heading.Groups[1].Value.Length, Text = heading.Groups[2].Value.Trim() });
                    continue;
                }

                if (IsTableLine(trimmed))
                {
                    FlushParagraph();
                    FlushList();
                    tableLines.Add(line);
                    continue;
                }

                if (TryParseListItem(trimmed, out var listText, out var isOrdered))
                {
                    FlushParagraph();
                    FlushTable();
                    if (listItems.Count > 0 && orderedList != isOrdered) FlushList();
                    orderedList = isOrdered;
                    listItems.Add(listText);
                    continue;
                }

                FlushList();
                FlushTable();
                if (paragraph.Length > 0) paragraph.Append(' ');
                paragraph.Append(trimmed);
            }

            if (inCode) FlushCode();
            FlushParagraph();
            FlushList();
            FlushTable();

            return blocks;
        }

        private static bool TryParseListItem(string line, out string text, out bool ordered)
        {
            text = line;
            ordered = false;

            var orderedMatch = Regex.Match(line, @"^(\d+)[\.)]\s+(.+)$");
            if (orderedMatch.Success)
            {
                text = orderedMatch.Groups[2].Value.Trim();
                ordered = true;
                return true;
            }

            var unorderedMatch = Regex.Match(line, @"^[-*+]\s+(.+)$");
            if (unorderedMatch.Success)
            {
                text = unorderedMatch.Groups[1].Value.Trim();
                return true;
            }

            return false;
        }

        private static bool IsTableLine(string line)
        {
            if (!line.Contains("|")) return false;
            return line.Trim().StartsWith("|") || line.Count(c => c == '|') >= 2;
        }

        private static bool IsSeparatorLine(string line)
        {
            var trimmed = line.Trim().Trim('|').Trim();
            if (string.IsNullOrEmpty(trimmed)) return false;
            return trimmed.All(c => c == '-' || c == ':' || c == '|' || char.IsWhiteSpace(c));
        }

        private static List<string> ParseTableCells(string line)
        {
            var trimmed = line.Trim();
            if (trimmed.StartsWith("|")) trimmed = trimmed.Substring(1);
            if (trimmed.EndsWith("|")) trimmed = trimmed.Substring(0, trimmed.Length - 1);
            return trimmed.Split('|').Select(c => CleanInlineMarkdown(c.Trim())).ToList();
        }

        private static List<List<string>> NormalizeTableRows(List<List<string>> rows)
        {
            int columns = rows.Max(r => r.Count);
            foreach (var row in rows)
            {
                while (row.Count < columns) row.Add(string.Empty);
            }
            return rows;
        }

        #endregion

        #region RTF 输出方法

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);
        private const int EM_SETPARAFORMAT = 0x0447;
        private const int EM_SETTYPOGRAPHYOPTIONS = 0x04CA;
        private const int TO_ADVANCEDTYPOGRAPHY = 1;

        private static void SetLineSpacing(RichTextBox rtb, float spacing)
        {
            try
            {
                SendMessage(rtb.Handle, EM_SETTYPOGRAPHYOPTIONS,
                    (IntPtr)TO_ADVANCEDTYPOGRAPHY, (IntPtr)TO_ADVANCEDTYPOGRAPHY);
            }
            catch { }
        }

        private static void AppendHeading(RichTextBox rtb, string text, int level)
        {
            if (string.IsNullOrWhiteSpace(text)) return;

            if (rtb.TextLength > 0) rtb.AppendText("\n");

            int markerStart = rtb.TextLength;
            rtb.SelectionStart = markerStart;
            rtb.SelectionLength = 0;
            rtb.SelectionColor = level <= 2 ? ColorPrimary : Color.FromArgb(90, 120, 170);
            rtb.SelectionFont = new Font("微软雅黑", level <= 2 ? 15f : 12f, FontStyle.Bold);
            rtb.AppendText(level <= 2 ? "▌" : "•");

            rtb.SelectionStart = rtb.TextLength;
            rtb.SelectionLength = 0;
            rtb.SelectionColor = ColorHeading;
            rtb.SelectionFont = new Font("微软雅黑", level == 1 ? 15f : level == 2 ? 13f : 11.5f, FontStyle.Bold);
            rtb.AppendText(" " + text + "\n");

            if (level <= 2)
            {
                rtb.SelectionStart = rtb.TextLength;
                rtb.SelectionLength = 0;
                rtb.SelectionColor = Color.FromArgb(226, 232, 240);
                rtb.SelectionFont = new Font("Consolas", 5f);
                rtb.AppendText(new string('─', 56) + "\n");
            }

            rtb.AppendText("\n");
            ResetSelectionStyle(rtb);
        }

        private static void AppendParagraph(RichTextBox rtb, string text)
        {
            text = CleanInlineMarkdown(text);
            if (string.IsNullOrWhiteSpace(text)) return;

            rtb.SelectionStart = rtb.TextLength;
            rtb.SelectionLength = 0;
            rtb.SelectionColor = ColorBody;
            rtb.SelectionFont = new Font("微软雅黑", 10.5f, FontStyle.Regular);
            AppendFormattedText(rtb, text);
            rtb.AppendText("\n\n");
            ResetSelectionStyle(rtb);
        }

        private static void AppendCodeBlock(RichTextBox rtb, string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return;

            var lines = text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
            rtb.AppendText("\n");

            foreach (var line in lines)
            {
                rtb.SelectionStart = rtb.TextLength;
                rtb.SelectionLength = 0;
                rtb.SelectionBackColor = ColorCodeBg;
                rtb.SelectionColor = Color.FromArgb(45, 55, 72);
                rtb.SelectionFont = new Font("Consolas", 9.5f, FontStyle.Regular);
                rtb.AppendText("  " + line + "  \n");
            }

            ResetSelectionStyle(rtb);
            rtb.AppendText("\n");
        }

        private static void AppendListBlock(RichTextBox rtb, List<string> items, bool isOrdered)
        {
            if (items == null || items.Count == 0) return;

            rtb.AppendText("\n");
            for (int i = 0; i < items.Count; i++)
            {
                rtb.SelectionStart = rtb.TextLength;
                rtb.SelectionLength = 0;
                rtb.SelectionColor = ColorPrimary;
                rtb.SelectionFont = new Font("微软雅黑", 10.5f, FontStyle.Bold);
                rtb.AppendText("  " + (isOrdered ? (i + 1) + "." : "•") + " ");

                rtb.SelectionStart = rtb.TextLength;
                rtb.SelectionLength = 0;
                rtb.SelectionColor = ColorBody;
                rtb.SelectionFont = new Font("微软雅黑", 10.5f, FontStyle.Regular);
                AppendFormattedText(rtb, CleanInlineMarkdown(items[i]));
                rtb.AppendText("\n");
            }
            rtb.AppendText("\n");
            ResetSelectionStyle(rtb);
        }

        private static void AppendFormattedText(RichTextBox rtb, string text)
        {
            if (string.IsNullOrEmpty(text)) return;

            int index = 0;
            var matches = Regex.Matches(text, @"(`[^`]+`|\*\*.+?\*\*)");
            foreach (Match match in matches)
            {
                if (match.Index > index)
                    AppendPlainText(rtb, text.Substring(index, match.Index - index));

                var value = match.Value;
                if (value.StartsWith("`") && value.EndsWith("`"))
                {
                    rtb.SelectionStart = rtb.TextLength;
                    rtb.SelectionLength = 0;
                    rtb.SelectionBackColor = ColorCodeBg;
                    rtb.SelectionColor = ColorCode;
                    rtb.SelectionFont = new Font("Consolas", 9.5f, FontStyle.Regular);
                    rtb.AppendText(" " + value.Substring(1, value.Length - 2) + " ");
                    rtb.SelectionBackColor = SystemColors.Window;
                }
                else if (value.StartsWith("**") && value.EndsWith("**"))
                {
                    rtb.SelectionStart = rtb.TextLength;
                    rtb.SelectionLength = 0;
                    rtb.SelectionColor = ColorHeading;
                    rtb.SelectionFont = new Font("微软雅黑", 10.5f, FontStyle.Bold);
                    rtb.AppendText(value.Substring(2, value.Length - 4));
                }

                index = match.Index + match.Length;
            }

            if (index < text.Length)
                AppendPlainText(rtb, text.Substring(index));
        }

        private static void AppendPlainText(RichTextBox rtb, string text)
        {
            rtb.SelectionStart = rtb.TextLength;
            rtb.SelectionLength = 0;
            rtb.SelectionBackColor = SystemColors.Window;
            rtb.SelectionColor = ColorBody;
            rtb.SelectionFont = new Font("微软雅黑", 10.5f, FontStyle.Regular);
            rtb.AppendText(text);
        }

        private static void AppendTable(RichTextBox rtb, List<List<string>> rows)
        {
            if (rows == null || rows.Count == 0) return;

            int colCount = rows.Max(r => r.Count);
            var tabs = BuildTableTabs(rtb, rows, colCount);
            rtb.AppendText("\n");

            for (int ri = 0; ri < rows.Count; ri++)
            {
                var row = rows[ri];
                var rowText = string.Join("\t", row.Select(c => TruncateCell(CleanInlineMarkdown(c), 42)));
                Color bg = ri == 0 ? Color.FromArgb(239, 246, 255) : ri % 2 == 0 ? Color.White : Color.FromArgb(248, 250, 252);

                rtb.SelectionStart = rtb.TextLength;
                rtb.SelectionLength = 0;
                rtb.SelectionTabs = tabs;
                rtb.SelectionBackColor = bg;
                rtb.SelectionColor = ri == 0 ? ColorHeading : ColorBody;
                rtb.SelectionFont = new Font("微软雅黑", 9.5f, ri == 0 ? FontStyle.Bold : FontStyle.Regular);
                rtb.AppendText("  " + rowText + "  \n");

                if (ri == 0)
                {
                    rtb.SelectionStart = rtb.TextLength;
                    rtb.SelectionLength = 0;
                    rtb.SelectionTabs = tabs;
                    rtb.SelectionBackColor = Color.FromArgb(219, 234, 254);
                    rtb.SelectionColor = Color.FromArgb(96, 165, 250);
                    rtb.SelectionFont = new Font("Consolas", 8f, FontStyle.Regular);
                    rtb.AppendText("  " + string.Join("\t", Enumerable.Repeat("────────", colCount)) + "  \n");
                }
            }

            ResetSelectionStyle(rtb);
            rtb.AppendText("\n");
        }

        private static int[] BuildTableTabs(RichTextBox rtb, List<List<string>> rows, int colCount)
        {
            var widths = new int[colCount];
            using (var g = rtb.CreateGraphics())
            using (var headerFont = new Font("微软雅黑", 9.5f, FontStyle.Bold))
            using (var bodyFont = new Font("微软雅黑", 9.5f, FontStyle.Regular))
            {
                for (int c = 0; c < colCount; c++)
                {
                    int max = 64;
                    for (int r = 0; r < rows.Count; r++)
                    {
                        string value = c < rows[r].Count ? TruncateCell(CleanInlineMarkdown(rows[r][c]), 42) : string.Empty;
                        int measured = (int)Math.Ceiling(g.MeasureString(value, r == 0 ? headerFont : bodyFont).Width) + 26;
                        max = Math.Max(max, measured);
                    }
                    widths[c] = Math.Min(max, 220);
                }
            }

            var tabs = new int[Math.Max(0, colCount - 1)];
            int current = 14;
            for (int i = 0; i < tabs.Length; i++)
            {
                current += widths[i];
                tabs[i] = current;
            }
            return tabs;
        }

        private static string TruncateCell(string text, int maxLength)
        {
            if (string.IsNullOrEmpty(text) || text.Length <= maxLength) return text;
            return text.Substring(0, maxLength - 1) + "…";
        }

        private static string CleanInlineMarkdown(string text)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;
            text = Regex.Replace(text, @"^#{1,6}\s+", string.Empty);
            text = text.Replace("<br>", " ").Replace("<br/>", " ").Replace("<br />", " ");
            return text.Trim();
        }

        private static void ResetSelectionStyle(RichTextBox rtb)
        {
            rtb.SelectionStart = rtb.TextLength;
            rtb.SelectionLength = 0;
            rtb.SelectionTabs = new int[0];
            rtb.SelectionBackColor = SystemColors.Window;
            rtb.SelectionColor = ColorBody;
            rtb.SelectionFont = new Font("微软雅黑", 10.5f, FontStyle.Regular);
        }

        #endregion

        #region 事件

        private void buttonBack_Click(object sender, EventArgs e)
        {
            CloseRequested?.Invoke(this, EventArgs.Empty);
        }

        private void buttonCopy_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(_result))
            {
                try
                {
                    Clipboard.SetText(_result);
                    AntdUI.Message.success(Program.MainForm, "已复制分析结果", autoClose: 3, font: Program.AlertFont);
                }
                catch
                {
                    // 忽略复制失败
                }
            }
        }

        private void buttonRetry_Click(object sender, EventArgs e)
        {
            _ = AnalyzeAsync();
        }

        private void buttonErrorRetry_Click(object sender, EventArgs e)
        {
            _ = AnalyzeAsync();
        }

        private bool _reasoningExpanded = false;
        private void buttonReasoning_Click(object sender, EventArgs e)
        {
            _reasoningExpanded = !_reasoningExpanded;
            richTextBoxReasoning.Visible = _reasoningExpanded;
            buttonReasoning.Text = _reasoningExpanded ? "思考过程 ▲" : "思考过程 ▼";

            if (_reasoningExpanded && string.IsNullOrEmpty(richTextBoxReasoning.Text))
            {
                richTextBoxReasoning.Text = _reasoningContent;
            }
        }

        #endregion
    }

    #region 数据模型

    internal class MarkdownSection
    {
        public string Title { get; set; } = string.Empty;
        public List<ContentBlock> Blocks { get; set; } = new List<ContentBlock>();
    }

    internal class ContentBlock
    {
        public BlockType Type { get; set; }
        public string Text { get; set; } = string.Empty;
        public int HeadingLevel { get; set; }
        public List<string> Items { get; set; } = new List<string>();
        public bool IsOrdered { get; set; }
        public List<List<string>> TableRows { get; set; } = new List<List<string>>();
    }

    internal enum BlockType
    {
        Text,
        Heading,
        List,
        Table,
        Code
    }

    #endregion
}
