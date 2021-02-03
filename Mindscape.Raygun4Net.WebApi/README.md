Raygun4Net.WebApi - Raygun Provider for ASP .NET WebApi projects
===================================================================

Where is my app API key?
========================
When you create a new application on your Raygun dashboard, your app API key is displayed at the top of the instructions page.
You can also find the API key by clicking the "Application Settings" button in the side bar of the Raygun dashboard.

Namespace
=========
The main classes can be found in the Mindscape.Raygun4Net namespace.

Usage
======

Add a section to configSections:

<section name="RaygunSettings" type="Mindscape.Raygun4Net.RaygunSettings, Mindscape.Raygun4Net"/>

Add the Raygun settings configuration block from above:

<RaygunSettings apikey="YOUR_APP_API_KEY" />

Now you can either setup Raygun to send unhandled exceptions automatically or/and send exceptions manually.

To send unhandled exceptions automatically, go to the WebApiConfig class in your project. In the static Register method, call the static RaygunWebApiClient.Attach method.

RaygunWebApiClient.Attach(config);

Anywhere in you code, you can also send exception reports manually simply by creating a new instance of the RaygunWebApiClient and call one of the Send or SendInBackground methods.
This is most commonly used to send exceptions caught in a try/catch block.

try
{
  
}
catch (Exception e)
{
  new RaygunWebApiClient().SendInBackground(e);
}

Providing a custom RaygunClient to the automatic exception handlers
===================================================================

Sometimes when setting up Raygun to send exceptions automatically, you may need to provide a custom RaygunWebApiClient instance in order to use some of the optional feature described below.
To do this, use the static RaygunWebApiClient.Attach method overload that takes a function. Within this function, return a new (or previously created) RaygunWebApiClient instance.
In this function you can setup any additional options on the RaygunWebApiClient instance that you need - more information about each feature is described below.

RaygunWebApiClient.Attach(config, () => {
  var client = new RaygunWebApiClient();
  client.ApplicationVersion = "5.9.0.1";
  client.UserInfo = new RaygunIdentifierMessage("user@example.com");
  client.SendingMessage += (sender, args) =>
  {
    if (args.Message.Details.MachineName == "BadServer")
    {
      args.Cancel = true;
    }
  };
  return client;
});

Additional configuration options and features
=============================================

Exclude errors by HTTP status code
----------------------------------

You can exclude errors by their HTTP status code by providing a comma separated list of status codes to ignore in the configuration.
For example if you wanted to exclude errors that return the "I'm a teapot" response code (http://tools.ietf.org/html/rfc2324), you could use the configuration below.

<RaygunSettings apikey="YOUR_APP_API_KEY" excludeHttpStatusCodes="418" />

Exclude errors that originate from a local origin
-------------------------------------------------

Toggle this boolean and Raygun will not send errors to Raygun if the request originated from a local origin.
i.e. A way to prevent local debug/development from notifying Raygun without having to resort to Web.config transforms.

<RaygunSettings apikey="YOUR_APP_API_KEY" excludeErrorsFromLocal="true" />

Remove sensitive request data
-----------------------------

If you have sensitive data in an HTTP request that you wish to prevent being transmitted to Raygun, you can provide lists of possible keys (names) to remove.
Keys to ignore can be specified on the RaygunSettings tag in web.config, (or you can use the equivalent methods on RaygunWebApiClient if you are setting things up in code).
The available options are:

ignoreSensitiveFieldNames
ignoreQueryParameterNames
ignoreFormFieldNames
ignoreHeaderNames
ignoreCookieNames
ignoreServerVariableNames

These can be set to be a comma separated list of keys to ignore. Setting an option as * will indicate that all the keys will not be sent to Raygun.
Placing * before, after or at both ends of a key will perform an ends-with, starts-with or contains operation respectively.
For example, ignoreFormFieldNames="*password*" will cause Raygun to ignore all form fields that contain "password" anywhere in the name.
These options are not case sensitive.

Note: The IgnoreSensitiveFieldNames will be applied to ALL fields in the RaygunRequestMessage. 

We provide extra options for removing sensitive data from the request raw data. This comes in the form of filters as implemented by the IRaygunDataFilter interface.
These filters read the raw data and strip values whose keys match those found in the RaygunSettings IgnoreSensitiveFieldNames property.

We currently provide two implementations with this provider.

RaygunKeyValuePairDataFilter e.g. filtering "user=raygun&password=pewpew"
RaygunXmlDataFilter e.g. filtering "<password>pewpew</password>"

These filters are initially disabled and can be enbled through the RaygunSettings class. You may also provide your own implementation of the IRaygunDataFilter and pass this to the RaygunClient to use when filtering raw data. An example for implementing an JSON filter can be found at the end of this readme.

Modify or cancel message
------------------------

On a RaygunWebApiClient instance, attach an event handler to the SendingMessage event. This event handler will be called just before the RaygunWebApiClient sends an exception - either automatically or manually.
The event arguments provide the RaygunMessage object that is about to be sent. One use for this event handler is to add or modify any information on the RaygunMessage.
Another use for this method is to identify exceptions that you never want to send to raygun, and if so, set e.Cancel = true to cancel the send.

Strip wrapper exceptions
------------------------

If you have common outer exceptions that wrap a valuable inner exception which you'd prefer to group by, you can specify these by using the multi-parameter method:

raygunWebApiClient.AddWrapperExceptions(typeof(TargetInvocationException));

In this case, if a TargetInvocationException occurs, it will be removed and replaced with the actual InnerException that was the cause.
Note that TargetInvocationException is already added to the wrapper exception list; you do not have to add this manually.
This method is useful if you have your own custom wrapper exceptions, or a framework is throwing exceptions using its own wrapper.

Customers
-------------------------------

There are properties named *User* and *UserInfo* on RaygunWebApiClient which you can set to provide customer info such as ID and email address
This allows you to see the count of affected customers for each error in the Raygun dashboard.
If you provide an email address, and the customer has an associated Gravatar, you will see their avatar in the error instance page.

Make sure to abide by any privacy policies that your company follows when using this feature.

Version numbering
-----------------

By default, Raygun will send the assembly version of your project with each report.
If you need to provide your own custom version value, you can do so by setting the ApplicationVersion property of the RaygunWebApiClient (in the format x.x.x.x where x is a positive integer).

Tags and custom data
--------------------

When sending exceptions manually, you can also send an arbitrary list of tags (an array of strings), and a collection of custom data (a dictionary of any objects).
This can be done using the various Send and SendInBackground method overloads.

MVC support
===========

Do you also need MVC Raygun support for your project? Simply install the Mindscape.Raygun4Net.Mvc NuGet package which will work happily with this WebApi package.
The MVC package includes an http module that will set up an MVC exception filter which can send exceptions to Raygun that could otherwise be missed in MVC projects.

Example JSON Data Filter
========================

using System;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Mindscape.Raygun4Net.Filters;

public class RaygunJsonDataFilter : IRaygunDataFilter
{
  private const string FILTERED_VALUE = "[FILTERED]";

  public bool CanParse(string data)
  {
    if (!string.IsNullOrEmpty(data))
    {
      int index = data.TakeWhile(c => char.IsWhiteSpace(c)).Count();
      if (index < data.Length)
      {
        if (data.ElementAt(index).Equals('{'))
        {
          return true;
        }
      }
    }
    return false;
  }

  public string Filter(string data, IList<string> ignoredKeys)
  {
    try
    {
      JObject jObject = JObject.Parse(data);

      FilterTokensRecursive(jObject.Children(), ignoredKeys);

      return jObject.ToString(Formatting.None, null);
    }
    catch
    {
      return null;
    }
  }

  private void FilterTokensRecursive(IEnumerable<JToken> tokens, IList<string> ignoredKeys)
  {
    foreach (JToken token in tokens)
    {
      if (token is JProperty)
      {
        var property = token as JProperty;

        if (ShouldIgnore(property, ignoredKeys))
        {
          property.Value = FILTERED_VALUE;
        }
        else if (property.Value.Type == JTokenType.Object)
        {
          FilterTokensRecursive(property.Value.Children(), ignoredKeys);
        }
      }
    }
  }

  private bool ShouldIgnore(JProperty property, IList<string> ignoredKeys)
  {
    bool hasValue = property.Value.Type != JTokenType.Null;

    if (property.Value.Type == JTokenType.String)
    {
      hasValue = !string.IsNullOrEmpty(property.Value.ToString());
    }

    return hasValue && !string.IsNullOrEmpty(property.Name) && ignoredKeys.Any(f => f.Equals(property.Name, StringComparison.OrdinalIgnoreCase));
  }
}
