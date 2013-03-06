Raygun4Net
==========

[Raygun.io](http://raygun.io) Provider for .NET Framework


Installation
====================

* The easiest way to install this provider is by grabbing the NuGet package. Ensure the NuGet Visual Studio extension is installed, right-click on your project -> Manage Nuget Packages -> Online -> search for **Mindscape.Raygun4Net**, then install it. Or, visit https://nuget.org/packages/Mindscape.Raygun4Net/ for instructions on installation using the package manager console.

* For Visual Studio 2008 (without NuGet) you can clone this repository, run build.bat, then add project references to **Mindscape.Raygun4Net.dll** and **Newtonsoft.Json.dll**.

* If you have issues trying to install the package into a WinRT project, see the troubleshooting section below.

Usage
====================

### ASP.NET
Add a section to configSections:

```
<section name="RaygunSettings" type="Mindscape.Raygun4Net.RaygunSettings, Mindscape.Raygun4Net"/>
```

Add the Raygun settings configuration block from above:

```
<RaygunSettings apikey="API_KEY_FOR_YOUR_APPLICATION" />
```

You can then either create a new instance of the RaygunClient class and call Send(Exception) e.g.

```
protected void Application_Error()
{
  var exception = Server.GetLastError();
  new RaygunClient().Send(exception);
}
```

Or there is an HttpModule you can add.

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

### WinForms/WPF/Other .NET applications

Create an instance of RaygunClient (passing your API key in the constructor) then inside an Unhandled Exception (or unobserved task exception) event handler make a call to Send, passing the ExceptionObject available in the handler's EventArgs (with a cast).

### WinRT
Reference the "Mindscape.Raygun4Net.WinRT.dll" instead.

Create a RaygunClient instance as above, then add a handler to the UnhandledException event to pick up exceptions from the UI thread. Note that for WinRT you are required to pass the whole UnhandledExceptionEventArgs object to Send(). For instance in App.xaml:

```csharp
public App()
{
...
UnhandledException += App_UnhandledException;
}

void App_UnhandledException(object sender, UnhandledExceptionEventArgs e)
{
  _raygunClient.Send(e);
}
```

Then inside catch blocks place a call to Send, or use the Wrap helper method, passing your code you want to execute. This will send (and throw) the exception in the case that one occurs.

#### Limitations of WinRT UnhandledException event and Wrap() workarounds

The options available in WinRT for catching unhandled exceptions at this point in time are more limited compared to the options in the more mature .NET framework. The UnhandledException event will be raised when invalid XAML is parsed, in addition to other runtime exceptions that happen on the main UI thread. While many errors will be picked up this way and therefore be able to be sent to Raygun, others will be missed by this exception handler. In particular asynchronous code or Tasks that execute on background threads will not have their exceptions caught.

A workaround for this issue is provided with the Wrap() method. These allow you to pass the code you want to execute to an instance of the Raygun client - it will simply call it surrounded by a try-catch block. If the method you pass in does result in an exception being thrown this will be transmitted to Raygun, and the exception will be again be thrown. Two overloads are available; one for methods that return void and another for methods that return an object.

#### Fody
Another option is to use the [Fody](https://github.com/Fody/Fody) library, and its [AsyncErrorHandling](https://github.com/Fody/AsyncErrorHandling) extension. This will automatically catch async exceptions and pass them to a handler of your choice (which would send to Raygun as above). See the [installation instructions here](https://github.com/Fody/Fody/wiki/SampleUsage), then check out the [sample project](https://github.com/Fody/FodyAddinSamples/tree/master/AsyncErrorHandlerWithRaygun) for how to use. 

## Version numbering and tags

* If you are plugging this provider into a classic .NET application, the version number that will be transmitted will be the AssemblyVersion. There is also an overload of Send() available where you can provide a different version if you wish (in the format x.x.x.x where x is a postive integer).

* If you are using WinRT, the transmitted version number will be that of the Windows Store package, set in in Package.appxmanifest (under Packaging).

* You can also set an arbitrary number of tags (as an array of strings), i.e. for tagging builds. This is optional and will be transmitted in addition to the version number above.

## Troubleshooting

* If the solution fails to build due to missing dependencies (Newtonsoft etc), in Visual Studio 2012 ensure you have the NuGet extension installed and that the Tools -> Options -> Package Manager -> 'Allow Nuget to download missing packages during build' box is checked. Then, go to the directory that you cloned this repository into and run build.bat.

* When installing the package via NuGet into a WinRT project you encounter an error due to an invalid dependency, clone this repository into a directory via Git. Then, open a Powershell or command prompt in the directory location, and run `.\build.bat CompileWinRT`. Then, add the resulting Mindscape.Raygun4Net.WinRT.dll (located in the /release folder) to your project.
