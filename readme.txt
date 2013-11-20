Raygun4Net - Raygun.io Provider for .NET Framework
===================

Where is my app API key?
====================
When you create a new application on your Raygun.io dashboard, your app API key is displayed at the top of the instructions page.
You can also find the API key by clicking the "Application Settings" button in the side bar of the Raygun.io dashboard.

Namespace
====================
The main classes can be found in the Mindscape.Raygun4Net namespace.

Usage
====================

The Raygun4Net provider includes support for many .NET frameworks.
Scroll down to find information about using Raygun for your type of application.

ASP.NET
====================
Add a section to configSections:

<section name="RaygunSettings" type="Mindscape.Raygun4Net.RaygunSettings, Mindscape.Raygun4Net"/>

Add the Raygun settings configuration block from above:

<RaygunSettings apikey="YOUR_APP_API_KEY" />

You can then either create a new instance of the RaygunClient class and call Send(Exception) e.g.

protected void Application_Error()
{
  var exception = Server.GetLastError();
  new RaygunClient().Send(exception);
}

Or there is an HttpModule you can add.

For system.web:

<httpModules>
  <add name="RaygunErrorModule" type="Mindscape.Raygun4Net.RaygunHttpModule"/>
</httpModules>

For system.webServer:

<modules>
  <add name="RaygunErrorModule" type="Mindscape.Raygun4Net.RaygunHttpModule"/>
</modules>

WPF
====================
Create an instance of RaygunClient by passing your app API key in the constructor.
Attach an event handler to the DispatcherUnhandledException event of your application.
In the event handler, use the RaygunClient.Send method to send the Exception.

private RaygunClient _client = new RaygunClient("YOUR_APP_API_KEY");

public App()
{
  DispatcherUnhandledException += OnDispatcherUnhandledException;
}

void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
{
  _client.Send(e.Exception);
}

WinForms
====================
Create an instance of RaygunClient by passing your app API key in the constructor.
Attach an event handler to the Application.ThreadException event BEFORE calling Application.Run(...).
In the event handler, use the RaygunClient.Send method to send the Exception.

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

WinRT
====================
Create a RaygunClient instance and pass in your app API key into the constructor, then add a handler to the UnhandledException event to pick up
exceptions from the UI thread. Note that for WinRT you are required to pass the whole UnhandledExceptionEventArgs
object to Send().

private RaygunClient _client = new RaygunClient("YOUR_APP_API_KEY");

public App()
{
  UnhandledException += App_UnhandledException;
}

void App_UnhandledException(object sender, UnhandledExceptionEventArgs e)
{
  _client.Send(e);
}

Limitations of WinRT UnhandledException event and Wrap() workarounds
====================
The options available in WinRT for catching unhandled exceptions at this point in time are more limited
compared to the options in the more mature .NET framework. The UnhandledException event will be raised when
invalid XAML is parsed, in addition to other runtime exceptions that happen on the main UI thread. While
many errors will be picked up this way and therefore be able to be sent to Raygun, others will be missed by
this exception handler. In particular asynchronous code or Tasks that execute on background threads will
not have their exceptions caught.

A workaround for this issue is provided with the Wrap() method. These allow you to pass the code you want
to execute to an instance of the Raygun client - it will simply call it surrounded by a try-catch block.
If the method you pass in does result in an exception being thrown this will be transmitted to Raygun, and
the exception will again be thrown. Two overloads are available; one for methods that return void and
another for methods that return an object.

Windows Phone 7.1 and 8
====================
Create a RaygunClient instance and pass in your app API key into the constructor. In the UnhandledException event handler of App.xaml.cs, use the RaygunClient to send the arguments.

private RaygunClient _client = new RaygunClient("YOUR_APP_API_KEY");

private void Application_UnhandledException(object sender, ApplicationUnhandledExceptionEventArgs e)
{
  _client.Send(e);
}

Xamarin for Android
====================
In the main/entry Activity of your application, use the static RaygunClient.Attach method using your app API key.
There is also an overload for the Attach method that lets you pass in a user-identity string which is useful for tracking affected users in your Raygun.io dashboard.

RaygunClient.Attach("YOUR_APP_API_KEY");

At any point after calling the Attach method, you can use RaygunClient.SharedClient to get the static instance. This can be used for manually sending messages or changing options such as the User identity string.

Xamarin for iOS
====================
In the main entry point of the application, use the static RaygunClient.Attach method using your app API key.
There is also an overload for the Attach method that lets you pass in a user-identity string which is useful for tracking affected users in your Raygun.io dashboard.

static void Main (string[] args)
{
  RaygunClient.Attach("YOUR_APP_API_KEY");

  UIApplication.Main (args, null, "AppDelegate");
}

At any point after calling the Attach method, you can use RaygunClient.SharedClient to get the static instance. This can be used for manually sending messages or changing options such as the User identity string.

====================
Troubleshooting
====================
If the solution fails to build due to missing dependencies (Newtonsoft etc), in Visual Studio 2012 ensure
you have the NuGet extension installed and that the Tools -> Options -> Package Manager ->
'Allow Nuget to download missing packages during build' box is checked. Then, go to the directory that
you cloned this repository into and run build.bat.
