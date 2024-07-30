Raygun4Net.AspNetCore - Raygun Provider for ASP.NET Core projects
===================================================================

Where is my app API key?
========================
When you create a new application on your Raygun dashboard, your app API key is displayed at the top of the instructions page.
You can also find the API key by clicking the "Application Settings" button in the side bar of the Raygun dashboard.

Namespace
=========
The main classes can be found in the Mindscape.Raygun4Net namespace.

Setup & Usage
======

In your ASP.NET Core project, add the Raygun4Net.AspNetCore package to your project:

```bash
dotnet add package Mindscape.Raygun4Net.AspNetCore
```

Add Raygun to your services:

  ```csharp
  public void ConfigureServices(IServiceCollection services)
  {
    // Assumes you're configuring Raygun in appsettings.json
    services.AddRaygun(Configuration);
    
    // Or if you're configuring Raygun in code
    services.AddRaygun(settings =>
    {
      settings.ApiKey = "YOUR_APP_API_KEY";
      ...
    });
  }
  ```

Add Raygun to your middleware:

  ```csharp
  public void Configure(IApplicationBuilder app, IHostingEnvironment env)
  {
    // This should be registered early in the pipeline to catch all exceptions
    app.UseRaygun();
  }
  ```

If you're configuring using appsettings.json, add the following to your appsettings.json file:

  ```json
  {
    "RaygunSettings": {
      "ApiKey": "YOUR_APP_API_KEY"
    }
  }
  ```

## Manually sending exceptions

After using `services.AddRaygun(...)`, you can inject `RaygunClient` into your controllers and use it to send exceptions manually.

```csharp
public class MyController : Controller
{
  private readonly RaygunClient _raygunClient;

  public MyController(RaygunClient raygunClient)
  {
    _raygunClient = raygunClient;
  }

  public async Task<IActionResult> TestManualError()
  {
    try
    {
      throw new Exception("Test from .NET Core MVC app");
    }
    catch (Exception ex)
    {
      await _raygunClient.SendInBackground(ex);
    }

    return View();
  }
}
```


## Custom User Provider

By default, Raygun4Net ships with a `DefaultRaygunUserProvider` which will attempt to get the user information from 
the `HttpContext.User` object. This is Opt-In which can be added by calling `services.AddRaygunUserProvider()`

If you want to provide your own implementation of the `IRaygunUserProvider` you 
can do so by creating a class that implements the interface and then adding it to the services during configuration
using `services.AddRaygunUserProvider<MyCustomUserProvider>()`.


```csharp
public class ExampleUserProvider : IRaygunUserProvider
{
  private readonly IHttpContextAccessor _contextAccessor;
  
  public ExampleUserProvider(IHttpContextAccessor httpContextAccessor)
  {
    _contextAccessor = contextAccessor;
  }
  
  public RaygunIdentifierMessage? GetUser()
  {
    var ctx = _contextAccessor.HttpContext;
    
    if (ctx == null)
    {
      return null;
    }

    var identity = ctx.User.Identity as ClaimsIdentity;
    
    if (identity?.IsAuthenticated == true)
    {
      return new RaygunIdentifierMessage(identity.Name)
      {
        IsAnonymous = false
      };
    
    return null;
  }
}
```

This can be registered in the services during configuration like so: 

```csharp
services.AddRaygunUserProvider<ExampleUserProvider>();
```

Make sure to abide by any privacy policies that your company follows when using this feature.

Additional configuration options and features
=============================================

The following features can be configured in the appsettings.json file or in code.

For example, in the appsettings.json file:

```json
{
  "RaygunSettings": {
    "ApiKey": "YOUR_APP_API_KEY",
    "ExcludeErrorsFromLocal": true
  }
}
```

The equivalent in code:

```csharp
services.AddRaygun(settings =>
{
  settings.ApiKey = "YOUR_APP_API_KEY";
  settings.ExcludeErrorsFromLocal = true;
});
```

Examples below are shown in appsettings.json format.

Replace unseekable request streams
----------------------------------

Raygun will try to send the raw request payload with each exception report where applicable, but this is only possible with request streams that are seekable.
If you are not seeing any raw request payloads in your exception reports where you'd expect to see them, then you can set the ReplaceUnseekableRequestStreams setting to true in your appsettings.json. This will attempt to replace any unseekable streams with a seekable copy on the request object so that Raygun can later read the raw request payload.

```json
"RaygunSettings": {
  "ApiKey": "YOUR_APP_API_KEY",
  "ReplaceUnseekableRequestStreams": true
}
```

This setting is false by default to avoid breaking changes or any unforseen issues with its initial deployment.

Raygun will not attempt to send raw request payloads for GET requests, "x-www-form-urlencoded" requests or "text/html" requests.

Exclude errors by HTTP status code
----------------------------------

You can exclude errors by their HTTP status code by providing an array of status codes to ignore in the configuration.
For example if you wanted to exclude errors that return the "I'm a teapot" response code (http://tools.ietf.org/html/rfc2324), you could use the configuration below.

```json
"RaygunSettings": {
  "ApiKey": "YOUR_APP_API_KEY",
  "ExcludedStatusCodes": [418]
}
```

Exclude errors that originate from a local origin
-------------------------------------------------

Toggle this boolean and Raygun will not send errors if the request originated from a local origin.
i.e. A way to prevent local debug/development from notifying Raygun without having to resort to Web.config transforms.

```json
"RaygunSettings": {
  "ApiKey": "YOUR_APP_API_KEY",
  "ExcludeErrorsFromLocal": true
}
```

Modify or cancel message
------------------------

On a RaygunClient instance, attach an event handler to the SendingMessage event. This event handler will be called just before the RaygunClient sends an exception - either automatically or manually.
The event arguments provide the RaygunMessage object that is about to be sent. One use for this event handler is to add or modify any information on the RaygunMessage.
Another use for this method is to identify exceptions that you never want to send to raygun, and if so, set e.Cancel = true to cancel the send.

Strip wrapper exceptions
------------------------

If you have common outer exceptions that wrap a valuable inner exception which you'd prefer to group by, you can specify these by using the multi-parameter method:

```csharp
_raygunClient.AddWrapperExceptions(typeof(TargetInvocationException));
```

In this case, if a TargetInvocationException occurs, it will be removed and replaced with the actual InnerException that was the cause.
Note that TargetInvocationException is already added to the wrapper exception list; you do not have to add this manually.
This method is useful if you have your own custom wrapper exceptions, or a framework is throwing exceptions using its own wrapper.

Version numbering
-----------------

By default, Raygun4Net will attempt to set `ApplicationVersion` from the entry assembly. It is possible to override this by providing a version through `RaygunSettings`:

```json
"RaygunSettings": {
  "ApplicationVersion": "1.0.0.0"
}
```
Or
```cs
services.AddRaygun(settings =>
{
  settings.ApplicationVersion = "1.0.0.0";
});
```

Tags and custom data
--------------------

When sending exceptions manually, you can also send an arbitrary list of tags (an array of strings), and a collection of custom data (a dictionary of any objects).
This can be done using the various SendAsync and SendInBackground method overloads.

Example:
```cs
await _raygunClient.SendAsync(ex, new List<string> { "Tag1", "Tag2" }, new Dictionary<string, string> { { "customKey", "value" } });
```

Remove sensitive request data
-----------------------------

If you have sensitive data in an HTTP request that you wish to prevent being transmitted to Raygun, you can provide lists of possible keys (names) to remove.
Keys to ignore can be specified on the `RaygunSettings` in `appsettings.json`, or on the `RaygunSettings` when you create the `RaygunClient` or call `services.AddRaygun(settings => {...});` 
The available options are:

- `IgnoreSensitiveFieldNames`
- `IgnoreQueryParameterNames`
- `IgnoreFormFieldNames`
- `IgnoreHeaderNames`
- `IgnoreCookieNames`
- `IgnoreServerVariableNames`

These can be set to an array of keys to ignore. Setting an option as `*` will indicate that all the keys will not be sent to Raygun.
Placing `*` before, after or at both ends of a key will perform an ends-with, starts-with or contains operation respectively.
For example, `IgnoreFormFieldNames: ["*password*"]` will cause Raygun to ignore all form fields that contain "password" anywhere in the name.
These options are not case sensitive.

Note: The `IgnoreSensitiveFieldNames` will be applied to ALL fields in the `RaygunRequestMessage`. 

We provide extra options for removing sensitive data from the request raw data. This comes in the form of filters as implemented by the `IRaygunDataFilter` interface.
These filters read the raw data and strip values whose keys match those found in the `RaygunSettings.IgnoreSensitiveFieldNames` property.

We currently provide two implementations with this provider:

1. RaygunKeyValuePairDataFilter e.g. filtering "user=raygun&password=pewpew"
2. RaygunXmlDataFilter e.g. filtering "<password>pewpew</password>"

These filters are initially disabled and can be enbled through the `RaygunSettings` class. 

```cs
services.AddRaygun(settings => 
{
  settings.UseXmlRawDataFilter = true;
  settings.UseKeyValuePairRawDataFilter = true;
  settings.IsRawDataIgnoredWhenFilteringFailed = true;
  settings.RawDataFilters.Add(new RaygunJsonDataFilter()); // Example below
});
```

You may also provide your own implementation of the `IRaygunDataFilter` and pass this to the `RaygunClient` to use when filtering raw data.

### Example JSON Data Filter

```csharp
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
```