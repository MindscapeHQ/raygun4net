Raygun4Net.Mvc - Raygun Provider for ASP .NET MVC projects
=============================================================

Where is my app API key?
========================
When you create a new application in your Raygun dashboard, your app API key is displayed at the top of the instructions page.
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

To send unhandled exceptions automatically, use the RaygunHttpModule in web.config in the appropriate way for your application as seen below.
Note that this will also create and attach a custom MVC error filter attribute for sending exceptions to Raygun, so that you don't need to do this.

For system.web:

<httpModules>
  <add name="RaygunErrorModule" type="Mindscape.Raygun4Net.RaygunHttpModule"/>
</httpModules>

For system.webServer:

<modules>
  <add name="RaygunErrorModule" type="Mindscape.Raygun4Net.RaygunHttpModule"/>
</modules>

Anywhere in you code, you can also send exception reports manually simply by creating a new instance of the RaygunClient and call one of the Send or SendInBackground methods.
This is most commonly used to send exceptions caught in a try/catch block.

try
{
  
}
catch (Exception e)
{
  new RaygunClient().SendInBackground(e);
}

Providing a custom RaygunClient to the http module
==================================================

Sometimes when setting up Raygun using the http module to send exceptions automatically, you may need to provide the http module with a custom RaygunClient instance in order to use some of the optional feature described below.
To do this, get your Http Application to implement the IRaygunApplication interface. Implement the GenerateRaygunClient method to return a new (or previously created) RaygunClient instance.
The http module will use the RaygunClient returned from this method to send the unhandled exceptions.
In this method you can setup any additional options on the RaygunClient instance that you need - more information about each feature is described below.

Additional configuration options and features
=============================================

Exclude errors by HTTP status code
----------------------------------

If using the HTTP module, then you can exclude errors by their HTTP status code by providing a comma separated list of status codes to ignore in the configuration.
For example if you wanted to exclude errors that return the "I'm a teapot" response code (http://tools.ietf.org/html/rfc2324), you could use the configuration below.

<RaygunSettings apikey="YOUR_APP_API_KEY" excludeHttpStatusCodes="418" />

Exclude errors that originate from a local origin
-------------------------------------------------

Toggle this boolean and the HTTP module will not send errors to Raygun if the request originated from a local origin.
i.e. A way to prevent local debug/development from notifying Raygun without having to resort to Web.config transforms.

<RaygunSettings apikey="YOUR_APP_API_KEY" excludeErrorsFromLocal="true" />

Remove sensitive request data
-----------------------------

If you have sensitive data in an HTTP request that you wish to prevent being transmitted to Raygun, you can provide lists of possible keys (names) to remove.
Keys to ignore can be specified on the RaygunSettings tag in web.config, (or you can use the equivalent methods on RaygunClient if you are setting things up in code).
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

On a RaygunClient instance, attach an event handler to the SendingMessage event. This event handler will be called just before the RaygunClient sends an exception - either automatically or manually.
The event arguments provide the RaygunMessage object that is about to be sent. One use for this event handler is to add or modify any information on the RaygunMessage.
Another use for this method is to identify exceptions that you never want to send to raygun, and if so, set e.Cancel = true to cancel the send.

Strip wrapper exceptions
------------------------

If you have common outer exceptions that wrap a valuable inner exception which you'd prefer to group by, you can specify these by using the multi-parameter method:

raygunClient.AddWrapperExceptions(typeof(TargetInvocationException));

In this case, if a TargetInvocationException occurs, it will be removed and replaced with the actual InnerException that was the cause.
Note that HttpUnhandledException and TargetInvocationException are already added to the wrapper exception list; you do not have to add these manually.
This method is useful if you have your own custom wrapper exceptions, or a framework is throwing exceptions using its own wrapper.

Unique (affected) user tracking
-------------------------------

There is a property named *User* on RaygunClient which you can set to be the current user's ID or email address.
This allows you to see the count of affected users for each error in the Raygun dashboard.
If you provide an email address, and the user has an associated Gravatar, you will see their avatar in the error instance page.

Make sure to abide by any privacy policies that your company follows when using this feature.

Version numbering
-----------------

By default, Raygun will send the assembly version of your project with each report.
If you need to provide your own custom version value, you can do so by setting the ApplicationVersion property of the RaygunClient (in the format x.x.x.x where x is a positive integer).

Tags and custom data
--------------------

When sending exceptions manually, you can also send an arbitrary list of tags (an array of strings), and a collection of custom data (a dictionary of any objects).
This can be done using the various Send and SendInBackground method overloads.

Breadcrumbs
-----------

Breadcrumbs let you provide logging points in your code that will be collected and sent along with any exception sent to Raygun. This lets you have a better understanding of the events that happened in the system that lead up to the exception.

To record a Breadcrumb you can use the RecordBreadcrumb(string message) method to record a breadcrumb with a custom message but other properties set to default. If you wish to customize the other properties such as Level, Category or attach CustomData to the Breadcrumb you can construct a Breadcrumb object your self and pass it to RecordBreadcrumb.

There are two settings related to Breadcrumbs in the RaygunSettings configuration, "breadcrumbsLevel" which can be set to Debug, Info, Warning, or Error and is the same as a log level in something like log4net and "breadcrumbsLocationRecordingEnabled" which when set to true will include the class and method where the breadcrumb was logged from. This has a performance cost associated with it so it is only recommended to be enabled when needed.

WebApi support
==============

Do you also need WebApi Raygun support for your project? Simply install the Mindscape.Raygun4Net.WebApi NuGet package which will work happily with this MVC package.
The WebApi package is able to send additional exceptions to Raygun that occur in WebApi projects and can only be detected in specific ways which the package will do for you.

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
