Usage
====================

### Android

To send all unhandled exceptions in your application, use the static RaygunClient.Attach method using your app API key. The best place to put this is in the main/entry Activity of your application.

```csharp
RaygunClient.Attach("YOUR_APP_API_KEY");
```

### iOS

In the main entry point of the application, use the static RaygunClient.Attach method using your app API key. This will send all unhandled exceptions in your application.

```csharp
static void Main (string[] args)
{
  RaygunClient.Attach("YOUR_APP_API_KEY");

  UIApplication.Main (args, null, "AppDelegate");
}
```

### Android & IOS

You can also send handled exceptions by creating a new instance of the RaygunClient (using your API key) and call one of the Send methods. There are various overloads for the Send method that allow you to optionally send tags, custom data and an alternate version number.

Where is my app API key?
====================

When sending exceptions to the Raygun.io service, an app API key is required to map the messages to your application.

When you create a new application on your Raygun.io dashboard, your app API key is displayed at the top of the instructions page. You can also find the API key by clicking the "Application Settings" button in the side bar of the Raygun.io dashboard.

Sample
====================

Because of the API key requirement mentioned above, in order to run the sample you'll need to replace YOUR_APP_API_KEY in MainActivity to be an api key you've generated in your Raygun.io dashboard.

Namespace
====================
The main classes can be found in the Mindscape.Raygun4Net namespace.
