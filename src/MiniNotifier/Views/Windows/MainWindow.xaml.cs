using System.ComponentModel;
using System.Windows;
using MiniNotifier.Helpers;
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
        Icon = AppIconProvider.LoadWindowIcon();
        snackbarService.SetSnackbarPresenter(RootSnackbarPresenter);

        Closing += OnClosing;
    }

    public void PrepareForExit()
    {
        _allowClose = true;
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
