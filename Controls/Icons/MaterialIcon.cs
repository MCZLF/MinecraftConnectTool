using System;
using System.Globalization;
using Avalonia;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace Material.Icons.Avalonia;

public class MaterialIcon : TemplatedControl
{
    public static readonly StyledProperty<MaterialIconKind> KindProperty =
        AvaloniaProperty.Register<MaterialIcon, MaterialIconKind>(nameof(Kind));

    public MaterialIconKind Kind
    {
        get => GetValue(KindProperty);
        set => SetValue(KindProperty, value);
    }
}

public sealed class MaterialIconKindToGeometryConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not MaterialIconKind kind)
            return BindingOperations.DoNothing;

        return MaterialIconPathData.GetPath(kind) is { } path ? Geometry.Parse(path) : null;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
