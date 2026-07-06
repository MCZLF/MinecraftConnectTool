using System;
using System.Linq;
using Avalonia.Controls;
using Avalonia.LogicalTree;

namespace MinecraftConnectTool.Services;

public static class TextBoxContextMenuService
{
    public static void ApplyChineseContextMenu(Control root)
    {
        foreach (var textBox in root.GetLogicalDescendants().OfType<TextBox>())
        {
            ApplyChineseContextMenu(textBox);
        }
    }

    public static void ApplyChineseContextMenu(TextBox textBox)
    {
        var undoItem = CreateMenuItem("撤销", (_, _) => textBox.Undo());
        var redoItem = CreateMenuItem("重做", (_, _) => textBox.Redo());
        var cutItem = CreateMenuItem("剪切", (_, _) => textBox.Cut());
        var copyItem = CreateMenuItem("复制", (_, _) => textBox.Copy());
        var pasteItem = CreateMenuItem("粘贴", (_, _) => textBox.Paste());
        var selectAllItem = CreateMenuItem("全选", (_, _) => textBox.SelectAll());
        var clearItem = CreateMenuItem("清空", (_, _) => textBox.Clear());

        textBox.ContextMenu = new ContextMenu
        {
            ItemsSource = new object[]
            {
                undoItem,
                redoItem,
                new Separator(),
                cutItem,
                copyItem,
                pasteItem,
                new Separator(),
                selectAllItem,
                clearItem
            }
        };
    }

    private static MenuItem CreateMenuItem(string header, EventHandler<Avalonia.Interactivity.RoutedEventArgs> clickHandler)
    {
        var item = new MenuItem { Header = header };
        item.Click += clickHandler;
        return item;
    }
}
