using Microsoft.AspNetCore.Http;
using System.Collections.Generic;

namespace Mindscape.Raygun4Net.AspNetCore.Tests
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

            client.SendingMessage += (_, args) =>
            {
                args.Message.Details.Tags ??= new List<string>();
                args.Message.Details.Tags.Add("new tag");
            };

            return client;
        }
    }
}