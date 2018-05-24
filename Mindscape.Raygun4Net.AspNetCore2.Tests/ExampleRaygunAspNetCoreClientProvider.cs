using Microsoft.AspNetCore.Http;

namespace Mindscape.Raygun4Net.AspNetCore2.Tests
{
    public class ExampleRaygunAspNetCoreClientProvider : DefaultRaygunAspNetCoreClientProvider
    {
        public override RaygunClient GetClient(RaygunSettings settings, HttpContext context)
        {
            var client = base.GetClient(settings, context);

            var email = "bob@raygun.com";

            client.UserInfo = new RaygunIdentifierMessage(email)
            {
                IsAnonymous = false,
                Email = email,
                FullName = "Bob"
            };
            
            return client;
        }
    }
}