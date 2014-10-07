using System.Threading;
using System.Web;
using Mindscape.Raygun4Net.Messages;
using Mindscape.Raygun4Net.Builders;

namespace Mindscape.Raygun4Net
{
  public class RaygunClient : RaygunClientBase
  {
    private readonly ThreadLocal<RaygunRequestMessage> _currentRequestMessage = new ThreadLocal<RaygunRequestMessage>(() => null);

    /// <summary>
    /// Initializes a new instance of the <see cref="RaygunClient" /> class.
    /// </summary>
    /// <param name="apiKey">The API key.</param>
    public RaygunClient(string apiKey) : base(apiKey)
    {
      AddWrapperExceptions(typeof(HttpUnhandledException));
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RaygunClient" /> class.
    /// Uses the ApiKey specified in the config file.
    /// </summary>
    public RaygunClient()
      : this(RaygunSettings.Settings.ApiKey)
    {
    }

    public override void SendInBackground(System.Exception exception, System.Collections.Generic.IList<string> tags, System.Collections.IDictionary userCustomData)
    {
      if (CanSend(exception))
      {
        // We need to process the HttpRequestMessage on the current thread,
        // otherwise it will be disposed while we are using it on the other thread.
        RaygunRequestMessage currentRequestMessage = BuildRequestMessage();

        ThreadPool.QueueUserWorkItem(c =>
        {
          _currentRequestMessage.Value = currentRequestMessage;
          Send(BuildMessage(exception, tags, userCustomData));
          _currentRequestMessage.Value = null;
        });
        FlagAsSent(exception);
      }
    }

    private RaygunRequestMessage BuildRequestMessage()
    {
      RaygunRequestMessage requestMessage = null;
      HttpContext context = HttpContext.Current;
      if (context != null)
      {
        HttpRequest request = null;
        try
        {
          request = context.Request;
        }
        catch (HttpException) { }

        if (request != null)
        {
          requestMessage = new RaygunRequestMessageBuilder().Build(request, _requestMessageOptions ?? new RaygunRequestMessageOptions());
        }
      }

      return requestMessage;
    }

    protected override IRaygunMessageBuilder BuildMessageCore()
    {
      if (_currentRequestMessage.Value == null)
      {
        return RaygunMessageBuilder.New
          .SetHttpDetails(HttpContext.Current, _requestMessageOptions);
      }

      return RaygunMessageBuilder.New
        .SetHttpDetails(_currentRequestMessage.Value);
    }
  }
}
