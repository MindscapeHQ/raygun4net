using Microsoft.Extensions.Logging;

namespace MauiApp1
{
    public partial class App : Application
    {
        public static IServiceProvider Services;
        public App(IServiceProvider serviceProvider)
        {
            InitializeComponent();
            Services = serviceProvider;
            MainPage = new AppShell();
        }



        protected override Window CreateWindow(IActivationState activationState)
        {
            Window window = base.CreateWindow(activationState);
            window.Page = MainPage;
            window.Destroying += Window_Destroying;


            return window;
        }

        private void Window_Destroying(object sender, EventArgs e)
        {
            try
            {
              throw new Exception("ssssshshshhh");
            }
            catch (Exception ex)
            {
                Services.GetService<ILogger<MainPage>>().LogCritical(ex, "death");
                Thread.Sleep(100);
              //  throw;
            }
        }
    }

}