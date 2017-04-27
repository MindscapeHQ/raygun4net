Raygun4Net.AspNetCore - Raygun Provider for ASP.NET Core projects
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

In your project.json file, add "Mindscape.Raygun4Net.AspNetCore": "5.3.1.0" to your dependencies.

Run dotnet.exe restore or restore packages within Visual Studio to download the package.

Add the following code to your appsettings.json (if you're using another type of config, add it there).

"RaygunSettings": {
  "ApiKey": "YOUR_APP_API_KEY"
}

To configure the RaygunAspNetCoreMiddleware to handle exceptions that have been triggered and send unhandled exceptions automatically.

In Startup.cs:
  1. Add using Mindscape.Raygun4Net; to your using statements.
  2. Add app.UseRaygun(); to the Configure method after any other ExceptionHandling methods e.g. app.UseDeveloperExceptionPage() or app.UseExceptionHandler("/Home/Error").
  3. Add services.AddRaygun(Configuration); to the ConfigureServices method.

Anywhere in your code, you can also send exception reports manually simply by creating a new instance of the RaygunClient and calling one of the Send or SendInBackground methods.
This is most commonly used to send exceptions caught in a try/catch block.

try
{
}
catch (Exception e)
{
  new RaygunClient("YOUR_APP_API_KEY").SendInBackground(e);
} 

Configure RaygunClient or settings in RaygunAspNetCoreMiddleware
================================================================

The AddRaygun method has an overload that takes a RaygunMiddlewareSettings object. 
These settings control the middleware (not to be confused with RaygunSettings which are the common settings we use across all of our .NET providers). 
Currently there's just one property on it, ClientProvider. This gives you a hook into the loading of RaygunSettings and the construction of the RaygunAspNetCoreClient used to send errors.

For example, say you want to set user details on your error reports. You'd create a custom client provider like this:

public class ExampleRaygunAspNetCoreClientProvider : DefaultRaygunAspNetCoreClientProvider
{
  public override RaygunClient GetClient(RaygunSettings settings, HttpContext context)
  {
    var client = base.GetClient(settings, context);
    client.ApplicationVersion = "1.1.0";

    var identity = context?.User?.Identity as ClaimsIdentity;
    if (identity?.IsAuthenticated == true)
    {
      var email = identity.Claims.Where(c => c.Type == ClaimTypes.Email).Select(c => c.Value).FirstOrDefault();

      client.UserInfo = new RaygunIdentifierMessage(email)
      {
        IsAnonymous = false,
        Email = email,
        FullName = identity.Name
      };
    }

    return client;
  }
}

Then you would change your services.AddRaygun(Configuration) call in ConfigureServices to this:

services.AddRaygun(Configuration, new RaygunMiddlewareSettings()
{
  ClientProvider = new ExampleRaygunAspNetCoreClientProvider()
});

Manually sending exceptions with a custom ClientProvider
========================================================

When configuring a custom ClientProvider you will also want to leverage this ClientProvider to get an instance of the RaygunClient when manually sending an exception.
To do this use the Dependency Injection framework to provide an instance of the IRaygunAspNetCoreClientProvider and IOptions<RaygunSettings> to your MVC Controller.
This will then ensure that the Raygun crash report also contains any HttpContext information and will execute any code defined in your ClientProvider.GetClient() method.

public class RaygunController : Controller
{
  private readonly IRaygunAspNetCoreClientProvider _clientProvider;
  private readonly IOptions<RaygunSettings> _settings;

  public RaygunController(IRaygunAspNetCoreClientProvider clientProvider, IOptions<RaygunSettings> settings)
  {
    _clientProvider = clientProvider;
    _settings = settings;
  }

  public async Task<IActionResult> TestManualError()
  {
    try
    {
      throw new Exception("Test from .NET Core MVC app");
    }
    catch (Exception ex)
    {
      var raygunClient = _clientProvider.GetClient(_settings.Value, HttpContext);
      await raygunClient.SendInBackground(ex);
    }

    return View();
  }
}

Additional configuration options and features
=============================================

Replace unseekable request streams
----------------------------------

Raygun will try to send the raw request payload with each exception report where applicable, but this is only possible with request streams that are seekable.
If you are not seeing any raw request payloads in your exception reports where you'd expect to see them, then you can set the ReplaceUnseekableRequestStreams setting to true in your appsettings.json. This will attempt to replace any unseekable streams with a seekable copy on the request object so that Raygun can later read the raw request payload.

"RaygunSettings": {
  "ApiKey": "YOUR_APP_API_KEY",
  "ReplaceUnseekableRequestStreams": true
}

This setting is false by default to avoid breaking changes or any unforseen issues with its initial deployment.

Raygun will not attempt to send raw request payloads for GET requests, "x-www-form-urlencoded" requests or "text/html" requests.

Exclude errors by HTTP status code
----------------------------------

You can exclude errors by their HTTP status code by providing an array of status codes to ignore in the configuration.
For example if you wanted to exclude errors that return the "I'm a teapot" response code (http://tools.ietf.org/html/rfc2324), you could use the configuration below.

"RaygunSettings": {
  "ApiKey": "YOUR_APP_API_KEY",
  "ExcludedStatusCodes": [418]
}

Exclude errors that originate from a local origin
-------------------------------------------------

Toggle this boolean and Raygun will not send errors if the request originated from a local origin.
i.e. A way to prevent local debug/development from notifying Raygun without having to resort to Web.config transforms.

"RaygunSettings": {
  "ApiKey": "YOUR_APP_API_KEY",
  "ExcludeErrorsFromLocal": true
}

Remove sensitive request data
-----------------------------

If you have sensitive data in an HTTP request that you wish to prevent being transmitted to Raygun, you can provide lists of possible keys (names) to remove.
Keys to ignore can be specified on the RaygunSettings in appsettings.json, (or you can use the equivalent methods on RaygunClient if you are setting things up in code).
The available options are:

IgnoreFormFieldNames
IgnoreHeaderNames
IgnoreCookieNames
IgnoreServerVariableNames

These can be set to an array of keys to ignore. Setting an option as * will indicate that all the keys will not be sent to Raygun.
Placing * before, after or at both ends of a key will perform an ends-with, starts-with or contains operation respectively.
For example, IgnoreFormFieldNames: ["*password*"] will cause Raygun to ignore all form fields that contain "password" anywhere in the name.
These options are not case sensitive.

Modify or cancel message
------------------------

On a RaygunClient instance, attach an event handler to the SendingMessage event. This event handler will be called just before the RaygunClient sends an exception - either automatically or manually.
The event arguments provide the RaygunMessage object that is about to be sent. One use for this event handler is to add or modify any information on the RaygunMessage.
Another use for this method is to identify exceptions that you never want to send to raygun, and if so, set e.Cancel = true to cancel the send.

Strip wrapper exceptions
------------------------

If you have common outer exceptions that wrap a valuable inner exception which you'd prefer to group by, you can specify these by using the multi-parameter method:

RaygunClient.AddWrapperExceptions(typeof(TargetInvocationException));

In this case, if a TargetInvocationException occurs, it will be removed and replaced with the actual InnerException that was the cause.
Note that TargetInvocationException is already added to the wrapper exception list; you do not have to add this manually.
This method is useful if you have your own custom wrapper exceptions, or a framework is throwing exceptions using its own wrapper.

Unique (affected) user tracking
-------------------------------

There are properties named *User* and *UserInfo* on RaygunClient which you can set to provide user info such as ID and email address
This allows you to see the count of affected users for each error in the Raygun dashboard.
If you provide an email address, and the user has an associated Gravatar, you will see their avatar in the error instance page.

Make sure to abide by any privacy policies that your company follows when using this feature.

Version numbering
-----------------

You can provide an application version value by setting the ApplicationVersion property of the RaygunClient (in the format x.x.x.x where x is a positive integer).

Tags and custom data
--------------------

When sending exceptions manually, you can also send an arbitrary list of tags (an array of strings), and a collection of custom data (a dictionary of any objects).
This can be done using the various Send and SendInBackground method overloads.
