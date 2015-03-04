using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Reflection;
using System.Web;
using Mindscape.Raygun4Net.Builders;
using Mindscape.Raygun4Net.Messages;

namespace Mindscape.Raygun4Net
{
  public class RaygunMessageBuilder : IRaygunMessageBuilder
  {
    public static RaygunMessageBuilder New
    {
      get
      {
        return new RaygunMessageBuilder();
      }
    }

    private readonly RaygunMessage _raygunMessage;

    private RaygunMessageBuilder()
    {
      _raygunMessage = new RaygunMessage();
    }

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
      }
      
      /*HttpException error = exception as HttpException;
      if (error != null)
      {
        int code = error.GetHttpCode();
        string description = null;
        if (Enum.IsDefined(typeof(HttpStatusCode), code))
        {
          description = ((HttpStatusCode)code).ToString();
        }
        _raygunMessage.Details.Response = new RaygunResponseMessage() { StatusCode = code, StatusDescription = description };
      }*/

      /*WebException webError = exception as WebException;
      if (webError != null)
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
      }*/

      return this;
    }

    public IRaygunMessageBuilder SetClientDetails()
    {
      _raygunMessage.Details.Client = new RaygunClientMessage()
      {
        // This is for the MVC project to set the correct client name - due to the client message class being in the core assembly.
        Name = ((AssemblyTitleAttribute)GetType().Assembly.GetCustomAttributes(typeof(AssemblyTitleAttribute), false)[0]).Title
      };
      return this;
    }

    public IRaygunMessageBuilder SetUserCustomData(IDictionary userCustomData)
    {
      _raygunMessage.Details.UserCustomData = userCustomData;
      return this;
    }

    public IRaygunMessageBuilder SetTags(IList<string> tags)
    {
      _raygunMessage.Details.Tags = tags;
      return this;
    }

    public IRaygunMessageBuilder SetUser(RaygunIdentifierMessage user)
    {
      _raygunMessage.Details.User = user;
      return this;
    }

    /*public IRaygunMessageBuilder SetHttpDetails(HttpContext context, RaygunRequestMessageOptions options = null)
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
    }*/

    public IRaygunMessageBuilder SetHttpDetails(RaygunRequestMessage message)
    {
      _raygunMessage.Details.Request = message;
      return this;
    }

    public IRaygunMessageBuilder SetVersion(string version)
    {
      if (!String.IsNullOrEmpty(version))
      {
        _raygunMessage.Details.Version = version;
      }
      else if (!String.IsNullOrEmpty(RaygunSettings.Settings.ApplicationVersion))
      {
        _raygunMessage.Details.Version = RaygunSettings.Settings.ApplicationVersion;
      }
      else
      {
        var entryAssembly = Assembly.GetEntryAssembly();
        if (entryAssembly != null)
        {
          _raygunMessage.Details.Version = entryAssembly.GetName().Version.ToString();
        }
        else
        {
          _raygunMessage.Details.Version = "Not supplied";
        }
      }
      return this;
    }
  }
}