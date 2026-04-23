using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;

namespace GModContentWizard
{
    /// <summary>
    /// Main application class for GMod Content Wizard.
    /// </summary>
    public partial class App : Application
    {
        private Window? _mainWindow;

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
                desktop.Exit += OnExit;
            }

            base.OnFrameworkInitializationCompleted();
        }

        private void OnExit(object? sender, EventArgs e)
        {
            _mainWindow?.Close();
            _mainWindow = null;
        }
    }
}