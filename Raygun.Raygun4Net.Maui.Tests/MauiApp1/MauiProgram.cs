using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.LifecycleEvents;
//using RGmagic;

namespace MauiApp1
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();

            using var stream =  FileSystem.OpenAppPackageFileAsync("appsettings.json").Result;
            var config = new ConfigurationBuilder()
                .AddJsonStream(stream)
                .Build();


            builder.Configuration.AddConfiguration(config);


            //builder.Services.Configure<ColorConsoleLoggerConfiguration>(c => builder.Configuration.GetSection(nameof(ColorConsoleLoggerConfiguration)).Bind(c));


            //builder.Logging.AddColorConsoleLogger(configuration =>
            //{
            //    // Replace warning value from appsettings.json of "Cyan"
            //    configuration.LogLevelToColorMap[LogLevel.Warning] = ConsoleColor.DarkCyan;
            //    // Replace warning value from appsettings.json of "Red"
            //    configuration.LogLevelToColorMap[LogLevel.Error] = ConsoleColor.DarkRed;
            //});


            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

            return builder.Build();
        }
    }
}