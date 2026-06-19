using System;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using MinecraftConnectTool.ViewModels.RightPage;

namespace MinecraftConnectTool.Views.RightPage;

public partial class ETRoomList : UserControl
{
    public event EventHandler? CloseRequested;

    public ETRoomList()
    {
        InitializeComponent();
        DataContext = new ETRoomListViewModel();

        if (DataContext is ETRoomListViewModel vm)
        {
            vm.CloseRequested += (s, e) => CloseRequested?.Invoke(this, EventArgs.Empty);
        }
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
