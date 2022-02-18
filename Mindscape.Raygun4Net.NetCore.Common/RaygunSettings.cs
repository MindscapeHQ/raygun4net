using System;

namespace Mindscape.Raygun4Net
{
    public abstract class RaygunSettingsBase
    {
        internal const string DefaultApiEndPoint = "https://api.raygun.com/entries";

        public RaygunSettingsBase()
        {
            ApiEndpoint = new Uri(DefaultApiEndPoint);
        }

        public string ApiKey { get; set; }

        public Uri ApiEndpoint { get; set; }

        public bool ThrowOnError { get; set; }

        public string ApplicationVersion { get; set; }

        /// <summary>
        /// If set to true will automatically setup handlers to catch Unhandled Exceptions  
        /// </summary>
        /// <remarks>
        /// Currently defaults to false. This may be change in future releases.
        /// </remarks>
        public bool CatchUnhandledExceptions { get; set; } = false;
    }
}
