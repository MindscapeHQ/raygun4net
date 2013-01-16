Raygun4Net
==========

Raygun.io Plugin for .NET Framework


Installation & Usage
====================

Reference the "Mindscape.Raygun4Net.dll"

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

### WinRT
Reference the "Mindscape.Raygun4Net.WinRT.dll" instead.

You can create an instance of RaygunClient, passing your API key in the constructor, then subscribing to the UnhandledException event to pick up XAML exceptions, for instance in App.xaml:

```
public App()
{
...
UnhandledException += App_UnhandledException;
}

void App_UnhandledException(object sender, UnhandledExceptionEventArgs e)
{
  _raygunClient.Send(e.Exception);
}
```

Then inside catch blocks place a call to Send, or use the Wrap helper method, passing your code you want to execute. This will send (and throw) the exception in the case that one occurs.
