Raygun4Net
==========

[Raygun.io](http://raygun.io) Provider for .NET Framework

! IMPORTANT CHANGE IN 5.0 !
====================

If you are manually referencing Raygun4Net dlls in your project rather than installing the NuGet package, then there are some minor dependency changes that you'll need to be aware of:

For Mvc projects, make sure your project references these dlls:
* Mindscape.Raygun4Net
* Mindscape.Raygun4Net.Mvc
* Mindscape Raygun4Net4

For other .Net 4.0+ projects, make sure your project references these dlls:
* Mindscape.Raygun4Net
* Mindscape.Raygun4Net4

For both of the above setups, you can find all the correct dlls in the Mvc and Net4 folders respectively within the .zip file hosted on the [latest GitHub release](https://github.com/MindscapeHQ/raygun4net/releases).
If you build the Raygun4Net projects yourself with the build.bat script, all the correct dlls will be in the raygun4net/build/Mvc and raygun4net/build/Net4 folders.
Or if you build the Raygun4Net projects in Visual Studio, all the correct dlls will be within the bin folder for each project.

If you are using Raygun4Net in a .Net project type not listed above, or if you are installing via NuGet, then you'll have no problems upgrading to version 5.0. There are no breaking changes in this version.

Installation
====================

* The easiest way to install this provider is by grabbing the NuGet package. Ensure the NuGet Visual Studio extension is installed, right-click on your project -> Manage NuGet Packages -> Online -> search for **Mindscape.Raygun4Net**, then install the appropriate package. Or, visit https://nuget.org/packages/Mindscape.Raygun4Net/ for instructions on installation using the package manager console.

* For Visual Studio 2008 (without NuGet) you can clone this repository, run build.bat, then add project references to **Mindscape.Raygun4Net.dll**

* If you have issues trying to install the package into a WinRT project, see the troubleshooting section below.

Supported platforms/frameworks
====================

Projects built with the following frameworks are supported:

* .NET 2.0, 3.5, 4.0+
* ASP.NET
* Mvc
* WebApi
* WinForms, WPF, console apps etc
* Windows Store apps (universal) for Windows 8.1 and Windows Phone 8.1
* Windows 8
* Windows Phone 7.1 and 8
* WinRT
* Xamarin.iOS and Xamarin.Mac (Both unified and classic)
* Xamarin.Android

Install the NuGet package to a project which uses one of the above frameworks and the correct assembly will be referenced.

Where is my app API key?
====================

When sending exceptions to the Raygun.io service, an app API key is required to map the messages to your application.

When you create a new application on your Raygun.io dashboard, your app API key is displayed at the top of the instructions page. You can also find the API key by clicking the "Application Settings" button in the side bar of the Raygun.io dashboard.

Namespace
====================
The main classes can be found in the Mindscape.Raygun4Net namespace.

Usage
====================

The Raygun4Net provider includes support for many .NET frameworks. Scroll down to find information about using Raygun for your type of application.

### ASP.NET
Add a section to configSections:

```
<section name="RaygunSettings" type="Mindscape.Raygun4Net.RaygunSettings, Mindscape.Raygun4Net"/>
```

Add the Raygun settings configuration block from above:

```
<RaygunSettings apikey="YOUR_APP_API_KEY" />
```

Now you can either setup Raygun to send unhandled exceptions automatically or/and send exceptions manually.

To send unhandled exceptions automatically, use the RaygunHttpModule in web.config in the appropriate way for your application:

For system.web:

```
<httpModules>
  <add name="RaygunErrorModule" type="Mindscape.Raygun4Net.RaygunHttpModule"/>
</httpModules>
```

For system.webServer:

```
<modules>
  <add name="RaygunErrorModule" type="Mindscape.Raygun4Net.RaygunHttpModule"/>
</modules>
```

Anywhere in you code, you can also send exception reports manually simply by creating a new instance of the RaygunClient and call one of the Send or SendInBackground methods.
This is most commonly used to send exceptions caught in a try/catch block.

```
try
{
  
}
catch (Exception e)
{
  new RaygunClient().SendInBackground(e);
}
```

Or to send exceptions in your own handlers rather than using the automatic setup above.

```
protected void Application_Error()
{
  var exception = Server.GetLastError();
  new RaygunClient().Send(exception);
}
```

####Additional ASP.NET configuration options

**Exclude errors by HTTP status code**

If using the HTTP module then you can exclude errors by their HTTP status code by providing a comma separated list of status codes to ignore in the configuration. For example if you wanted to exclude errors that return the [I'm a teapot](http://tools.ietf.org/html/rfc2324) response code, you could use the configuration below.

```
<RaygunSettings apikey="YOUR_APP_API_KEY" excludeHttpStatusCodes="418" />
```

**Exclude errors that originate from a local origin**

Toggle this boolean and the HTTP module will not send errors to Raygun.io if the request originated from a local origin. i.e. A way to prevent local debug/development from notifying Raygun without having to resort to Web.config transforms.

```
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
For example, ignoreFormFieldNames="*password*" will cause Raygun to ignore all form fields that **contain** "password" anywhere in the name.
These options are **not** case sensitive.

**Providing a custom RaygunClient to the http module**

Sometimes when setting up Raygun using the http module to send exceptions automatically, you may need to provide the http module with a custom RaygunClient instance in order to use some of the optional feature described at the end of this file.
To do this, get your Http Application to implement the IRaygunApplication interface. Implement the GenerateRaygunClient method to return a new (or previously created) RaygunClient instance.
The http module will use the RaygunClient returned from this method to send the unhandled exceptions.
In this method you can setup any additional options on the RaygunClient instance that you need - more information about each feature is described at the end of this file.

### MVC

As of version 4.0.0, Mvc support has been moved into a new NuGet package.
If you have an Mvc project, please uninstall the Raygun4Net NuGet package and install the Mindscape.Raygun4Net.Mvc NuGet package instead.
The NuGet package will include a readme containing everything you need to know about using it.

The Mvc and WebApi NuGet packages can be installed in the same project.

### Web Api

As of version 4.0.0, WebApi support has been moved into a new NuGet package.
If you have a WebApi project, please uninstall the Raygun4Net NuGet package and install the Mindscape.Raygun4Net.WebApi NuGet package instead.
The NuGet package will include a readme containing everything you need to know about using it.

The Mvc and WebApi NuGet packages can be installed in the same project.

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

### Windows Store Apps (Windows 8.1 and Windows Phone 8.1)

In the App.xaml.cs constructor (or any central entry point in your application), call the static RaygunClient.Attach method using your API key. This will catch and send all unhandled exception to Raygun.io for you.

```csharp
public App()
{
  RaygunClient.Attach("YOUR_APP_API_KEY");
}
```

At any point after calling the Attach method, you can use RaygunClient.Current to get the static instance. This can be used for manually sending messages (via the Send methods) or changing options such as the User identity string.

You can manually send exceptions with the SendAsync method. When manually sending, currently the compiler does not allow you to use `await` in a catch block. You can however call SendAsync in a blocking way:

```csharp
try
{
  throw new Exception("foo");
}
catch (Exception e)
{
  RaygunClient.Current.SendAsync(e);
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

### Windows Phone 7.1 and 8

In the App.xaml.cs constructor (or any main entry point to your application), call the static RaygunClient.Attach method using your API key.

```csharp
RaygunClient.Attach("YOUR_APP_API_KEY");
```

At any point after calling the Attach method, you can use RaygunClient.Current to get the static instance. This can be used for manually sending messages (via the Send methods) or changing options such as the User identity string.

### Xamarin for Android

In the main/entry Activity of your application, use the static RaygunClient.Attach method using your app API key.
There is also an overload for the Attach method that lets you pass in a user-identity string which is useful for tracking affected users in your Raygun.io dashboard.

```csharp
RaygunClient.Attach("YOUR_APP_API_KEY");
```

At any point after calling the Attach method, you can use RaygunClient.Current to get the static instance. This can be used for manually sending messages or changing options such as the User identity string.

### Xamarin for iOS

In the main entry point of the application, use the static RaygunClient.Attach method using your app API key.
There is also an overload for the Attach method that lets you pass in a user-identity string which is useful for tracking affected users in your Raygun.io dashboard.

```csharp
static void Main(string[] args)
{
  RaygunClient.Attach("YOUR_APP_API_KEY");

  UIApplication.Main(args, null, "AppDelegate");
}
```

At any point after calling the Attach method, you can use RaygunClient.Current to get the static instance. This can be used for manually sending messages or changing options such as the User identity string.

### Xamarin for Mac

Xamarin for Mac support is not included in the NuGet package or the Raygun4Net Xamarin Component. Instead, download the .zip of assemblies from the latest release on GitHub: https://github.com/MindscapeHQ/raygun4net/releases (Click the green button). Then copy and reference the Mindscape.Raygun4Net.Xamarin.Mac.dll into your Xamarin.Mac project.

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

raygunClient.AddWrapperExceptions(typeof(TargetInvocationException));

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
If you are using WinRT, the transmitted version number will be that of the Windows Store package, set in Package.appxmanifest (under Packaging).

If you need to provide your own custom version value, you can do so by setting the ApplicationVersion property of the RaygunClient (in the format x.x.x.x where x is a positive integer).

## Tags and custom data

When sending exceptions manually, you can also send an arbitrary list of tags (an array of strings), and a collection of custom data (a dictionary of any objects).
This can be done using the various Send and SendInBackground method overloads.

## Proxy settings

The Raygun4NET provider uses the default Windows proxy settings (as set in Internet Explorer's Connection tab, or Web.config) when sending messages to the Raygun API. If your proxy requires authentication credentials, you can provide these by setting the `ProxyCredentials` property after instantiating a RaygunClient, then using it to send later:

```csharp
var raygunClient = new RaygunClient()
{
  ProxyCredentials = new NetworkCredential("user", "pword")
};
```

## Troubleshooting

* If the solution fails to build due to missing dependencies, in Visual Studio 2012 ensure you have the NuGet extension installed and that the Tools -> Options -> Package Manager -> 'Allow NuGet to download missing packages during build' box is checked. Then, go to the directory that you cloned this repository into and run build.bat.

* When installing the package via NuGet into a WinRT project you encounter an error due to an invalid dependency, clone this repository into a directory via Git. Then, open a Powershell or command prompt in the directory location, and run `.\build.bat CompileWinRT`. Then, add the resulting Mindscape.Raygun4Net.WinRT.dll (located in the /release folder) to your project.
