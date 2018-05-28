namespace Mindscape.Raygun4Net
{
    public class RaygunClient : RaygunClientBase
    {
        public RaygunClient(string apiKey)
            : this(new RaygunSettings { ApiKey = apiKey })
        {
        }
        
        public RaygunClient(RaygunSettingsBase settings) : base(settings)
        {
        }
    }
}