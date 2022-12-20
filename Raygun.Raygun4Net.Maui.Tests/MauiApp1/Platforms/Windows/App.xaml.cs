using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
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

        private readonly Lazy<ILogger<MainPage>> _loggerLazy;
      //  private readonly RingBuffer<Exception> _exceptionBuffer = new();
        private ILogger<MainPage> _logger => _loggerLazy.Value;


        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            _loggerLazy = new Lazy<ILogger<MainPage>>(() => Services.GetService<ILogger<MainPage>>());



            AppDomain.CurrentDomain.FirstChanceException += (sender, args) =>
            {
             //   _exceptionBuffer.Add(args.Exception);

            //    this.Services.GetService<ILogger<App>>().LogCritical(args.Exception, "it went up");
            };

            AppDomain.CurrentDomain.ProcessExit += (sender, args) =>
            {
                this.Services.GetService<ILogger<App>>()
                    .LogCritical(args.ToString(), "it went left");
            };

            AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
            {
             //   this.Services.GetService<ILogger<App>>().LogCritical(args.ExceptionObject as Exception, "it went right");
            };
            this.UnhandledException += (sender, args) =>
            {
               // var ex =   _exceptionBuffer.Find(exception => exception.Message == args.Message);
             //   _logger.LogCritical(ex ?? args.Exception, args.Exception.StackTrace is null ? "StackTrace is null" : "it went down");
            };
            this.InitializeComponent();
        }

        protected override void OnLaunched(LaunchActivatedEventArgs args)
        {
            base.OnLaunched(args);
        }

        protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
    }
}