using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Raygun.Raygun4Net.Maui;
using System.Diagnostics;

//using RGmagic;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace MauiApp1.WinUI
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public partial class App : MauiWinUIApplication
    {

        private readonly Lazy<ILogger> _loggerLazy;
        private readonly RingBuffer<Exception> _exceptionBuffer = new();
        private ILogger _logger => _loggerLazy.Value;


        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            _loggerLazy = new Lazy<ILogger>(() => Services.GetService<ILogger>());


            HandleTheUnhandled();

            AppDomain.CurrentDomain.ProcessExit += (sender, args) =>
            {
                this.Services.GetService<ILogger<App>>()
                    .LogCritical(args.ToString(), "it went left");
            };

            AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
            {
                this.Services.GetService<ILogger<App>>().LogCritical(args.ExceptionObject as Exception, "it went right");
            };

            this.InitializeComponent();
        }


    //TODO: Move this into an extension method in the Raygun.Raygun4Net.Maui project
    private void HandleTheUnhandled()
        {
          AppDomain.CurrentDomain.FirstChanceException += (sender, args) =>
          {

            _exceptionBuffer.Add(args.Exception);
          };

          UnhandledException += (sender, args) =>
          {
            if (args.Exception.StackTrace is not null)
            {
              Services.GetService<ILogger>().LogCritical(args.Exception, "UnhandledException");
            }
            var ex = _exceptionBuffer.Find(exception => exception.Message == args.Message);
            _logger.LogCritical(ex ?? args.Exception, "UnhandledException");

          };
          AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
          {
            if (args.ExceptionObject is Exception ex)
            {
              _logger.LogCritical(ex, "UnhandledException");
            }
            //var ex = _exceptionBuffer.Find(exception => exception.Message == args.);
            //_logger.LogCritical(ex ?? args.Exception, "UnhandledException");

          };
    }

        protected override void OnLaunched(LaunchActivatedEventArgs args)
        {
            base.OnLaunched(args);
        }

        protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
    }
}