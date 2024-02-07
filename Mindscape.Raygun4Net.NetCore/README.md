Raygun4Net.NetCore - Raygun Provider for .NET 6+ projects
===========================================================

Where is my app API key?
========================
When you create a new application on your Raygun dashboard, your app API key is displayed within the instructions page.
You can also find the API key by clicking the "Application Settings" button in the side bar of the Raygun dashboard.

Namespace
=========
The main classes can be found in the Mindscape.Raygun4Net namespace.

Installation
=====

Install the **Mindscape.Raygun4Net.NetCore** NuGet package into your project. You can either use the below dotnet CLI command, or the NuGet management GUI in the IDE you use.

```
dotnet add package Mindscape.Raygun4Net.NetCore
```

Create an instance of RaygunClient by passing your app API key to the constructor, and hook it up to the unhandled exception delegate. This is typically done in Program.cs or the main method.

```csharp
using Mindscape.Raygun4Net;

private static RaygunClient _raygunClient = new RaygunClient("paste_your_api_key_here");

AppDomain.CurrentDomain.UnhandledException += (object sender, UnhandledExceptionEventArgs e) =>
  _raygunClient.Send(e.ExceptionObject as Exception);
  
```

Alternatively you can configure RaygunClient to automatically report any Unhandled Exceptions directly to Raygun

```csharp
using Mindscape.Raygun4Net;

private static RaygunSettings _raygunSettings = new RaygunSettings
{
  ApiKey = "paste_your_api_key_here",
  CatchUnhandledExceptions = true // automatically reports any unhandled exceptions to Raygun
};
private static RaygunClient _raygunClient = new RaygunClient(_raygunSettings);
```

Add some temporary code to throw an exception and manually send it to Raygun.

```csharp
try
{
  ...
}
catch (Exception e)
{
  _raygunClient.SendInBackground(e);
} 
```

Manually sending exceptions
------------------------

The above instructions will setup Raygun4Net to automatically detect and send all unhandled exceptions. Sometimes you may want to send exceptions manually, such as handled exceptions from within a try/catch block.

RaygunClient provides Send and SendInBackground methods for manually sending to Raygun. It's important to note that SendInBackground should only be used for handled exceptions, rather than exceptions that will cause the application to crash - otherwise the application will most likely shutdown all threads before Raygun is able to finish sending. 

Additional configuration options and features
=============================================

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

---
See the [Raygun docs](https://raygun.com/documentation/language-guides/dotnet/crash-reporting/net-core/) for more detailed instructions on how to use this provider.