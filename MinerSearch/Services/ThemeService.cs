using Microsoft.Win32;
using System.Windows;

namespace MSearch.Services;

public class ThemeService : IThemeService
{
    private readonly IConfigService _configService;
    
    public Theme CurrentTheme { get; private set; } = Theme.Light;
    public event EventHandler<Theme>? ThemeChanged;

    public ThemeService(IConfigService configService)
    {
        _configService = configService;
        CurrentTheme = _configService.Settings.Theme;
    }

    public void SetTheme(Theme theme)
    {
        CurrentTheme = theme;
        
        Application.Current.Dispatcher.Invoke(() =>
        {
            var resources = Application.Current.Resources;
            resources.MergedDictionaries.Clear();
            
            var themePath = theme == Theme.Dark 
                ? "Resources/Themes/DarkTheme.xaml" 
                : "Resources/Themes/LightTheme.xaml";
            
            resources.MergedDictionaries.Add(new ResourceDictionary
            {
                Source = new Uri(themePath, UriKind.Relative)
            });
        });

        _configService.Settings.Theme = theme;
        _configService.Save();
        
        ThemeChanged?.Invoke(this, theme);
        App.Logger?.Information("Theme changed to {Theme}", theme);
    }

    public void ToggleTheme()
    {
        SetTheme(CurrentTheme == Theme.Light ? Theme.Dark : Theme.Light);
    }

    public void ApplySystemTheme()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            
            var value = key?.GetValue("AppsUseLightTheme");
            var isDarkMode = value != null && (int)value == 0;
            
            SetTheme(isDarkMode ? Theme.Dark : Theme.Light);
        }
        catch (Exception ex)
        {
            App.Logger?.Warning(ex, "Failed to apply system theme, using default");
        }
    }
}
