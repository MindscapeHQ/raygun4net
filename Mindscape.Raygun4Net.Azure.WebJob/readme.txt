Raygun4Net.Azure.WebJob - Raygun Provider for Azure WebJob SDK enabled projects
=============================================================

Where is my app API key?
========================
When you create a new application in your Raygun dashboard, your app API key is displayed at the top of the instructions page.
You can also find the API key by clicking the "Application Settings" button in the side bar of the Raygun dashboard.

Usage
======

Add a section to configSections:

<section name="RaygunSettings" type="Mindscape.Raygun4Net.RaygunSettings, Mindscape.Raygun4Net"/>

Add the Raygun settings configuration block from above:

<RaygunSettings apikey="YOUR_APP_API_KEY" />

Now you can setup Raygun to send unhandled exceptions automatically.

To send unhandled exceptions automatically, add a config.UseRaygun() call to your JobHostConfiguration setup (where config is your instance of JobHostConfiguration)

You may optionally also pass in an already constructed RaygunClient instance if you wish to do any further customization of Raygun4Net.

Remove sensitive request data
-----------------------------

If you have sensitive data in an HTTP request that you wish to prevent being transmitted to Raygun, you can provide lists of possible keys (names) to remove.
Keys to ignore can be specified on the RaygunSettings tag in web.config, (or you can use the equivalent methods on RaygunClient if you are setting things up in code).
The available options are:

ignoreFormFieldNames
ignoreHeaderNames
ignoreCookieNames
ignoreServerVariableNames

These can be set to be a comma separated list of keys to ignore. Setting an option as * will indicate that all the keys will not be sent to Raygun.
Placing * before, after or at both ends of a key will perform an ends-with, starts-with or contains operation respectively.
For example, ignoreFormFieldNames="*password*" will cause Raygun to ignore all form fields that contain "password" anywhere in the name.
These options are not case sensitive.

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

If you wish to add custom tags to automatically tracked exceptions you may add them via the RaygunExceptionHandler class

RaygunExceptionHandler.CustomTags = new[] {"CustomTag"};