Raygun4Net
==========

[Raygun](http://raygun.com) provider for .NET Framework

Supported platforms/frameworks
====================

Projects built with the following frameworks are supported:

* .NET 4.0+, up to .NET 7.0
* .NET 4.0 Client Profile
* .NET Core 1.0, 2.0, 3.0+
* ASP.NET
* ASP.NET MVC
* ASP.NET WebApi
* WinForms, WPF, console apps etc
* Xamarin.iOS and Xamarin.Mac (Both unified and classic)
* Xamarin.Android

Install the NuGet package to a project which uses one of the above frameworks and the correct assembly will be referenced.

For Xamarin we recommended adding either [Raygun4Xamarin.Forms](https://www.nuget.org/packages/Raygun4Xamarin.Forms/), or the [Mindscape.Raygun4Net.Xamarin.Android](https://www.nuget.org/packages/Mindscape.Raygun4Net.Xamarin.Android/) & [Mindscape.Raygun4Net.Xamarin.iOS.Unified](https://www.nuget.org/packages/Mindscape.Raygun4Net.Xamarin.iOS.Unified/) packages.

For .NET Core and .NET versions 5.0 or higher, we recommend using the [Mindscape.Raygun4Net.NetCore](https://www.nuget.org/packages/Mindscape.Raygun4Net.NetCore/) package.


Installation
====================

* The easiest way to install this provider is by either using the below dotnet CLI command, or in the NuGet management GUI in the IDE you use.

``` sh
dotnet add package Mindscape.Raygun4Net
```

---
See the [Raygun docs](https://raygun.com/documentation/language-guides/dotnet/crash-reporting/) for more detailed instructions on how to use this provider.

Where is my app API key?
====================

When sending exceptions to the Raygun service, an app API key is required to map the messages to your application.

When you create a new application in your Raygun dashboard, your app API key is displayed within the instructions page. You can also find the API key by clicking the "Application Settings" button in the side bar of the Raygun dashboard.

Namespace
====================
The main classes can be found in the Mindscape.Raygun4Net namespace.

Usage
====================

The Raygun4Net provider includes support for many .NET frameworks. Scroll down to find information about using Raygun for your type of application.

### ASP.NET 5+

As of version 5.0.0, ASP.NET support has been moved into a new NuGet package.
If you have a ASP.NET project, please uninstall the Raygun4Net NuGet package and install the Mindscape.Raygun4Net.AspNetCore NuGet package instead.

Once the package is installed, add the following code to your appsettings.json (if you're using another type of config, add it there):

```json
"RaygunSettings": {
  "ApiKey":  "YOUR_APP_API_KEY"
}
```

Configure the Raygun Middleware to handle exceptions that have been triggered and send unhandled exceptions automatically.

In `Program.cs`:

1. Add `using Mindscape.Raygun4Net.AspNetCore;` to your using statements.
2. Add `builder.Services.AddRaygun(builder.Configuration);`.
3. Add `app.UseRaygun();` after any other ExceptionHandling methods e.g. `app.UseDeveloperExceptionPage()` or `app.UseExceptionHandler("/Home/Error")`.

```csharp
using Mindscape.Raygun4Net.AspNetCore;
```

```csharp
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRaygun(builder.Configuration);
  
/*The rest of your builder setup*/

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");

    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.MapGet("/throw", (Func<string>)(() => throw new Exception("Exception in request pipeline")));

app.UseRaygun();

/*The rest of your app setup*/
```

The above set up will cause all unhandled exceptions to be sent to your Raygun account, where you can easily view all of your error monitoring and crash report data.

#### TLS configuration for .NET 3.5 or earlier

Raygun's ingestion nodes require TLS 1.2 or TLS 1.3 If you are using **.NET 3.5 or earlier**, you may need to enable these protocols in your application and patch your OS.


Check out the [TLS troubleshooting guide from Microsoft](https://techcommunity.microsoft.com/t5/azure-paas-blog/ssl-tls-connection-issue-troubleshooting-guide/ba-p/2108065) for their recommendations.

Updating the protocol property in your application's startup code.

```csharp
protected void Application_Start()
  {
    // Enable TLS 1.2 and TLS 1.3
    ServicePointManager.SecurityProtocol |=  ( (SecurityProtocolType)3072 /*TLS 1.2*/ | (SecurityProtocolType)12288 /*TLS 1.3*/  );
  }
```

### ASP.NET Framework

In your Web.config file, find or add a `<configSections>` element, which should be nested under the `<configuration>` element, and add the following entry:

```
<section name="RaygunSettings" type="Mindscape.Raygun4Net.RaygunSettings, Mindscape.Raygun4Net"/>
```

Then reference it by adding the following line somewhere after the `configSections` tag.

```xml
<RaygunSettings apikey="YOUR_APP_API_KEY" />
```

Now you can either setup Raygun to send unhandled exceptions automatically or/and send exceptions manually.

To send unhandled exceptions automatically, use the Raygun HTTP module within the `<configuration>` element in web.config. This is done slightly differently depending on what version of IIS you're using. If in doubt, just try them both:

```xml
<system.web>
  <httpModules>
    <add name="RaygunErrorModule" type="Mindscape.Raygun4Net.RaygunHttpModule"/>
  </httpModules>
</system.web>
```

For IIS 7.0, use `system.webServer`

```xml
<system.webServer>
  <modules>
    <add name="RaygunErrorModule" type="Mindscape.Raygun4Net.RaygunHttpModule"/>
  </modules>
</system.webServer>
```

Anywhere in you code, you can also send exception reports manually simply by creating a new instance of the RaygunClient and call one of the Send or SendInBackground methods.
This is most commonly used to send exceptions caught in a try/catch block.

```csharp
try
{
  
}
catch (Exception e)
{
  new RaygunClient().SendInBackground(e);
}
```

Or to send exceptions in your own handlers rather than using the automatic setup above.

```csharp
protected void Application_Error()
{
  var exception = Server.GetLastError();
  new RaygunClient().Send(exception);
}
```

#### Additional ASP.NET configuration options

**Exclude errors by HTTP status code**

If using the HTTP module then you can exclude errors by their HTTP status code by providing a comma separated list of status codes to ignore in the configuration. For example if you wanted to exclude errors that return the [I'm a teapot](http://tools.ietf.org/html/rfc2324) response code, you could use the configuration below.

```xml
<RaygunSettings apikey="YOUR_APP_API_KEY" excludeHttpStatusCodes="418" />
```

**Exclude errors that originate from a local origin**

Toggle this boolean and the HTTP module will not send errors to Raygun if the request originated from a local origin. i.e. A way to prevent local debug/development from notifying Raygun without having to resort to Web.config transforms.

```xml
<RaygunSettings apikey="YOUR_APP_API_KEY" excludeErrorsFromLocal="true" />
```

**Remove sensitive request data**

If you have sensitive data in an HTTP request that you wish to prevent being transmitted to Raygun, you can provide lists of possible keys (names) to remove.
Keys to ignore can be specified on the RaygunSettings tag in web.config, (or you can use the equivalent methods on RaygunClient if you are setting things up in code).
The available options are:

* ignoreFormFieldNames
* ignoreHeaderNames
* ignoreCookieNames
* ignoreServerVariableNames

These can be set to be a comma separated list of keys to ignore. Setting an option as * will indicate that all the keys will **not** be sent to Raygun.
Placing * before, after or at both ends of a key will perform an ends-with, starts-with or contains operation respectively.
For example, ignoreFormFieldNames="\*password\*" will cause Raygun to ignore all form fields that **contain** "password" anywhere in the name.
These options are **not** case sensitive.

**Providing a custom RaygunClient to the http module**

Sometimes when setting up Raygun using the http module to send exceptions automatically, you may need to provide the http module with a custom RaygunClient instance in order to use some of the optional feature described at the end of this file.
To do this, get your Http Application to implement the IRaygunApplication interface. Implement the GenerateRaygunClient method to return a new (or previously created) RaygunClient instance.
The http module will use the RaygunClient returned from this method to send the unhandled exceptions.
In this method you can setup any additional options on the RaygunClient instance that you need - more information about each feature is described at the end of this file.

### ASP.NET MVC

As of version 4.0.0, Mvc support has been moved into a new NuGet package.
If you have an Mvc project, please uninstall the Raygun4Net NuGet package and install the `Mindscape.Raygun4Net.Mvc` NuGet package instead.

Once the package is installed, see the [package README](/Mindscape.Raygun4Net.Mvc/README.md) for instructions on configuration.

The Mvc and WebApi NuGet packages can be installed in the same project safely.

### ASP.NET Web API

As of version 4.0.0, WebApi support has been moved into a new NuGet package.
If you have a WebApi project, please uninstall the Raygun4Net NuGet package and install the `Mindscape.Raygun4Net.WebApi` NuGet package instead.

Once the package is installed, see the [package README](/Mindscape.Raygun4Net.WebApi/README.md) for instructions on configuration.

The Mvc and WebApi NuGet packages can be installed in the same project safely.

### WPF

Create an instance of RaygunClient by passing your app API key in the constructor. Attach an event handler to the DispatcherUnhandledException event of your application. In the event handler, use the RaygunClient.Send method to send the Exception.

```csharp
private RaygunClient _client = new RaygunClient("YOUR_APP_API_KEY");

public App()
{
  DispatcherUnhandledException += OnDispatcherUnhandledException;
}

void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
{
  _client.Send(e.Exception);
}
```

### WinForms

Create an instance of RaygunClient by passing your app API key in the constructor. Attach an event handler to the Application.ThreadException event BEFORE calling Application.Run(...). In the event handler, use the RaygunClient.Send method to send the Exception.

```csharp
private static readonly RaygunClient _raygunClient = new RaygunClient("YOUR_APP_API_KEY");

[STAThread]
static void Main()
{
  Application.EnableVisualStyles();
  Application.SetCompatibleTextRenderingDefault(false);

  Application.ThreadException += new ThreadExceptionEventHandler(Application_ThreadException);

  Application.Run(new Form1());
}

private static void Application_ThreadException(object sender, ThreadExceptionEventArgs e)
{
  _raygunClient.Send(e.Exception);
}
```

### WinRT

In the App.xaml.cs constructor (or any main entry point to your application), call the static RaygunClient.Attach method using your API key.

```csharp
public App()
{
  RaygunClient.Attach("YOUR_APP_API_KEY");
}
```

At any point after calling the Attach method, you can use RaygunClient.Current to get the static instance. This can be used for manually sending messages (via the Send methods) or changing options such as the User identity string.

#### Limitations of WinRT UnhandledException event and Wrap() workarounds

The options available in WinRT for catching unhandled exceptions at this point in time are more limited compared to the options in the more mature .NET framework. The UnhandledException event will be raised when invalid XAML is parsed, in addition to other runtime exceptions that happen on the main UI thread. While many errors will be picked up this way and therefore be able to be sent to Raygun, others will be missed by this exception handler. In particular asynchronous code or Tasks that execute on background threads will not have their exceptions caught.

A workaround for this issue is provided with the Wrap() method. These allow you to pass the code you want to execute to an instance of the Raygun client - it will simply call it surrounded by a try-catch block. If the method you pass in does result in an exception being thrown this will be transmitted to Raygun, and the exception will again be thrown. Two overloads are available; one for methods that return void and another for methods that return an object.

#### Fody
Another option is to use the [Fody](https://github.com/Fody/Fody) library, and its [AsyncErrorHandler](https://github.com/Fody/AsyncErrorHandler) extension. This will automatically catch async exceptions and pass them to a handler of your choice (which would send to Raygun as above). See the [installation instructions here](https://github.com/Fody/Fody/wiki/SampleUsage), then check out the [sample project](https://github.com/Fody/FodyAddinSamples/tree/master/AsyncErrorHandlerWithRaygun) for how to use.

### Xamarin for Android

In the main/entry Activity of your application, use the static RaygunClient.Attach method using your app API key.
There is also an overload for the Attach method that lets you pass in a user-identity string which is useful for tracking affected users in your Raygun dashboard.

```csharp
RaygunClient.Attach("YOUR_APP_API_KEY");
```

At any point after calling the Attach method, you can use RaygunClient.Current to get the static instance. This can be used for manually sending messages or changing options such as the User identity string.

### Xamarin for iOS

In the main entry point of the application, use the static RaygunClient.Attach method using your app API key.

```csharp
static void Main(string[] args)
{
  RaygunClient.Attach("YOUR_APP_API_KEY");

  UIApplication.Main(args, null, "AppDelegate");
}
```

There is also an overload for the Attach method that lets you enable native iOS crash reporting.

```csharp
static void Main(string[] args)
{
  RaygunClient.Attach("YOUR_APP_API_KEY", true, true);

  UIApplication.Main(args, null, "AppDelegate");
}
```

The first boolean parameter is simply to enable the native iOS error reporting. The second boolean parameter is whether or not to hijack some of the native signals – this is to solve the well known iOS crash reporter issue where null reference exceptions within a try/catch block can cause the application to crash. By setting the second boolean parameter to true, the managed code will take over the SIGBUS and SIGSEGV iOS signals which solves the null reference issue. Doing this however prevents SIGBUS and SIGSEGV native errors from being detected, meaning they don’t get sent to Raygun. This is why we provide this as an option – so if you don’t have any issues with null reference exceptions occurring within try/catch blocks and you want to maximize the native errors that you can be notified of, then set the second boolean parameter to false.

At any point after calling the Attach method, you can use RaygunClient.Current to get the static instance. This can be used for manually sending messages or changing options such as the User identity string.

### Xamarin for Mac

In the main entry point of the application, use the static RaygunClient.Attach method using your app API key.

```csharp
static void Main(string[] args)
{
  RaygunClient.Attach("YOUR_APP_API_KEY");

  NSApplication.Init();
  NSApplication.Main(args);
}
```

At any point after calling the Attach method, you can use RaygunClient.Current to get the static instance. This can be used for manually sending messages or changing options such as the User identity string.

### Additional features for all .Net frameworks:

## Modify or cancel message

On a RaygunClient instance, attach an event handler to the SendingMessage event. This event handler will be called just before the RaygunClient sends an exception - either automatically or manually.
The event arguments provide the RaygunMessage object that is about to be sent. One use for this event handler is to add or modify any information on the RaygunMessage.
Another use for this method is to identify exceptions that you never want to send to raygun, and if so, set e.Cancel = true to cancel the send.

## Strip wrapper exceptions

If you have common outer exceptions that wrap a valuable inner exception which you'd prefer to group by, you can specify these by using the multi-parameter method:

```csharp
raygunClient.AddWrapperExceptions(typeof(TargetInvocationException));
```

In this case, if a TargetInvocationException occurs, it will be removed and replaced with the actual InnerException that was the cause.
Note that HttpUnhandledException and TargetInvocationException are already added to the wrapper exception list; you do not have to add these manually.
This method is useful if you have your own custom wrapper exceptions, or a framework is throwing exceptions using its own wrapper.

## Affected user tracking

There is a property named ```User``` on RaygunClient which you can set to be the current user's ID. This allows you to see the count of affected users for each error in the Raygun dashboard. 

If you want more detailed information about users (and the ability to use the new Affected User reporting feature when it is released), you can set the ```UserInfo``` property on the RaygunClient to a new RaygunIdentifierMessage object. [This class](https://github.com/MindscapeHQ/raygun4net/blob/master/Mindscape.Raygun4Net/Messages/RaygunIdentifierMessage.cs) has a number of properties on it to help identifier the user who experienced a crash.

Make sure to abide by any privacy policies that your company follows when using this feature.

### Properties
The only required field is Identifier.

```Identifier``` is the unique identifier from your system for this user.

```IsAnonymous``` is a flag indicating whether the user is logged in (or identifiable) or if they are anonymous. An anonymous user can still have a unique identifier.

```Email``` The user's email address. If you use email addresses to identify your users, feel free to set the identifier to their email and leave this blank, as we will use the identifier as the email address if it looks like one, and no email address is not specified.

```FullName``` The user's full name.

```FirstName``` The user's first (or preferred) name.

```UUID``` A device identifier. Could be used to identify users across devices, or machines that are breaking for many users.

### Usage
```csharp
raygunClient.User = "user@email.com";
// OR
raygunClient.UserInfo = new RaygunIdentifierMessage("user@email.com")
{
  IsAnonymous = false,
  FullName = "Robbie Raygun",
  FirstName = "Robbie"
};
```

## Version numbering

By default, Raygun will send the assembly version of your project with each report.

If you need to provide your own custom version value, you can do so by setting the ApplicationVersion property of the RaygunClient (in the format x.x.x.x where x is a positive integer).

## Tags and custom data

When sending exceptions manually, you can also send an arbitrary list of tags (an array of strings), and a collection of custom data (a dictionary of any objects).
This can be done using the various Send and SendInBackground method overloads.

## Proxy settings

The Raygun4NET provider uses the default Windows proxy settings (as set in Internet Explorer's Connection tab, or Web.config) when sending messages to the Raygun API. If your proxy requires authentication credentials, you can provide these by setting the `ProxyCredentials` property after instantiating a RaygunClient, then using it to send later:

```csharp
var raygunClient = new RaygunClient()
{
  ProxyCredentials = new NetworkCredential("user", "password")
};
```

## Custom grouping keys

You can provide your own grouping key if you wish. We only recommend this you're having issues with errors not being grouped properly.

On a RaygunClient instance, attach an event handler to the CustomGroupingKey event. This event handler will be called after Raygun has built the RaygunMessage object, but before the SendingMessage event is called.
The event arguments provide the RaygunMessage object that is about to be sent, and the original exception that triggered it. You can use anything you like to generate the key, and set it by `CustomGroupingKey`
property on the event arguments. Setting it to null or empty string will leave the exception to be grouped by Raygun, setting it to something will cause Raygun to group it with other exceptions you've sent with that key.

The key has a maximum length of 100.

## Troubleshooting


### Raygun4Net does not send crash reports and there are no errors to help troubleshoot why this is happening

- Raygun4net has the throwOnError property set to false by default. The first thing is to allow what ever error occurring in Raygun4Net to bubble up the stack and be reported as an unhandled exception, so add this attribute in the raygun4Net Config section or enable it in the config options of the client.  
`<RaygunSettings apikey="[Raygun4Net api key goes here]" throwOnError="true"/>`
- These errors will start going to the event viewer or you could attach a trace listener and have them logged to a text file as well
- There are many reasons why crash reports may not be sent through. In the Event that the error message mentions “*The underlying connection was closed: An unexpected error occurred on a send*.” This is probably a TLS handshake issue. Confirm this by inspecting the inner exception or the rest of the trace and look for cipher mismatch phrase. This will be a clear indication that there is a TLS issue. - To Resolve this Add the following global config where it is most convenient e.g. Global.asax  ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12 Taking care not to include the less secure protocols like SSL3 and to some extent the TLS1.1.


