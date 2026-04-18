using Avalonia.Controls;

namespace MiniNotifier.Avalonia.Views;

public partial class MainWindow : Window
{
    private bool _canExit;

    public MainWindow()
    {
        InitializeComponent();
        Closing += OnClosing;
    }

    public void PrepareForExit()
    {
        _canExit = true;
    }

    private void OnClosing(object? sender, WindowClosingEventArgs e)
    {
        if (_canExit)
        {
            return;
        }

        e.Cancel = true;
        Hide();
    }
}
