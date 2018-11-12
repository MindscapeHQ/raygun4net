Raygun4Net.Xamarin.iOS - Raygun Provider for Xamarin iOS
===================

Where is my app API key?
====================
When you create a new application in your Raygun dashboard, your app API key is displayed at the top of the instructions page.
You can also find the API key by clicking the "Application Settings" button in the side bar of the Raygun dashboard.

Namespace
====================
The main classes can be found in the Mindscape.Raygun4Net namespace.

Usage
====================

In the main entry point of the application, use the static RaygunClient.Attach method using your app API key.

static void Main(string[] args)
{
  RaygunClient.Attach("YOUR_APP_API_KEY");

  UIApplication.Main(args, null, "AppDelegate");
}

There is also an overload for the Attach method that lets you enable native iOS crash reporting.

static void Main(string[] args)
{
  RaygunClient.Attach("YOUR_APP_API_KEY", true, true);

  UIApplication.Main(args, null, "AppDelegate");
}

The first boolean parameter is simply to enable the native iOS crash reporting.
The second boolean parameter is whether or not to hijack some of the native signals  this is to solve the well known iOS crash reporter issue where null reference exceptions within a try/catch block can cause the application to crash.
By setting the second boolean parameter to true, the managed code will take over the SIGBUS and SIGSEGV iOS signals which solves the null reference issue.
Doing this however prevents SIGBUS and SIGSEGV native errors from being detected, meaning they dont get sent to Raygun.
This is why we provide this as an option  so if you dont have any issues with null reference exceptions occurring within try/catch blocks and you want to maximize the native errors that you can be notified of, then set the second boolean parameter to false.

At any point after calling the Attach method, you can use RaygunClient.Current to get the static instance. This can be used for manually sending messages or changing options such as the User identity string.

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
If you are using WinRT, the transmitted version number will be that of the Windows Store package, set in Package.appxmanifest (under Packaging).

If you need to provide your own custom version value, you can do so by setting the ApplicationVersion property of the RaygunClient (in the format x.x.x.x where x is a positive integer).

Tags and custom data
--------------------

When sending exceptions manually, you can also send an arbitrary list of tags (an array of strings), and a collection of custom data (a dictionary of any objects).
This can be done using the various Send and SendInBackground method overloads.

Custom grouping keys
--------------------
You can provide your own grouping key if you wish. We only recommend this you're having issues with errors not being grouped properly.

On a RaygunClient instance, attach an event handler to the CustomGroupingKey event. This event handler will be called after Raygun has built the RaygunMessage object, but before the SendingMessage event is called.
The event arguments provide the RaygunMessage object that is about to be sent, and the original exception that triggered it. You can use anything you like to generate the key, and set it by `CustomGroupingKey`
property on the event arguments. Setting it to null or empty string will leave the exception to be grouped by Raygun, setting it to something will cause Raygun to group it with other exceptions you've sent with that key.

The key has a maximum length of 100.
