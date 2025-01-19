using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Orange.Ide.App;

internal partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private async void AboutMenuItem_Click(object? sender, RoutedEventArgs e)
    {
        var aboutWindow = new AboutWindow();
        await aboutWindow.ShowDialog(this);
    }

    private void QuitMenuItem_Click(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}