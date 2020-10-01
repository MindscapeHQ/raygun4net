Raygun4Net.NetCore - Raygun Provider for .NET Core projects
===========================================================

Where is my app API key?
========================
When you create a new application on your Raygun dashboard, your app API key is displayed at the top of the instructions page.
You can also find the API key by clicking the "Application Settings" button in the side bar of the Raygun dashboard.

Namespace
=========
The main classes can be found in the Mindscape.Raygun4Net namespace.

Usage
=====

In your project file, add "Mindscape.Raygun4Net.NetCore": "6.4.0" to your dependencies.

Run dotnet.exe restore or restore packages within Visual Studio to download the package.

Anywhere in your code, you can also send exception reports manually simply by creating a new instance of the RaygunClient and calling one of the Send or SendInBackground methods.
This is most commonly used to send exceptions caught in a try/catch block.

try
{
}
catch (Exception e)
{
  new RaygunClient("YOUR_APP_API_KEY").SendInBackground(e);
} 

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
