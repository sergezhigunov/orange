using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Orange.Ide.App;

internal partial class AboutWindow : Window
{
    public AboutWindow()
    {
        InitializeComponent();
    }

    private void Button_Click(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}