using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MinecraftConnectTool.Controls;

public partial class ColorPickerButton : UserControl
{
    public static readonly StyledProperty<Color> SelectedColorProperty =
        AvaloniaProperty.Register<ColorPickerButton, Color>(nameof(SelectedColor), Color.Parse("#6750A4"));

    public static readonly StyledProperty<string> TitleProperty =
        AvaloniaProperty.Register<ColorPickerButton, string>(nameof(Title), "选择颜色");

    public static readonly StyledProperty<double> MixIntensityProperty =
        AvaloniaProperty.Register<ColorPickerButton, double>(nameof(MixIntensity), 0.15);

    public Color SelectedColor
    {
        get => GetValue(SelectedColorProperty);
        set => SetValue(SelectedColorProperty, value);
    }

    public string Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public double MixIntensity
    {
        get => GetValue(MixIntensityProperty);
        set => SetValue(MixIntensityProperty, value);
    }

    public event EventHandler<Color>? ColorChanged;
    public event EventHandler<double>? MixIntensityChanged;

    public ColorPickerButton()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void OnButtonClick(object? sender, RoutedEventArgs e)
    {
        ShowColorPickerPopup();
    }

    private void ShowColorPickerPopup()
    {
        // 当前临时颜色（用于预览）
        var tempColor = SelectedColor;
        Button? selectedPresetButton = null;

        // 先声明滑块和预览变量，稍后再初始化
        StackPanel? rSliderPanel = null, gSliderPanel = null, bSliderPanel = null;
        Slider? rSlider = null, gSlider = null, bSlider = null;
        Border? previewBorder = null;

        // 创建弹出窗口
        var popupWindow = new Window
        {
            Title = Title,
            Width = 320,
            Height = 560,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false,
            ShowInTaskbar = false,
            Background = this.FindResource("MaterialSurfaceBrush") as IBrush
        };

        // 主布局
        var mainPanel = new StackPanel
        {
            Margin = new Thickness(16),
            Spacing = 16
        };

        // 预设颜色网格
        var presetPanel = new StackPanel { Spacing = 8 };
        presetPanel.Children.Add(new TextBlock
        {
            Text = "预设颜色",
            FontSize = 14,
            FontWeight = FontWeight.Medium,
            Foreground = this.FindResource("MaterialOnSurfaceBrush") as IBrush
        });

        var presetGrid = new UniformGrid
        {
            Columns = 6,
            Rows = 3,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center
        };

        // 预设颜色列表
        var presetColors = new[]
        {
            "#F44336", "#E91E63", "#9C27B0", "#673AB7", "#3F51B5", "#2196F3",
            "#03A9F4", "#00BCD4", "#009688", "#4CAF50", "#8BC34A", "#CDDC39",
            "#FFEB3B", "#FFC107", "#FF9800", "#FF5722", "#795548", "#607D8B"
        };

        foreach (var colorHex in presetColors)
        {
            var color = Color.Parse(colorHex);
            var colorButton = new Button
            {
                Width = 36,
                Height = 36,
                Margin = new Thickness(4),
                CornerRadius = new CornerRadius(18),
                Background = new SolidColorBrush(color),
                BorderThickness = new Thickness(0),
                BorderBrush = this.FindResource("MaterialOnSurfaceBrush") as IBrush
            };

            colorButton.Click += (s, e) =>
            {
                // 取消之前选中的按钮边框
                if (selectedPresetButton != null)
                {
                    selectedPresetButton.BorderThickness = new Thickness(0);
                }
                
                // 设置当前选中的按钮
                selectedPresetButton = colorButton;
                selectedPresetButton.BorderThickness = new Thickness(2);
                
                // 更新临时颜色
                tempColor = color;
                
                // 更新滑块值
                if (rSlider != null) rSlider.Value = color.R;
                if (gSlider != null) gSlider.Value = color.G;
                if (bSlider != null) bSlider.Value = color.B;
                
                // 更新预览
                if (previewBorder != null)
                    previewBorder.Background = new SolidColorBrush(tempColor);
            };

            // 如果当前颜色匹配预设，高亮显示
            if (color == SelectedColor)
            {
                colorButton.BorderThickness = new Thickness(2);
                selectedPresetButton = colorButton;
            }

            presetGrid.Children.Add(colorButton);
        }

        presetPanel.Children.Add(presetGrid);
        mainPanel.Children.Add(presetPanel);

        // 自定义颜色区域
        var customPanel = new StackPanel { Spacing = 8 };
        customPanel.Children.Add(new TextBlock
        {
            Text = "自定义颜色",
            FontSize = 14,
            FontWeight = FontWeight.Medium,
            Foreground = this.FindResource("MaterialOnSurfaceBrush") as IBrush
        });

        // RGB 滑块
        var rgbPanel = new StackPanel { Spacing = 8 };

        // 颜色预览
        previewBorder = new Border
        {
            Height = 48,
            CornerRadius = new CornerRadius(8),
            Background = new SolidColorBrush(tempColor),
            BorderThickness = new Thickness(1),
            BorderBrush = this.FindResource("MaterialOutlineBrush") as IBrush
        };

        // 创建滑块
        rSliderPanel = CreateColorSlider("R", tempColor.R, (v) => 
        {
            tempColor = Color.FromRgb(v, tempColor.G, tempColor.B);
            if (previewBorder != null)
                previewBorder.Background = new SolidColorBrush(tempColor);
            // 取消预设选中状态
            if (selectedPresetButton != null)
            {
                selectedPresetButton.BorderThickness = new Thickness(0);
                selectedPresetButton = null;
            }
        }, out rSlider);
        gSliderPanel = CreateColorSlider("G", tempColor.G, (v) => 
        {
            tempColor = Color.FromRgb(tempColor.R, v, tempColor.B);
            if (previewBorder != null)
                previewBorder.Background = new SolidColorBrush(tempColor);
            if (selectedPresetButton != null)
            {
                selectedPresetButton.BorderThickness = new Thickness(0);
                selectedPresetButton = null;
            }
        }, out gSlider);
        bSliderPanel = CreateColorSlider("B", tempColor.B, (v) => 
        {
            tempColor = Color.FromRgb(tempColor.R, tempColor.G, v);
            if (previewBorder != null)
                previewBorder.Background = new SolidColorBrush(tempColor);
            if (selectedPresetButton != null)
            {
                selectedPresetButton.BorderThickness = new Thickness(0);
                selectedPresetButton = null;
            }
        }, out bSlider);

        rgbPanel.Children.Add(rSliderPanel);
        rgbPanel.Children.Add(gSliderPanel);
        rgbPanel.Children.Add(bSliderPanel);

        customPanel.Children.Add(rgbPanel);
        customPanel.Children.Add(previewBorder);
        mainPanel.Children.Add(customPanel);

        // 混色浓度滑块
        var intensityPanel = new StackPanel { Spacing = 8 };
        intensityPanel.Children.Add(new TextBlock
        {
            Text = "混色浓度",
            FontSize = 14,
            FontWeight = FontWeight.Medium,
            Foreground = this.FindResource("MaterialOnSurfaceBrush") as IBrush
        });

        var intensityValue = MixIntensity;
        var intensityValueBlock = new TextBlock
        {
            Text = intensityValue.ToString("F2"),
            Width = 50,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
            Foreground = this.FindResource("MaterialOnSurfaceBrush") as IBrush
        };

        var intensitySlider = new Slider
        {
            Minimum = 0.01,
            Maximum = 1.00,
            Value = intensityValue,
            Width = 200,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
            TickFrequency = 0.01,
            IsSnapToTickEnabled = true
        };

        var intensitySliderPanel = new StackPanel 
        { 
            Orientation = Avalonia.Layout.Orientation.Horizontal, 
            Spacing = 8 
        };
        intensitySliderPanel.Children.Add(intensitySlider);
        intensitySliderPanel.Children.Add(intensityValueBlock);

        intensitySlider.ValueChanged += (s, e) =>
        {
            intensityValue = Math.Round(e.NewValue, 2);
            intensityValueBlock.Text = intensityValue.ToString("F2");
        };

        intensityPanel.Children.Add(intensitySliderPanel);
        mainPanel.Children.Add(intensityPanel);

        // 按钮区域（确定 + 重置）
        var buttonPanel = new StackPanel
        {
            Orientation = Avalonia.Layout.Orientation.Horizontal,
            Spacing = 8,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch,
            Margin = new Thickness(0, 8, 0, 0)
        };

        // 重置按钮
        var resetButton = new Button
        {
            Content = "重置配置",
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch,
            HorizontalContentAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            Width = 100
        };
        resetButton.Click += (s, e) =>
        {
            // 重置为默认值
            tempColor = Color.Parse("#6750A4");
            intensityValue = 0.15;
            
            // 更新滑块
            rSlider!.Value = tempColor.R;
            gSlider!.Value = tempColor.G;
            bSlider!.Value = tempColor.B;
            intensitySlider.Value = intensityValue;
            
            // 更新预览
            previewBorder!.Background = new SolidColorBrush(tempColor);
            intensityValueBlock.Text = intensityValue.ToString("F2");
            
            // 取消预设选中
            if (selectedPresetButton != null)
            {
                selectedPresetButton.BorderThickness = new Thickness(0);
                selectedPresetButton = null;
            }
        };
        buttonPanel.Children.Add(resetButton);

        // 随机按钮
        var randomButton = new Button
        {
            Content = "🎲 随机",
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch,
            HorizontalContentAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            Width = 80
        };
        randomButton.Click += (s, e) =>
        {
            // 生成随机颜色
            var random = new Random();
            tempColor = Color.FromRgb(
                (byte)random.Next(256),
                (byte)random.Next(256),
                (byte)random.Next(256)
            );
            
            // 更新滑块
            rSlider!.Value = tempColor.R;
            gSlider!.Value = tempColor.G;
            bSlider!.Value = tempColor.B;
            
            // 更新预览
            previewBorder!.Background = new SolidColorBrush(tempColor);
            
            // 取消预设选中
            if (selectedPresetButton != null)
            {
                selectedPresetButton.BorderThickness = new Thickness(0);
                selectedPresetButton = null;
            }
        };
        buttonPanel.Children.Add(randomButton);

        // 确认按钮
        var confirmButton = new Button
        {
            Content = "确定",
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch,
            HorizontalContentAlignment = Avalonia.Layout.HorizontalAlignment.Center
        };
        confirmButton.Click += (s, e) =>
        {
            SelectedColor = tempColor;
            MixIntensity = intensityValue;
            ColorChanged?.Invoke(this, tempColor);
            MixIntensityChanged?.Invoke(this, intensityValue);
            popupWindow.Close();
        };
        buttonPanel.Children.Add(confirmButton);
        
        mainPanel.Children.Add(buttonPanel);

        popupWindow.Content = new ScrollViewer { Content = mainPanel };

        // 获取当前窗口作为 owner
        if (VisualRoot is Window owner)
        {
            popupWindow.ShowDialog(owner);
        }
        else
        {
            popupWindow.Show();
        }
    }

    private StackPanel CreateColorSlider(string label, byte value, Action<byte> onValueChanged, out Slider slider)
    {
        var panel = new StackPanel { Orientation = Avalonia.Layout.Orientation.Horizontal, Spacing = 8 };

        var labelBlock = new TextBlock
        {
            Text = label,
            Width = 20,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
            Foreground = this.FindResource("MaterialOnSurfaceBrush") as IBrush
        };

        slider = new Slider
        {
            Minimum = 0,
            Maximum = 255,
            Value = value,
            Width = 200,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
        };

        var localSlider = slider;

        var valueTextBox = new TextBox
        {
            Text = value.ToString(),
            Width = 50,
            Height = 20,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            TextAlignment = Avalonia.Media.TextAlignment.Center,
            BorderThickness = new Thickness(0, 0, 0, 1),
            BorderBrush = this.FindResource("MaterialOutlineBrush") as IBrush,
            Background = Brushes.Transparent,
            Padding = new Thickness(0, 0, 0, 2),
            FontSize = 13
        };

        slider.ValueChanged += (s, e) =>
        {
            var byteValue = (byte)e.NewValue;
            valueTextBox.Text = byteValue.ToString();
            onValueChanged(byteValue);
        };

        void UpdateValueFromText()
        {
            if (int.TryParse(valueTextBox.Text, out var parsedValue))
            {
                var clampedValue = Math.Clamp(parsedValue, 0, 255);
                var byteValue = (byte)clampedValue;
                localSlider.Value = byteValue;
                onValueChanged(byteValue);
                
                // 如果输入值超出范围，更新文本框显示为边界值
                if (parsedValue < 0 || parsedValue > 255)
                {
                    valueTextBox.Text = byteValue.ToString();
                }
            }
            else
            {
                valueTextBox.Text = ((byte)localSlider.Value).ToString();
            }
        }

        valueTextBox.TextChanged += (s, e) =>
        {
            if (int.TryParse(valueTextBox.Text, out var parsedValue))
            {
                var clampedValue = Math.Clamp(parsedValue, 0, 255);
                var byteValue = (byte)clampedValue;
                localSlider.Value = byteValue;
                onValueChanged(byteValue);
                
                // 如果输入值超出范围，更新文本框显示为边界值
                if (parsedValue < 0 || parsedValue > 255)
                {
                    valueTextBox.Text = byteValue.ToString();
                }
            }
        };

        valueTextBox.LostFocus += (s, e) =>
        {
            UpdateValueFromText();
        };

        valueTextBox.KeyDown += (s, e) =>
        {
            if (e.Key == Key.Enter)
            {
                UpdateValueFromText();
                e.Handled = true;
            }
        };

        panel.Children.Add(labelBlock);
        panel.Children.Add(slider);
        panel.Children.Add(valueTextBox);

        return panel;
    }
}
