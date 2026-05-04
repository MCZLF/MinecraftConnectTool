using System;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using MinecraftConnectTool.ViewModels;

namespace MinecraftConnectTool;

public class ViewLocator : IDataTemplate
{
    public Control? Build(object? param)
    {
        if (param is null)
            return null;

        // 如果是 Control 类型（如页面），直接返回
        if (param is Control control)
        {
            return control;
        }

        var name = param.GetType().FullName!.Replace("ViewModel", "View", StringComparison.Ordinal);
        var type = Type.GetType(name);

        if (type != null)
        {
            return (Control)Activator.CreateInstance(type)!;
        }

        return new TextBlock { Text = "Not Found: " + name };
    }

    public bool Match(object? data)
    {
        return data is ViewModelBase || data is Control;
    }
}
