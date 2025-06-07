using System.Configuration;
using System.Data;
using System.Windows;

namespace FeatureManager;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>

public enum AppTheme { Light, Dark }

public partial class App : System.Windows.Application
{
    public void SetTheme(AppTheme theme)
    {
        var dict = new ResourceDictionary();
        switch (theme)
        {
            case AppTheme.Dark:
                dict.Source = new Uri("Themes/DarkTheme.xaml", UriKind.Relative);
                break;
            case AppTheme.Light:
            default:
                dict.Source = new Uri("Themes/LightTheme.xaml", UriKind.Relative);
                break;
        }

        Resources.MergedDictionaries.Clear();
        Resources.MergedDictionaries.Add(dict);
    }
}