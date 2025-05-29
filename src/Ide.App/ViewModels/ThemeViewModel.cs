using TextMateSharp.Grammars;

namespace Orange.Ide.App.ViewModels;

public class ThemeViewModel(ThemeName themeName)
{
    public ThemeName ThemeName { get; } = themeName;

    public string DisplayName => ThemeName.ToString();
}