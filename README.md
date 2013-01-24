Raygun4Net
==========

Raygun.io Plugin for .NET Framework


Installation & Usage
====================

Building from source requires the Nuget VS2012 extension. Nuget will fetch the dependencies automatically - you will need to enable this option if it isn't already (Tools -> Options -> Package Manager -> Allow Nuget to download missing packages during build).

In your project, add a reference to "Mindscape.Raygun4Net.dll"

### ASP.NET
Add a section to configSections:

```
<section name="RaygunSettings" type="Mindscape.Raygun4Net.RaygunSettings, Mindscape.Raygun4Net"/>
```

Add the Raygun settings configuration block from above:

```
<RaygunSettings apikey="{{apikey for your application}}" />
```

You can then either use the RaygunClient class directly to pass exceptions to Raygun or there is an HttpModule you can add.

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

## Troubleshooting

If the solution fails to build due to missing dependencies (Newtonsoft etc), in Visual Studio 2012 ensure you have the NuGet extension installed and that the Tools -> Options -> Package Manager -> 'Allow Nuget to download missing packages during build' box is checked. Then, go to the directory that you cloned this repository into and run build.bat.