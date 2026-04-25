using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Serilog;
using System;
using System.IO;

namespace GModContentWizard
{
    /// <summary>
    /// Main entry point for the GModContentWizard application.
    /// </summary>
    class Program
    {
        /// <summary>
        /// Application entry point with logging and exception handling.
        /// </summary>
        /// <param name="args">Command line arguments.</param>
        [STAThread]
        public static void Main(string[] args)
        {
            var logDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "GModContentWizard",
                "logs");
            Directory.CreateDirectory(logDir);

            var logPath = Path.Combine(logDir, "app-.log");

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
                .WriteTo.File(logPath,
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 2,
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
                .CreateLogger();

            try
            {
                Log.Information("Application starting");
                var builder = BuildAvaloniaApp();
                
                bool useSoftwareRender = args.Contains("--software-render");
                if (!useSoftwareRender && IsNvidiaSystem())
                {
                    Log.Warning("NVIDIA detected, using software rendering to avoid shutdown crash");
                    useSoftwareRender = true;
                }
                
                if (useSoftwareRender)
                {
                    builder.With(new X11PlatformOptions
                    {
                        RenderingMode = new[] { X11RenderingMode.Software }
                    });
                }
                
                builder.StartWithClassicDesktopLifetime(args);
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Application terminated unexpectedly");
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }

        /// <summary>
        /// Checks if running on an NVIDIA system (heuristic).
        /// </summary>
        private static bool IsNvidiaSystem()
        {
            try
            {
                var nvidiaFiles = new[] { 
                    "/lib/libnvidia-gl.so.1", 
                    "/usr/lib/libnvidia-gl.so.1",
                    "/lib64/libnvidia-gl.so.1"
                };
                foreach (var path in nvidiaFiles)
                {
                    if (File.Exists(path))
                        return true;
                }
                
                var procModules = File.ReadAllText("/proc/modules");
                if (procModules.Contains("nvidia"))
                    return true;
            }
            catch { }
            return false;
        }

        /// <summary>
        /// Creates and configures the Avalonia application builder.
        /// </summary>
        /// <returns>The configured AppBuilder instance.</returns>
        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .LogToTrace();
    }
}