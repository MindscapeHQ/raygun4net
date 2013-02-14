Raygun4Net - Raygun.io Provider for .NET Framework
===================


ASP.NET
====================
Add a section to configSections:

<section name="RaygunSettings" type="Mindscape.Raygun4Net.RaygunSettings, Mindscape.Raygun4Net"/>

Add the Raygun settings configuration block from above:

<RaygunSettings apikey="{{apikey for your application}}" />

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


WinForms/WPF/Other .NET applications
====================
Create an instance of RaygunClient (passing your API key in the constructor) then inside an Unhandled Exception
(or unobserved task exception) event handler make a call to Send, passing the ExceptionObject available in
the handler's EventArgs (with a cast).

WinRT
Reference the "Mindscape.Raygun4Net.WinRT.dll" instead.

Create a RaygunClient instance as above, then add a handler to the UnhandledException event to pick up
exceptions from the UI thread. Note that for WinRT you are required to pass the whole UnhandledExceptionEventArgs
object to Send(). For instance in App.xaml:

public App()
{
...
UnhandledException += App_UnhandledException;
}

void App_UnhandledException(object sender, UnhandledExceptionEventArgs e)
{
  _raygunClient.Send(e);
}

Then inside catch blocks place a call to Send, or use the Wrap helper method, passing your code you
want to execute. This will send (and throw) the exception in the case that one occurs.


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
the exception will be again be thrown. Two overloads are available; one for methods that return void and
another for methods that return an object.

====================
Troubleshooting
====================
If the solution fails to build due to missing dependencies (Newtonsoft etc), in Visual Studio 2012 ensure
you have the NuGet extension installed and that the Tools -> Options -> Package Manager ->
'Allow Nuget to download missing packages during build' box is checked. Then, go to the directory that
you cloned this repository into and run build.bat.
