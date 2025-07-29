using SerpentModding;
using System.Reflection;
using System.Runtime.InteropServices;

namespace GModContentWizard
{
    internal static class Program
    {
        [DllImport("kernel32.dll")]
        private static extern IntPtr GetConsoleWindow();
        [DllImport("kernel32.dll")]
        private static extern bool AttachConsole(int dwProcessId);
        private const int ATTACH_PARENT_PROCESS = -1;

        [STAThread]
        static void Main(string[] args)
        {
            Logger.Instance.Trace("Program.Main() started");
            string productName = GetAssemblyProduct();
            Logger.Instance.Debug($"Product name: {productName}");
            using Mutex mutex = new(true, productName, out bool isNewInstance);
            if (!isNewInstance)
            {
                Logger.Instance.Warn("Application already running. Exiting.");
                MessageBox.Show("The application is already running.", "Single Instance", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            EnsureConsoleAttached();
            Logger.Instance.Info("Console attached (if needed)");
            if (args.Any(a => a.Equals("--help", StringComparison.OrdinalIgnoreCase)))
            {
                Logger.Instance.Info("Help argument detected. Showing help.");
                Console.WriteLine("DiscordEmojiDownloader - Options:");
                Console.WriteLine("  --log-level=LEVEL   Sets the LogLevel (Trace, Debug, Info, Warn, Error, Fatal)");
                Console.WriteLine("  --help              Shows this help");
                Console.WriteLine();
                Console.WriteLine("Note: If you started this app from a console, close the window yourself after reading this help.");
                Environment.Exit(0);
            }
#if DEBUG
            LogLevel logLevel = LogLevel.Debug;
#else
            LogLevel logLevel = LogLevel.Info;
#endif
            Logger.Instance.Debug("Parsing command line arguments for log level");
            foreach (var arg in args)
            {
                if (arg.StartsWith("--log-level=", StringComparison.OrdinalIgnoreCase))
                {
                    var value = arg[12..];
                    if (Enum.TryParse<LogLevel>(value, true, out var parsedLevel))
                    {
                        logLevel = parsedLevel;
                        Logger.Instance.Info($"Log level set to {logLevel}");
                    }
                    else
                    {
                        Logger.Instance.Warn($"Invalid log level argument: {value}");
                    }
                }
            }

            Logger.Instance.Initialize(logLevel, logToConsole: true);
            Logger.Instance.Trace($"Logger initialized with logLevel={logLevel}");
            Logger.Instance.Info("Initializing Logger and application configuration.");
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.ThrowException);
            Logger.Instance.Debug("UnhandledExceptionMode set to ThrowException");

            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Logger.Instance.Info("High DPI mode set to SystemAware");
            Application.SetCompatibleTextRenderingDefault(false);
            Logger.Instance.Info("Compatible text rendering set to false");
            ApplicationConfiguration.Initialize();
            Logger.Instance.Info("Application configuration initialized");
            try
            {
                Logger.Instance.Trace("Starting Main form");
                Application.Run(new Main());
            }
            catch (Exception ex)
            {
                Logger.Instance.Error($"Fatal error in Main: {ex.Message}");
                Logger.Instance.Fatal($"Unhandled exception: {ex}");
                throw;
            }
        }

        private static string GetAssemblyProduct()
        {
            Logger.Instance.Trace("GetAssemblyProduct() called");
            Assembly assembly = Assembly.GetExecutingAssembly();
            var productAttribute = (AssemblyProductAttribute)Attribute.GetCustomAttribute(assembly, typeof(AssemblyProductAttribute));
            Logger.Instance.Debug($"Product attribute: {productAttribute?.Product}");
            return productAttribute?.Product;
        }

        private static void EnsureConsoleAttached()
        {
            Logger.Instance.Trace("EnsureConsoleAttached() called");
            if (GetConsoleWindow() == IntPtr.Zero)
            {
                Logger.Instance.Info("No console window detected. Attaching to parent process.");
                AttachConsole(ATTACH_PARENT_PROCESS);
            }
            else
            {
                Logger.Instance.Debug("Console window already present.");
            }
        }
    }
}