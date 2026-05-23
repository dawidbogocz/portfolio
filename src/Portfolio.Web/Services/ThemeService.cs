// ThemeService.cs -- manages the 4 portfolio themes
// How to add a theme: add a new case to AllThemes list and SetTheme switch,
// then add corresponding [data-theme="name"] CSS variables in app.css
namespace Portfolio.Web.Services;

public class ThemeService
{
    public string CurrentTheme { get; private set; } = "vsdark";
    public event Action? ThemeChanged;

    // Available themes: vsdark (default), godot, monokai, light
    // Each corresponds to a [data-theme] CSS selector in app.css
    public static readonly string[] AllThemes = ["vsdark", "godot", "monokai", "light"];

    public void SetTheme(string theme)
    {
        if (CurrentTheme == theme) return;
        CurrentTheme = theme;
        ThemeChanged?.Invoke();
    }
}