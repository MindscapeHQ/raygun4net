using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Text;
using Mindscape.Raygun4Net.Messages;
using UnityEngine;

namespace Mindscape.Raygun4Net
{
  public class RaygunClient
  {
    private readonly string _apiKey;

    private string _user;
    private string _applicationVersion;

    /// <summary>
    /// Initializes a new instance of the <see cref="RaygunClient" /> class.
    /// </summary>
    /// <param name="apiKey">The API key.</param>
    public RaygunClient(string apiKey)
    {
      _apiKey = apiKey;
    }

    private bool ValidateApiKey()
    {
      if (string.IsNullOrEmpty(_apiKey))
      {
        System.Diagnostics.Debug.WriteLine("ApiKey has not been provided, exception will not be logged");
        return false;
      }
      return true;
    }

    /// <summary>
    /// Gets or sets the user identity string.
    /// </summary>
    public string User
    {
      get { return _user; }
      set
      {
        _user = value;
      }
    }

    /// <summary>
    /// Gets or sets a custom application version identifier for all error messages sent to the Raygun.io endpoint.
    /// </summary>
    public string ApplicationVersion
    {
      get { return _applicationVersion; }
      set
      {
        _applicationVersion = value;
      }
    }

    /// <summary>
    /// Transmits an exception to Raygun.io synchronously.
    /// </summary>
    /// <param name="exception">The exception to deliver.</param>
    public void Send(Exception exception)
    {
      Send(exception, null, (IDictionary)null);
    }

    /// <summary>
    /// Transmits an exception to Raygun.io synchronously specifying a list of string tags associated
    /// with the message for identification, as well as sending a key-value collection of custom data.
    /// </summary>
    /// <param name="exception">The exception to deliver.</param>
    /// <param name="tags">A list of strings associated with the message.</param>
    /// <param name="userCustomData">A key-value collection of custom data that will be added to the payload.</param>
    public void Send(Exception exception, IList<string> tags, IDictionary userCustomData)
    {
      Send(BuildMessage(exception, tags, userCustomData));
    }

    internal RaygunMessage BuildMessage(Exception exception, IList<string> tags, IDictionary userCustomData)
    {
      //exception = StripWrapperExceptions(exception)

      RaygunMessage message = RaygunMessageBuilder.New
        .SetEnvironmentDetails()
        .SetMachineName(Environment.MachineName)
        .SetExceptionDetails(exception)
        .SetClientDetails()
        .SetVersion(ApplicationVersion)
        .SetTags(tags)
        .SetUserCustomData(userCustomData)
        .SetUser(User)
        .Build();
      return message;
    }

    /// <summary>
    /// Posts a RaygunMessage to the Raygun.io api endpoint.
    /// </summary>
    /// <param name="raygunMessage">The RaygunMessage to send. This needs its OccurredOn property
    /// set to a valid DateTime and as much of the Details property as is available.</param>
    public void Send(RaygunMessage raygunMessage)
    {
      if (ValidateApiKey())
      {
        string message = null;

        try
        {
          message = SimpleJson.SerializeObject(raygunMessage);
        }
        catch (Exception ex)
        {
          System.Diagnostics.Debug.WriteLine(string.Format("Error serializing raygun message: {0}", ex.Message));
        }

        if (message != null)
        {
          SendMessage(message);
        }
      }
    }

    private bool SendMessage(string message)
    {
      //WWWForm form = new WWWForm();
      //form.headers.Add("X-ApiKey", _apiKey);

      //byte[] data = Encoding.ASCII.GetBytes(message);
      //WWW www = new WWW(RaygunSettings.Settings.ApiEndpoint.AbsolutePath, data, form.headers);

      byte[] data = Encoding.ASCII.GetBytes(message);
      Hashtable table = new Hashtable();
      table.Add("X-ApiKey", "ec4ofSDUPCXa6U5G3uYsEA==");
      WWW www = new WWW("https://api.raygun.io/entries", data, table);

      //StartCoroutine(WaitForRequest(www));
      /*using (WebClient client = new WebClient())
      {
        client.Headers.Add("X-ApiKey", _apiKey);
        client.Encoding = System.Text.Encoding.UTF8;

        try
        {
          client.UploadString(RaygunSettings.Settings.ApiEndpoint, message);
        }
        catch (Exception e)
        {
          System.Diagnostics.Debug.WriteLine(string.Format("Error Logging Exception to Raygun.io {0}", e.Message));
          return false;
        }
      }*/
      return true;
    }

    private IEnumerator WaitForRequest(WWW www)
    {
      // check for errors
      if (www.error == null)
      {
        Debug.Log("WWW Ok!: " + www.text);
      } else {
        Debug.Log("WWW Error: "+ www.error);
      }
      yield return www;
    }
  }
}
