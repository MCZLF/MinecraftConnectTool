using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Media;

namespace MinecraftConnectTool.Views.RightPage;

/// <summary>
/// 支持简单 Markdown 格式的 TextBlock：
/// - **粗体**
/// </summary>
public class FormattedTextBlock : TextBlock
{
    public static readonly StyledProperty<string> MarkdownTextProperty =
        AvaloniaProperty.Register<FormattedTextBlock, string>(nameof(MarkdownText));

    public string MarkdownText
    {
        get => GetValue(MarkdownTextProperty);
        set => SetValue(MarkdownTextProperty, value);
    }

    static FormattedTextBlock()
    {
        MarkdownTextProperty.Changed.AddClassHandler<FormattedTextBlock>((x, _) => x.UpdateInlines());
    }

    private void UpdateInlines()
    {
        Inlines?.Clear();
        var text = MarkdownText ?? string.Empty;
        if (string.IsNullOrEmpty(text)) return;

        var parts = ParseBold(text);
        foreach (var (partText, isBold) in parts)
        {
            if (isBold)
                Inlines?.Add(new Run(partText) { FontWeight = FontWeight.Bold });
            else
                Inlines?.Add(new Run(partText));
        }
    }

    private static List<(string Text, bool IsBold)> ParseBold(string text)
    {
        var result = new List<(string, bool)>();
        int i = 0;
        while (i < text.Length)
        {
            int boldStart = text.IndexOf("**", i, StringComparison.Ordinal);
            if (boldStart == -1)
            {
                result.Add((text.Substring(i), false));
                break;
            }
            if (boldStart > i)
                result.Add((text.Substring(i, boldStart - i), false));

            int boldEnd = text.IndexOf("**", boldStart + 2, StringComparison.Ordinal);
            if (boldEnd == -1)
            {
                result.Add((text.Substring(boldStart), false));
                break;
            }
            result.Add((text.Substring(boldStart + 2, boldEnd - boldStart - 2), true));
            i = boldEnd + 2;
        }
        return result;
    }
}
