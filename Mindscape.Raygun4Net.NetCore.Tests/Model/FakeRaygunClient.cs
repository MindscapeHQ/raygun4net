using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework.Internal;

namespace Mindscape.Raygun4Net.NetCore.Tests
{
    public class FakeRaygunClient : RaygunClient
    {
        public FakeRaygunClient()
         : base(string.Empty)
        {
            
        }
        public FakeRaygunClient(string apiKey)
            : base(apiKey)
        {
        }

        public RaygunMessage ExposeBuildMessage(Exception exception, [Optional] IList<string> tags, [Optional] IDictionary userCustomData)
        {
            var task = BuildMessage(exception, tags, userCustomData);
            
            task.Wait();

            return task.Result;
        }
        
        public bool ExposeValidateApiKey()
        {
            return ValidateApiKey();
        }

        public bool ExposeOnSendingMessage(RaygunMessage raygunMessage)
        {
            return OnSendingMessage(raygunMessage);
        }

        public bool ExposeCanSend(Exception exception)
        {
            return CanSend(exception);
        }

        public void ExposeFlagAsSent(Exception exception)
        {
            FlagAsSent(exception);
        }
    }
}
