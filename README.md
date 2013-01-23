Raygun4Net
==========

Raygun.io Plugin for .NET Framework


Installation & Usage
====================

Reference the "Mindscape.Raygun4Net.dll"

Nuget can fetch the dependencies automatically when building for the first time. You may need to turn this option on (Tools -> Options -> Package Manager -> Allow Nuget to download missing packages during build).

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

Create an instance of RaygunClient (passing your API key in the constructor) then inside an Unhandled Exception event handler make a call to Send, passing the exception.

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
