using System.Collections.ObjectModel;

using AvaloniaEdit;
using AvaloniaEdit.Editing;
using AvaloniaEdit.TextMate;

using CommunityToolkit.Mvvm.ComponentModel;

using TextMateSharp.Grammars;
#nullable disable

namespace Orange.Ide.App.ViewModels;

public class MainWindowViewModel(TextMate.Installation textMateInstallation, RegistryOptions registryOptions) : ObservableObject
{
    public ObservableCollection<ThemeViewModel> AllThemes { get; set; } = [];
    private ThemeViewModel _selectedTheme;
    private readonly TextMate.Installation _textMateInstallation = textMateInstallation;
    private readonly RegistryOptions _registryOptions = registryOptions;

    public ThemeViewModel SelectedTheme
    {
        get => _selectedTheme;
        set
        {
            SetProperty(ref _selectedTheme, value);
            _textMateInstallation.SetTheme(_registryOptions.LoadTheme(value.ThemeName));
        }
    }

    public static void CopyMouseCommand(TextArea textArea)
    {
        ApplicationCommands.Copy.Execute(null, textArea);
    }

    public static void CutMouseCommand(TextArea textArea)
    {
        ApplicationCommands.Cut.Execute(null, textArea);
    }

    public static void PasteMouseCommand(TextArea textArea)
    {
        ApplicationCommands.Paste.Execute(null, textArea);
    }

    public static void SelectAllMouseCommand(TextArea textArea)
    {
        ApplicationCommands.SelectAll.Execute(null, textArea);
    }

    public static void UndoMouseCommand(TextArea textArea)
    {
        ApplicationCommands.Undo.Execute(null, textArea);
    }
}