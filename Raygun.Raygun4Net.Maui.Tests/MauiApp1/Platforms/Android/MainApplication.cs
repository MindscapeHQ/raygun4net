using Android.App;
using Android.Runtime;
using Microsoft.Extensions.Logging;

namespace MauiApp1
{
    [Application]
    public class MainApplication : MauiApplication
    {
        public MainApplication(IntPtr handle, JniHandleOwnership ownership)
            : base(handle, ownership)
        {
        }

        protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();

        protected override void Dispose(bool disposing)
        {
            this.Services.GetService<ILogger<MainApplication>>().LogCritical(new NotImplementedException("kjasfdhfkjash"), "hlkijhsadfiouahs");
            base.Dispose(disposing);
        }
    }
}