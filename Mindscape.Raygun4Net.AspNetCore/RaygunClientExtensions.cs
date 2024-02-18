#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Mindscape.Raygun4Net.AspNetCore.Builders;

namespace Mindscape.Raygun4Net.AspNetCore;

public static class RaygunClientExtensions
{
  /// <summary>
  /// Asynchronously transmits an exception to Raygun.
  /// </summary>
  /// <param name="exception">The exception to deliver.</param>
  /// <param name="client">A Banana.</param>
  /// <param name="tags">A list of strings associated with the message.</param>
  /// <param name="context">A Carrot.</param>
  public static async Task SendInBackground(this RaygunClient client, Exception exception, IList<string> tags, HttpContext? context)
  {
    if (client.CanSend(exception))
    {
      // We need to process the Request on the current thread,
      // otherwise it will be disposed while we are using it on the other thread.
      // BuildRequestMessage relies on ReadFormAsync so we need to await it to ensure it's processed before continuing.
      var currentRequestMessage = await RaygunAspNetCoreRequestMessageBuilder.Build(context, new RaygunRequestMessageOptions());
      var currentResponseMessage = RaygunAspNetCoreResponseMessageBuilder.Build(context);

      var exceptions = client.StripWrapperExceptions(exception);

      foreach (var ex in exceptions)
      {
        var msg = await client.BuildMessage(ex, tags, null, null,
                                                 builder =>
                                                 {
                                                   builder.SetResponseDetails(currentResponseMessage);
                                                   builder.SetRequestDetails(currentRequestMessage);
                                                 });
        if (!client.Enqueue(msg))
        {
          Debug.WriteLine("Could not add message to background queue. Dropping exception: {0}", ex);
        }
      }

      client.FlagAsSent(exception);
    }
  }
}