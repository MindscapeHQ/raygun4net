using Microsoft.Extensions.Logging;

namespace MauiApp1
{
    public partial class MainPage : ContentPage
    {
        int count = 0;
        private readonly Lazy<ILogger<MainPage>> _loggerLazy;
        private ILogger<MainPage> _logger => _loggerLazy.Value;

        public MainPage()
        {
            _loggerLazy = new Lazy<ILogger<MainPage>>(() => Handler.MauiContext.Services.GetService<ILogger<MainPage>>());
            InitializeComponent();
        }

        private void OnCounterClicked(object sender, EventArgs e)
        {
            count++;



            if (count == 1)
                CounterBtn.Text = $"Clicked {count} time";
            else
                CounterBtn.Text = $"Clicked {count} times";

            if (count == 3)
            {
                try
                {
                    throw new ArgumentException("rarrha");
                }
                catch (Exception exception)
                {
                    _logger.LogError( exception, "oh no");
                    Console.WriteLine(exception);
                    throw;
                }
            }

            SemanticScreenReader.Announce(CounterBtn.Text);
        }
    }
}