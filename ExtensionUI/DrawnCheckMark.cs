using System;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Media;

namespace MinecraftConnectTool.ExtensionUI;

internal sealed class DrawnCheckMark : Control
{
    internal static readonly StyledProperty<double> ProgressProperty =
        AvaloniaProperty.Register<DrawnCheckMark, double>(nameof(Progress));

    public double Progress
    {
        get => GetValue(ProgressProperty);
        set => SetValue(ProgressProperty, value);
    }

    static DrawnCheckMark()
    {
        AffectsRender<DrawnCheckMark>(ProgressProperty);
    }

    public DrawnCheckMark()
    {
        Width = 24;
        Height = 24;
        IsHitTestVisible = false;
        Transitions = new Transitions
        {
            new DoubleTransition { Property = ProgressProperty, Duration = TimeSpan.FromMilliseconds(190) }
        };
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        var progress = Math.Clamp(Progress, 0, 1);
        if (progress <= 0 || !this.TryFindResource("MaterialOnPrimaryBrush", out var resource) || resource is not IBrush brush)
            return;

        var start = new Point(6, 12);
        var corner = new Point(10, 16);
        var end = new Point(18, 8);
        var pen = new Pen(brush, 2, lineCap: PenLineCap.Round, lineJoin: PenLineJoin.Round);
        var firstProgress = Math.Min(progress * 3, 1);
        context.DrawLine(pen, start, start + (corner - start) * firstProgress);
        if (progress > 1.0 / 3)
            context.DrawLine(pen, corner, corner + (end - corner) * ((progress - 1.0 / 3) * 1.5));
    }
}
