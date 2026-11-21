using System.Threading;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;

namespace MinecraftConnectTool.Services;

public sealed partial class ModCheckProgress : ObservableObject
{
    private int _operation;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsVisible))]
    private string _text = "";

    [ObservableProperty]
    private bool _isIndeterminate;

    [ObservableProperty]
    private double _value;

    public bool IsVisible => Text.Length != 0;

    internal int Begin(string text)
    {
        var operation = Interlocked.Increment(ref _operation);
        Update(operation, text);
        return operation;
    }

    internal void Update(int operation, string text, double? percent = null)
    {
        Dispatcher.UIThread.Post(() =>
        {
            if (operation != Volatile.Read(ref _operation))
                return;

            IsIndeterminate = percent == null;
            Value = percent ?? 0;
            Text = text;
        });
    }

    internal void Scanning(int operation, int completed, int total) =>
        Update(operation, $"模组校验 · 已扫描 {completed} / {total}", total == 0 ? 100 : completed * 100.0 / total);

    internal void End(int operation) => Update(operation, "");
}
