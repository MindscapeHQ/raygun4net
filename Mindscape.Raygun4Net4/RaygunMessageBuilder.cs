using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Web;
using Mindscape.Raygun4Net.Builders;
using Mindscape.Raygun4Net.Logging;
using Mindscape.Raygun4Net.Messages;

namespace Mindscape.Raygun4Net
{
  public class RaygunMessageBuilder : IRaygunMessageBuilder
  {
    private static string LookupRaygunClientName(Type builderType)
    {
      var lastRaygunClientName = _lastRaygunClientName;
      if (lastRaygunClientName is null || !ReferenceEquals(lastRaygunClientName.Item1, builderType))
      {
        // The MVC provider references the Raygun4Net4 provider, so this is a special case to get the correct client name:
        var clientName  = AppDomain.CurrentDomain.GetAssemblies().Any(a => a.FullName.StartsWith("Mindscape.Raygun4Net.Mvc"))
          ? "Raygun4Net.Mvc"
          : ((AssemblyTitleAttribute)builderType.Assembly.GetCustomAttributes(typeof(AssemblyTitleAttribute), false)[0]).Title;
        _lastRaygunClientName = lastRaygunClientName = new Tuple<Type, string>(builderType, clientName);
      }
      return lastRaygunClientName.Item2;
    }
    private static Tuple<Type, string> _lastRaygunClientName;

    public static RaygunMessageBuilder New => new RaygunMessageBuilder();

    private readonly RaygunMessage _raygunMessage = new RaygunMessage();

    private RaygunMessageBuilder() { }

    public RaygunMessage Build()
    {
      return _raygunMessage;
    }

    public IRaygunMessageBuilder SetMachineName(string machineName)
    {
      _raygunMessage.Details.MachineName = machineName;
      return this;
    }

    public IRaygunMessageBuilder SetEnvironmentDetails()
    {
      _raygunMessage.Details.Environment = RaygunEnvironmentMessageBuilder.Build();
      return this;
    }

    public IRaygunMessageBuilder SetExceptionDetails(Exception exception)
    {
      if (exception != null)
      {
        _raygunMessage.Details.Error = RaygunErrorMessageBuilder.Build(exception);

        if (_raygunMessage.Details.Error != null)
        {
          AssignCorrelationId(_raygunMessage.Details);
        }
      }

      if (exception is HttpException error)
      {
        int code = error.GetHttpCode();
        string description = null;
        if (Enum.IsDefined(typeof(HttpStatusCode), code))
        {
          description = ((HttpStatusCode)code).ToString();
        }

        _raygunMessage.Details.Response = new RaygunResponseMessage() { StatusCode = code, StatusDescription = description };
      }

      try
      {
        if (exception is WebException webError)
        {
          if (webError.Status == WebExceptionStatus.ProtocolError && webError.Response is HttpWebResponse)
          {
            HttpWebResponse response = (HttpWebResponse)webError.Response;
            _raygunMessage.Details.Response = new RaygunResponseMessage() { StatusCode = (int)response.StatusCode, StatusDescription = response.StatusDescription };
          }
          else if (webError.Status == WebExceptionStatus.ProtocolError && webError.Response is FtpWebResponse)
          {
            FtpWebResponse response = (FtpWebResponse)webError.Response;
            _raygunMessage.Details.Response = new RaygunResponseMessage() { StatusCode = (int)response.StatusCode, StatusDescription = response.StatusDescription };
          }
          else
          {
            _raygunMessage.Details.Response = new RaygunResponseMessage() { StatusDescription = webError.Status.ToString() };
          }
        }
      }
      catch (Exception ex)
      {
        RaygunLogger.Instance.Error($"Error retrieving response info {ex.Message}");
      }

      return this;
    }

    private void AssignCorrelationId(RaygunMessageDetails details)
    {
      if (details != null && details.Error != null)
      {
        details.CorrelationId = GenerateCorrelationId(details.Error.ClassName);
      }
    }

    private string GenerateCorrelationId(string className)
    {
      return Guid.NewGuid().ToString();
    }

    public IRaygunMessageBuilder SetClientDetails()
    {
      _raygunMessage.Details.Client = new RaygunClientMessage()
      {
        // RaygunClientMessage is in core, so this message builder overrides the Name to get the correct client name.
        Name = LookupRaygunClientName(GetType())
      };

      return this;
    }

    public IRaygunMessageBuilder SetUserCustomData(IDictionary userCustomData)
    {
      _raygunMessage.Details.UserCustomData = userCustomData ?? new Dictionary<string, string>();
      return this;
    }

    public IRaygunMessageBuilder SetTags(IList<string> tags)
    {
      _raygunMessage.Details.Tags = tags ?? new List<string>();
      return this;
    }

    public IRaygunMessageBuilder SetUser(RaygunIdentifierMessage user)
    {
      _raygunMessage.Details.User = user;
      return this;
    }

    public IRaygunMessageBuilder SetHttpDetails(HttpContext context, RaygunRequestMessageOptions options = null)
    {
      if (context != null)
      {
        HttpRequest request;
        try
        {
          request = context.Request;
        }
        catch (HttpException)
        {
          return this;
        }

        _raygunMessage.Details.Request = RaygunRequestMessageBuilder.Build(request, options);
      }

      return this;
    }

    public IRaygunMessageBuilder SetHttpDetails(RaygunRequestMessage message)
    {
      _raygunMessage.Details.Request = message;
      return this;
    }

    public IRaygunMessageBuilder SetVersion(string version)
    {
      if (!string.IsNullOrEmpty(version))
      {
        _raygunMessage.Details.Version = version;
        return this;
      }

      if (!string.IsNullOrEmpty(RaygunSettings.Settings.ApplicationVersion))
      {
        _raygunMessage.Details.Version = RaygunSettings.Settings.ApplicationVersion;
      }
      else
      {
        var entryAssembly = Assembly.GetEntryAssembly() ?? GetWebEntryAssembly() ?? Assembly.GetExecutingAssembly();
        _raygunMessage.Details.Version = entryAssembly.GetName().Version.ToString();
      }

      return this;
    }

    private static Assembly GetWebEntryAssembly()
    {
      if (HttpContext.Current == null || HttpContext.Current.ApplicationInstance == null)
      {
        return null;
      }

      var type = HttpContext.Current.ApplicationInstance.GetType();
      while (type != null && "ASP".Equals(type.Namespace))
      {
        type = type.BaseType;
      }

      return type?.Assembly;
    }

    public IRaygunMessageBuilder SetTimeStamp(DateTime? currentTime)
    {
      if (currentTime != null)
      {
        _raygunMessage.OccurredOn = currentTime.Value;
      }

      return this;
    }

    public IRaygunMessageBuilder SetBreadcrumbs(List<RaygunBreadcrumb> breadcrumbs)
    {
      _raygunMessage.Details.Breadcrumbs = breadcrumbs;

      return this;
    }

    public IRaygunMessageBuilder SetContextId(string contextId)
    {
      _raygunMessage.Details.ContextId = contextId;

      return this;
    }
    
    public IRaygunMessageBuilder Customise(Action<RaygunMessage> customiseAction)
    {
      customiseAction?.Invoke(_raygunMessage);
      return this;
    }
  }
}