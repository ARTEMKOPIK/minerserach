using Microsoft.Extensions.DependencyInjection;
using MSearch.Services;
using MSearch.UI.ViewModels;
using System.Windows;

namespace MSearch.UI.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = App.ServiceProvider.GetRequiredService<MainViewModel>();
    }

    protected override void OnStateChanged(EventArgs e)
    {
        base.OnStateChanged(e);
        
        if (WindowState == WindowState.Minimized)
        {
            var configService = App.ServiceProvider.GetRequiredService<IConfigService>();
            if (configService.Settings.MinimizeToTray)
            {
                Hide();
            }
        }
    }

    protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
    {
        base.OnClosing(e);
        
        var configService = App.ServiceProvider.GetRequiredService<IConfigService>();
        if (configService.Settings.MinimizeToTray)
        {
            e.Cancel = true;
            Hide();
        }
    }
}
