using System.ComponentModel;
using System.Windows;
using MiniNotifier.ViewModels.Windows;
using Wpf.Ui;

namespace MiniNotifier.Views.Windows;

public partial class MainWindow
{
    private readonly MainWindowViewModel _viewModel;
    private bool _allowClose;

    public MainWindow(MainWindowViewModel viewModel, ISnackbarService snackbarService)
    {
        _viewModel = viewModel;

        InitializeComponent();

        DataContext = _viewModel;
        snackbarService.SetSnackbarPresenter(RootSnackbarPresenter);

        Loaded += OnLoaded;
        Closing += OnClosing;
    }

    public void PrepareForExit()
    {
        _allowClose = true;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        await _viewModel.InitializeAsync();
        Hide();
    }

    private void OnClosing(object? sender, CancelEventArgs e)
    {
        if (_allowClose)
        {
            return;
        }

        e.Cancel = true;
        Hide();
    }
}
