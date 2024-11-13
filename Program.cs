using System.Reflection;

namespace GModContentWizard
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            string productName = GetAssemblyProduct();
            Mutex mutex = new(true, productName, out bool isNewInstance);
            if (!isNewInstance)
            {
                MessageBox.Show("The application is already running.", "Single Instance", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Sentry Setup
            SentrySdk.Init(o =>
            {
                // Tells which project in Sentry to send events to:
                o.Dsn = "https://5caa7114b50739ae7a97e73ab4076392@sentry.serpensin.com/20";
                // When configuring for the first time, to see what the SDK is doing:
                o.Debug = true;
                // Set TracesSampleRate to 1.0 to capture 100% of transactions for tracing.
                // We recommend adjusting this value in production.
                o.TracesSampleRate = 1.0;
                o.AutoSessionTracking = true;
                o.IsGlobalModeEnabled = true;
                o.Environment = "Development";
            });
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.ThrowException);

            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.SetCompatibleTextRenderingDefault(false);
            ApplicationConfiguration.Initialize();
            Application.Run(new Main());
        }

        private static string GetAssemblyProduct()
        {
            Assembly assembly = Assembly.GetExecutingAssembly();

            var productAttribute = (AssemblyProductAttribute)Attribute.GetCustomAttribute(assembly, typeof(AssemblyProductAttribute));

            return productAttribute?.Product;
        }
    }
}