namespace Mindscape.Raygun4Net.AspNetCore.Tests.TestLib
{
    public class RaygunClientFactory
    {
        private static string _apiKey;

        public static void Initialize(string apiKey)
        {
            _apiKey = apiKey;
        }
        
        public static RaygunClient GetClient()
        {
            return new RaygunClient(_apiKey);
        }
    }
}