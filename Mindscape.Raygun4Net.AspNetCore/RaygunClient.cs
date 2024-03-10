#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Mindscape.Raygun4Net.AspNetCore.Builders;

namespace Mindscape.Raygun4Net.AspNetCore;

public class RaygunClient : RaygunClientBase
{
  [Obsolete("Please use the RaygunClient(RaygunSettings settings) constructor instead.")]
  public RaygunClient(string apiKey) : base(new RaygunSettings {ApiKey = apiKey})
  {
  }

  public RaygunClient(RaygunSettings settings, HttpClient? httpClient = null, IRaygunUserProvider? userProvider = null) : base(settings, httpClient, userProvider)
  {
  }

  public RaygunClient(RaygunSettings settings, IRaygunUserProvider? userProvider = null) : base(settings, null, userProvider)
  {
  }

  // ReSharper disable once MemberCanBePrivate.Global
  protected Lazy<RaygunSettings> Settings => new(() => (RaygunSettings) _settings);

  protected override bool CanSend(RaygunMessage? message)
  {
    if (message?.Details?.Response == null)
    {
      return true;
    }

    var settings = Settings.Value;
    if (settings.ExcludedStatusCodes == null)
    {
      return true;
    }

    return !settings.ExcludedStatusCodes.Contains(message.Details.Response.StatusCode);
  }
  /// <summary>
  /// Asynchronously transmits an exception to Raygun with optional Http Request data.
  /// </summary>
  /// <param name="exception">The exception to deliver.</param>
  /// <param name="tags">A List&lt;string&gt; of tags to associate to the exception.</param>
  /// <param name="context">(Optional) The current HttpContext of the request.</param>
  public async Task SendInBackground(Exception exception, IList<string> tags, HttpContext? context = null)
  {
    if (CanSend(exception))
    {
      // We need to process the Request on the current thread,
      // otherwise it will be disposed while we are using it on the other thread.
      // BuildRequestMessage relies on ReadFormAsync so we need to await it to ensure it's processed before continuing.
      var currentRequestMessage = await RaygunAspNetCoreRequestMessageBuilder.Build(context, Settings.Value);
      var currentResponseMessage = RaygunAspNetCoreResponseMessageBuilder.Build(context);

      var exceptions = StripWrapperExceptions(exception);

      foreach (var ex in exceptions)
      {
        var msg = await BuildMessage(ex, tags, customiseMessage: msg =>
        {
          msg.Details.Request = currentRequestMessage;
          msg.Details.Response = currentResponseMessage;
        });

        if (!Enqueue(msg))
        {
          Debug.WriteLine("Could not add message to background queue. Dropping exception: {0}", ex);
        }
      }

      FlagAsSent(exception);
    }
  }
}