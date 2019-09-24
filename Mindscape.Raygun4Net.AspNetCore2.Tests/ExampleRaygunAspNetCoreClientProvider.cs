using Microsoft.AspNetCore.Http;
using Mindscape.Raygun4Net.AspNetCore;

namespace Mindscape.Raygun4Net.AspNetCore2.Tests
{
    public class ExampleRaygunAspNetCoreClientProvider : DefaultRaygunAspNetCoreClientProvider
    {
        public override AspNetCore.RaygunClient GetClient(AspNetCore.RaygunSettings settings, HttpContext context)
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