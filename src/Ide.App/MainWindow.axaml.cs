using System.ComponentModel;
using System.Runtime.CompilerServices;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;

using AvaloniaEdit;
using AvaloniaEdit.CodeCompletion;
using AvaloniaEdit.Document;
using AvaloniaEdit.Editing;
using AvaloniaEdit.Folding;
using AvaloniaEdit.Indentation.CSharp;
using AvaloniaEdit.Rendering;
using AvaloniaEdit.TextMate;

using Orange.Ide.App.Resources;
using Orange.Ide.App.ViewModels;

using TextMateSharp.Grammars;

namespace Orange.Ide.App;

using Pair = KeyValuePair<int, Control?>;

public partial class MainWindow : Window
{
    private readonly TextEditor _textEditor;
    private FoldingManager? _foldingManager;
    private readonly TextMate.Installation _textMateInstallation;
    private CompletionWindow? _completionWindow;
    private OverloadInsightWindow? _insightWindow;
    private readonly ComboBox _syntaxModeCombo;
    private readonly TextBlock _statusTextBlock;
    private readonly ElementGenerator _generator = new();
    private readonly RegistryOptions _registryOptions;
    private readonly int _currentTheme = (int)ThemeName.DarkPlus;
    private readonly CustomMargin _customMargin;

    public MainWindow()
    {
        InitializeComponent();
        this.AttachDevTools();
        _textEditor = this.FindControl<TextEditor>("Editor")!;
        _textEditor.HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Visible;
        _textEditor.Background = Brushes.Transparent;
        _textEditor.ShowLineNumbers = true;
        _textEditor.TextArea.Background = this.Background;
        _textEditor.TextArea.TextEntered += TextEditor_TextArea_TextEntered;
        _textEditor.TextArea.TextEntering += TextEditor_TextArea_TextEntering;
        _textEditor.Options.AllowToggleOverstrikeMode = true;
        _textEditor.Options.EnableTextDragDrop = true;
        _textEditor.Options.ShowBoxForControlCharacters = true;
        _textEditor.Options.ColumnRulerPositions = [80, 100];
        _textEditor.TextArea.IndentationStrategy = new CSharpIndentationStrategy(_textEditor.Options);
        _textEditor.TextArea.Caret.PositionChanged += Caret_PositionChanged;
        _textEditor.TextArea.RightClickMovesCaret = true;
        _textEditor.Options.HighlightCurrentLine = true;
        _textEditor.Options.CompletionAcceptAction = CompletionAcceptAction.DoubleTapped;
        _textEditor.TextArea.TextView.ElementGenerators.Add(_generator);
        _registryOptions = new RegistryOptions((ThemeName)_currentTheme);
        _textMateInstallation = _textEditor.InstallTextMate(_registryOptions);
        _textMateInstallation.AppliedTheme += TextMateInstallationOnAppliedTheme;
        var csharpLanguage = _registryOptions.GetLanguageByExtension(".cs");
        _syntaxModeCombo = this.FindControl<ComboBox>("syntaxModeCombo")!;
        List<Language> availiableLanguages = [
            csharpLanguage,
            _registryOptions.GetLanguageByExtension(".json"),
            _registryOptions.GetLanguageByExtension(".xml"),
        ];
        _syntaxModeCombo.ItemsSource = availiableLanguages;
        _syntaxModeCombo.SelectedItem = csharpLanguage;
        _syntaxModeCombo.SelectionChanged += SyntaxModeCombo_SelectionChanged;
        string scopeName = _registryOptions.GetScopeByLanguageId(csharpLanguage.Id);
        _textEditor.Document = new TextDocument(ResourceLoader.LoadSampleFile(scopeName));
        _textMateInstallation.SetGrammar(_registryOptions.GetScopeByLanguageId(csharpLanguage.Id));
        _textEditor.TextArea.TextView.LineTransformers.Add(new UnderlineAndStrikeThroughTransformer());
        _statusTextBlock = this.Find<TextBlock>("StatusText")!;
        AddHandler(
            PointerWheelChangedEvent,
            (o, i) =>
            {
                if (i.KeyModifiers != KeyModifiers.Control) return;
                if (i.Delta.Y > 0) _textEditor.FontSize++;
                else _textEditor.FontSize = _textEditor.FontSize > 1 ? _textEditor.FontSize - 1 : 1;
            },
            RoutingStrategies.Bubble, true);
        _customMargin = new CustomMargin();
        _textEditor.TextArea.LeftMargins.Insert(0, _customMargin);
        var mainWindowVM = new MainWindowViewModel(_textMateInstallation, _registryOptions);
        foreach (ThemeName themeName in new[] { ThemeName.DarkPlus, ThemeName.LightPlus })
        {
            var themeViewModel = new ThemeViewModel(themeName);
            mainWindowVM.AllThemes.Add(themeViewModel);
            if (themeName == ThemeName.DarkPlus)
            {
                mainWindowVM.SelectedTheme = themeViewModel;
            }
        }
        DataContext = mainWindowVM;
    }

    private void TextMateInstallationOnAppliedTheme(object? sender, TextMate.Installation e)
    {
        ApplyThemeColorsToEditor(e);
        ApplyThemeColorsToWindow(e);
    }

    private void ApplyThemeColorsToEditor(TextMate.Installation e)
    {
        ApplyBrushAction(e, "editor.background", brush => _textEditor.Background = brush);
        ApplyBrushAction(e, "editor.foreground", brush => _textEditor.Foreground = brush);

        if (!ApplyBrushAction(e, "editor.selectionBackground", brush => _textEditor.TextArea.SelectionBrush = brush))
            if (Application.Current!.TryGetResource("TextAreaSelectionBrush", out var resourceObject))
                if (resourceObject is IBrush brush)
                    _textEditor.TextArea.SelectionBrush = brush;

        if (!ApplyBrushAction(e, "editor.lineHighlightBackground",
            brush =>
            {
                _textEditor.TextArea.TextView.CurrentLineBackground = brush;
                _textEditor.TextArea.TextView.CurrentLineBorder = new Pen(brush);
            }))
            _textEditor.TextArea.TextView.SetDefaultHighlightLineColors();

        if (!ApplyBrushAction(e, "editorLineNumber.foreground",
            brush => _textEditor.LineNumbersForeground = brush))
            _textEditor.LineNumbersForeground = _textEditor.Foreground;
    }

    private void ApplyThemeColorsToWindow(TextMate.Installation e)
    {
        var panel = this.Find<StackPanel>("StatusBar");
        if (panel == null)
            return;
        if (!ApplyBrushAction(e, "statusBar.background", brush => panel.Background = brush))
            panel.Background = Brushes.Purple;
        if (!ApplyBrushAction(e, "statusBar.foreground", brush => _statusTextBlock.Foreground = brush))
            _statusTextBlock.Foreground = Brushes.White;
        if (!ApplyBrushAction(e, "sideBar.background", brush => _customMargin.BackGroundBrush = brush))
            _customMargin.SetDefaultBackgroundBrush();
        ApplyBrushAction(e, "editor.background", brush => Background = brush);
        ApplyBrushAction(e, "editor.foreground", brush => Foreground = brush);
    }

    private static bool ApplyBrushAction(TextMate.Installation e, string colorKeyNameFromJson, Action<IBrush> applyColorAction)
    {
        if (!e.TryGetThemeColor(colorKeyNameFromJson, out var colorString))
            return false;
        if (!Color.TryParse(colorString, out Color color))
            return false;
        var colorBrush = new SolidColorBrush(color);
        applyColorAction(colorBrush);
        return true;
    }

    private void Caret_PositionChanged(object? sender, EventArgs e)
    {
        _statusTextBlock.Text = string.Format("Line {0} Column {1}", _textEditor.TextArea.Caret.Line, _textEditor.TextArea.Caret.Column);
    }

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);
        _textMateInstallation.Dispose();
    }

    private void SyntaxModeCombo_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        RemoveUnderlineAndStrikethroughTransformer();
        var language = (Language)_syntaxModeCombo.SelectedItem!;
        if (_foldingManager != null)
        {
            _foldingManager.Clear();
            FoldingManager.Uninstall(_foldingManager);
        }
        string scopeName = _registryOptions.GetScopeByLanguageId(language.Id);
        _textMateInstallation.SetGrammar(null);
        _textEditor.Document = new TextDocument(ResourceLoader.LoadSampleFile(scopeName));
        _textMateInstallation.SetGrammar(scopeName);
        if (language.Id == "xml")
        {
            _foldingManager = FoldingManager.Install(_textEditor.TextArea);
            var strategy = new XmlFoldingStrategy();
            strategy.UpdateFoldings(_foldingManager, _textEditor.Document);
            return;
        }
    }

    private void RemoveUnderlineAndStrikethroughTransformer()
    {
        for (int i = _textEditor.TextArea.TextView.LineTransformers.Count - 1; i >= 0; i--)
            if (_textEditor.TextArea.TextView.LineTransformers[i] is UnderlineAndStrikeThroughTransformer)
                _textEditor.TextArea.TextView.LineTransformers.RemoveAt(i);
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void TextEditor_TextArea_TextEntering(object? sender, TextInputEventArgs e)
    {
        if (e?.Text?.Length > 0 && _completionWindow != null)
            if (!char.IsLetterOrDigit(e.Text[0]))
                _completionWindow.CompletionList.RequestInsertion(e);
        _insightWindow?.Hide();
    }

    private void TextEditor_TextArea_TextEntered(object? sender, TextInputEventArgs e)
    {
        if (e.Text == ".")
        {
            _completionWindow = new CompletionWindow(_textEditor.TextArea);
            _completionWindow.Closed += (o, args) => _completionWindow = null;
            var data = _completionWindow.CompletionList.CompletionData;
            for (int i = 0; i < 500; i++)
                data.Add(new MyCompletionData("Item" + i.ToString()));
            data.Insert(20, new MyCompletionData("long item to demosntrate dynamic poup resizing"));
            _completionWindow.Show();
        }
        else if (e.Text == "(")
        {
            _insightWindow = new OverloadInsightWindow(_textEditor.TextArea);
            _insightWindow.Closed += (o, args) => _insightWindow = null;
            _insightWindow.Provider = new MyOverloadProvider(
            [
                ("Method1(int, string)", "Method1 description"),
                ("Method2(int)", "Method2 description"),
                ("Method3(string)", "Method3 description"),
            ]);
            _insightWindow.Show();
        }
    }

    private class UnderlineAndStrikeThroughTransformer : DocumentColorizingTransformer
    {
        protected override void ColorizeLine(DocumentLine line)
        {
            if (line.LineNumber == 2)
            {
                string lineText = this.CurrentContext.Document.GetText(line);
                int indexOfUnderline = lineText.IndexOf("underline");
                int indexOfStrikeThrough = lineText.IndexOf("strikethrough");
                if (indexOfUnderline != -1)
                {
                    ChangeLinePart(
                        line.Offset + indexOfUnderline,
                        line.Offset + indexOfUnderline + "underline".Length,
                        visualLine =>
                        {
                            if (visualLine.TextRunProperties.TextDecorations == null)
                                visualLine.TextRunProperties.SetTextDecorations(TextDecorations.Underline);
                            else
                            {
                                var textDecorations = new TextDecorationCollection(visualLine.TextRunProperties.TextDecorations) { TextDecorations.Underline[0] };
                                visualLine.TextRunProperties.SetTextDecorations(textDecorations);
                            }
                        }
                    );
                }
                if (indexOfStrikeThrough != -1)
                {
                    ChangeLinePart(
                        line.Offset + indexOfStrikeThrough,
                        line.Offset + indexOfStrikeThrough + "strikethrough".Length,
                        visualLine =>
                        {
                            if (visualLine.TextRunProperties.TextDecorations == null)
                                visualLine.TextRunProperties.SetTextDecorations(TextDecorations.Strikethrough);
                            else
                            {
                                var textDecorations = new TextDecorationCollection(visualLine.TextRunProperties.TextDecorations) { TextDecorations.Strikethrough[0] };
                                visualLine.TextRunProperties.SetTextDecorations(textDecorations);
                            }
                        }
                    );
                }
            }
        }
    }

    private class MyOverloadProvider : IOverloadProvider
    {
        private readonly IList<(string header, string content)> _items;
        private int _selectedIndex;

        public MyOverloadProvider(IList<(string header, string content)> items)
        {
            _items = items;
            SelectedIndex = 0;
        }

        public int SelectedIndex
        {
            get => _selectedIndex;
            set
            {
                _selectedIndex = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CurrentHeader));
                OnPropertyChanged(nameof(CurrentContent));
            }
        }

        public int Count => _items.Count;
        public string CurrentIndexText => $"{SelectedIndex + 1} of {Count}";
        public object CurrentHeader => _items[SelectedIndex].header;
        public object CurrentContent => _items[SelectedIndex].content;

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class MyCompletionData(string text) : ICompletionData
    {
        public IImage? Image => null;

        public string Text { get; } = text;

        public object Content => _contentControl ??= BuildContentControl();

        public object Description => "Description for " + Text;

        public double Priority { get; } = 0;

        public void Complete(TextArea textArea, ISegment completionSegment, EventArgs insertionRequestEventArgs)
        {
            textArea.Document.Replace(completionSegment, Text);
        }

        private TextBlock BuildContentControl()
        {
            var textBlock = new TextBlock
            {
                Text = Text,
                Margin = new Thickness(5)
            };
            return textBlock;
        }

        private Control? _contentControl;
    }

    private class ElementGenerator : VisualLineElementGenerator, IComparer<Pair>
    {
        public List<Pair> Controls { get; } = [];

        /// <summary>
        /// Gets the first interested offset using binary search
        /// </summary>
        /// <returns>The first interested offset.</returns>
        /// <param name="startOffset">Start offset.</param>
        public override int GetFirstInterestedOffset(int startOffset)
        {
            int pos = Controls.BinarySearch(new Pair(startOffset, null), this);
            if (pos < 0)
                pos = ~pos;
            return pos < Controls.Count ? Controls[pos].Key : -1;
        }

        public override VisualLineElement? ConstructElement(int offset)
        {
            int pos = Controls.BinarySearch(new Pair(offset, null), this);
            return pos >= 0 ? new InlineObjectElement(0, Controls[pos].Value!) : null;
        }

        int IComparer<Pair>.Compare(Pair x, Pair y)
        {
            return x.Key.CompareTo(y.Key);
        }
    }
}