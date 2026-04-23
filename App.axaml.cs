using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using System;

namespace GModContentWizard
{
    /// <summary>
    /// Main application class for GMod Content Wizard.
    /// </summary>
    public partial class App : Application
    {
        private Window? _mainWindow;
        private bool _isShuttingDown;

        /// <summary>
        /// Initializes the application by loading XAML resources.
        /// </summary>
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        /// <summary>
        /// Called when framework initialization is complete. Sets up the main window.
        /// </summary>
        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                _mainWindow = new MainWindow();
                desktop.MainWindow = _mainWindow;
                desktop.ShutdownMode = ShutdownMode.OnMainWindowClose;
                desktop.Exit += OnDesktopExit;
            }

            base.OnFrameworkInitializationCompleted();
        }

        private void OnDesktopExit(object? sender, EventArgs e)
        {
            Dispatcher.UIThread.Post(() =>
            {
                if (_mainWindow != null && !_isShuttingDown)
                {
                    _isShuttingDown = true;
                    _mainWindow.Close();
                    _mainWindow = null;
                }
            });
        }
    }
}