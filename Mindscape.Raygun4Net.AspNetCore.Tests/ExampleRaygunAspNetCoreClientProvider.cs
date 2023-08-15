using Microsoft.AspNetCore.Http;
using Mindscape.Raygun4Net.AspNetCore;
using System.Collections.Generic;

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

            client.SendingMessage += (_, args) =>
            {
                args.Message.Details.Tags ??= new List<string>();
                args.Message.Details.Tags.Add("new tag");
            };

            return client;
        }
    }
}